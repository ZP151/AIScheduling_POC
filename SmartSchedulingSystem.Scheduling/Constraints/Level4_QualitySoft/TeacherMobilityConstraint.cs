using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level4_QualitySoft
{
    /// <summary>
    /// Teacher mobility constraint - Evaluates the reasonableness of teacher movement between different buildings
    /// </summary>
    public class TeacherMobilityConstraint : BaseConstraint, IConstraint
    {
        /// <summary>
        /// Constraint definition ID
        /// </summary>
        public override string DefinitionId => ConstraintDefinitions.TeacherMobility;

        /// <summary>
        /// Basic rule
        /// </summary>
        public override string BasicRule => BasicSchedulingRules.TeacherPreference;
        
        /// <summary>
        /// ID
        /// </summary>
        public override int Id => 12;
        
        /// <summary>
        /// Name
        /// </summary>
        public override string Name { get; } = "Teacher Mobility Constraint";
        
        /// <summary>
        /// Description
        /// </summary>
        public override string Description { get; } = "Ensures teachers don't need to move quickly between different buildings for consecutive courses";
        
        /// <summary>
        /// Whether this is a hard constraint
        /// </summary>
        public override bool IsHard => false;
        
        /// <summary>
        /// Constraint hierarchy level
        /// </summary>
        public override ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level4_QualitySoft;
        
        /// <summary>
        /// Constraint category
        /// </summary>
        public override string Category => "TeacherQuality";

        /// <summary>
        /// Constructor
        /// </summary>
        public TeacherMobilityConstraint()
        {
            Weight = 0.4;
        }

        /// <summary>
        /// Evaluate constraint
        /// </summary>
        public override (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (!IsValidSolution(solution))
                return (1.0, new List<SchedulingConflict>());

            return EvaluateTeacherMobility(solution);
        }

        private bool IsValidSolution(SchedulingSolution solution)
        {
            return solution != null && 
                   solution.Assignments != null && 
                   solution.Assignments.Count > 0 && 
                   solution.Problem != null;
        }

        private (double Score, List<SchedulingConflict> Conflicts) EvaluateTeacherMobility(SchedulingSolution solution)
        {
            List<SchedulingConflict> conflicts = new List<SchedulingConflict>();
            int totalConsecutive = 0;
            int distantCount = 0;
            
            // Analyze consecutive courses for each teacher
            var assignmentsByTeacher = solution.Assignments
                .Where(a => a.TeacherId > 0 && a.ClassroomId > 0)
                .GroupBy(a => a.TeacherId)
                .ToList();
                
            foreach (var teacherGroup in assignmentsByTeacher)
            {
                int teacherId = teacherGroup.Key;
                var teacherAssignments = teacherGroup.ToList();
                
                // Group by day
                var assignmentsByDay = teacherAssignments
                    .GroupBy(a => solution.Problem.TimeSlots.FirstOrDefault(t => t.Id == a.TimeSlotId)?.DayOfWeek ?? 0)
                    .Where(g => g.Key > 0) // Filter out assignments with unknown time slots
                    .ToList();
                
                foreach (var dayGroup in assignmentsByDay)
                {
                    var dayAssignments = dayGroup.ToList();
                    
                    // Sort by time
                    dayAssignments.Sort((a, b) =>
                    {
                        var timeSlotA = solution.Problem.TimeSlots.FirstOrDefault(t => t.Id == a.TimeSlotId);
                        var timeSlotB = solution.Problem.TimeSlots.FirstOrDefault(t => t.Id == b.TimeSlotId);
                        
                        if (timeSlotA == null || timeSlotB == null)
                            return 0;
                            
                        return timeSlotA.StartTime.CompareTo(timeSlotB.StartTime);
                    });
                    
                    // Check classroom distances between consecutive courses
                    for (int i = 0; i < dayAssignments.Count - 1; i++)
                    {
                        var current = dayAssignments[i];
                        var next = dayAssignments[i + 1];
                        
                        var currentTimeSlot = solution.Problem.TimeSlots.FirstOrDefault(t => t.Id == current.TimeSlotId);
                        var nextTimeSlot = solution.Problem.TimeSlots.FirstOrDefault(t => t.Id == next.TimeSlotId);
                        
                        if (currentTimeSlot != null && nextTimeSlot != null && IsConsecutive(currentTimeSlot, nextTimeSlot))
                        {
                            totalConsecutive++;
                            
                            var currentRoom = solution.Problem.Classrooms.FirstOrDefault(c => c.Id == current.ClassroomId);
                            var nextRoom = solution.Problem.Classrooms.FirstOrDefault(c => c.Id == next.ClassroomId);
                            
                            if (currentRoom != null && nextRoom != null && currentRoom.Building != nextRoom.Building)
                            {
                                // Consecutive courses in different buildings
                                distantCount++;
                                conflicts.Add(CreateBuildingDistanceConflict(
                                    solution, teacherId, current, next, nextTimeSlot, currentRoom, nextRoom));
                            }
                        }
                    }
                }
            }
            
            // Calculate mobility score
            double score = totalConsecutive > 0 ? Math.Max(0, 1.0 - ((double)distantCount / totalConsecutive)) : 1.0;
            return (score, conflicts);
        }

        private SchedulingConflict CreateBuildingDistanceConflict(
            SchedulingSolution solution, int teacherId, 
            SchedulingAssignment current, SchedulingAssignment next,
            TimeSlotInfo timeSlot, ClassroomInfo currentRoom, ClassroomInfo nextRoom)
        {
            var teacher = solution.Problem.Teachers.FirstOrDefault(t => t.Id == teacherId);
            string teacherName = teacher?.Name ?? $"Teacher {teacherId}";
            
            return new SchedulingConflict
            {
                Id = solution.GetNextConflictId(),
                ConstraintId = this.Id,
                Type = SchedulingConflictType.BuildingDistanceConflict,
                Description = $"{teacherName} needs to move from {currentRoom.Building}-{currentRoom.Name} " +
                             $"to {nextRoom.Building}-{nextRoom.Name} in consecutive time slots, different buildings may cause delays",
                Severity = ConflictSeverity.Moderate,
                Category = "Unreasonable Teacher Movement Distance",
                InvolvedEntities = new Dictionary<string, List<int>>
                {
                    { "Teachers", new List<int> { teacherId } },
                    { "TimeSlots", new List<int> { timeSlot.Id } },
                    { "Classrooms", new List<int> { currentRoom.Id, nextRoom.Id } }
                }
            };
        }

        private bool IsConsecutive(TimeSlotInfo first, TimeSlotInfo second)
        {
            // If two time slots are on the same day and the second one immediately follows the first
            return first.DayOfWeek == second.DayOfWeek && 
                   Math.Abs((second.StartTime - first.EndTime).TotalMinutes) <= 20; // Allow 20 minutes margin
        }
    }
} 