using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters
{
    /// <summary>
    /// Converting teacher conflict constraints to CP model constraints
    /// </summary>
    public class TeacherConflictConstraintConverter : ICPConstraintConverter
    {
        /// <summary>
        /// Get the constraint level of the constraint converter
        /// </summary>
        public Engine.ConstraintApplicationLevel ConstraintLevel => Engine.ConstraintApplicationLevel.Basic;

        public void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));  
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            // Process all teachers
            foreach (var teacher in problem.Teachers)
            {
                // Process all time slots
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // Find all variables that assign this teacher to this time slot
                    var conflictingVars = variables
                        .Where(kv => kv.Key.Contains($"_t{timeSlot.Id}_") &&
                                    kv.Key.EndsWith($"_f{teacher.Id}"))
                        .Select(kv => kv.Value)
                        .ToList();

                    // If there are multiple variables, add a constraint to ensure that at most one is 1 (a teacher can only teach one course at the same time)
                    if (conflictingVars.Count > 1)
                    {
                        model.Add(LinearExpr.Sum(conflictingVars) <= 1);
                    }
                }
            }

            // Consider teacher availability for consecutive time slots (e.g., if a teacher needs to move between different campuses)
            // Here, assume problem.TimeSlots is ordered by time
            var orderedTimeSlots = problem.TimeSlots.OrderBy(ts => ts.DayOfWeek).ThenBy(ts => ts.StartTime).ToList();

            for (int i = 0; i < orderedTimeSlots.Count - 1; i++)
            {
                var currentSlot = orderedTimeSlots[i];
                var nextSlot = orderedTimeSlots[i + 1];

                // Only process consecutive time slots on the same day
                if (currentSlot.DayOfWeek == nextSlot.DayOfWeek)
                {
                    // Calculate the interval between the two time slots (minutes)
                    var interval = (nextSlot.StartTime - currentSlot.EndTime).TotalMinutes;

                    // If the interval is too short (less than 15 minutes), add a constraint to prevent a teacher from teaching in different buildings/campuses consecutively
                    if (interval < 15)
                    {
                        foreach (var teacher in problem.Teachers)
                        {
                            // Find all variables that assign this teacher to the current time slot
                            var currentSlotVars = variables
                                .Where(kv => kv.Key.Contains($"_t{currentSlot.Id}_") &&
                                           kv.Key.EndsWith($"_f{teacher.Id}"))
                                .ToList();

                            // Find all variables that assign this teacher to the next time slot
                            var nextSlotVars = variables
                                .Where(kv => kv.Key.Contains($"_t{nextSlot.Id}_") &&
                                           kv.Key.EndsWith($"_f{teacher.Id}"))
                                .ToList();

                            // For each pair of possible assignments, check if they are in different buildings/campuses
                            foreach (var currentVar in currentSlotVars)
                            {
                                string currentKey = currentVar.Key;
                                int currentRoomId = ExtractRoomId(currentKey);

                                foreach (var nextVar in nextSlotVars)
                                {
                                    string nextKey = nextVar.Key;
                                    int nextRoomId = ExtractRoomId(nextKey);

                                    // If in different buildings/campuses, add a constraint to prevent simultaneous allocation
                                    if (AreRoomsInDifferentBuildings(currentRoomId, nextRoomId, problem))
                                    {
                                        // If both are 1, then the constraint is violated, so the sum of the two variables must be at most 1
                                        model.Add(currentVar.Value + nextVar.Value <= 1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extract the classroom ID from the variable name
        /// </summary>
        private int ExtractRoomId(string variableName)
        {
            // Variable name format: c{courseId}_t{timeSlotId}_r{roomId}_f{teacherId}
            var parts = variableName.Split('_');
            if (parts.Length >= 3 && parts[2].StartsWith("r"))
            {
                if (int.TryParse(parts[2].Substring(1), out int roomId))
                {
                    return roomId;
                }
            }

            throw new ArgumentException($"Cannot extract classroom ID from variable name {variableName}.");
        }

        /// <summary>
        /// Determine if two classrooms are in different buildings/campuses
        /// </summary>
        private bool AreRoomsInDifferentBuildings(int roomId1, int roomId2, SchedulingProblem problem)
        {
            var room1 = problem.Classrooms.FirstOrDefault(r => r.Id == roomId1);
            var room2 = problem.Classrooms.FirstOrDefault(r => r.Id == roomId2);

            if (room1 == null || room2 == null)
            {
                return false; // If classroom information is not found, assume they are in the same building
            }

            // If the classrooms are in different campuses, they must be in different buildings
            if (room1.CampusId != room2.CampusId)
            {
                return true;
            }

            // Check if they are in the same building
            return room1.Building != room2.Building;
        }
    }
}