using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// Convert CP solver solutions to scheduling system solutions
    /// </summary>
    public class SolutionConverter
    {
        /// <summary>
        /// Convert CP solver solutions to scheduling system solutions (alias method)
        /// </summary>
        public SchedulingSolution ConvertToDomainSolution(SchedulingProblem problem, Dictionary<string, long> cpSolution)
        {
            // Call original method
            return ConvertToSchedulingSolution(cpSolution, problem);
        }
        
        /// <summary>
        /// Convert CP solver solutions to scheduling system solutions
        /// </summary>
        public SchedulingSolution ConvertToSchedulingSolution(Dictionary<string, long> cpSolution, SchedulingProblem problem)
        {
            var solution = new SchedulingSolution
            {
                Problem = problem,
                Algorithm = "CP",
                GeneratedAt = DateTime.Now
            };

            // Parse variable names and create assignments
            var assignments = new List<SchedulingAssignment>();
            int assignmentId = 1;

            foreach (var entry in cpSolution)
            {
                // Only process variables with value 1 (indicating this assignment is selected)
                if (entry.Value != 1)
                    continue;

                string varName = entry.Key;
                
                // Parse variable name format: c{sectionId}_t{timeSlotId}_r{classroomId}_f{teacherId}
                var parts = varName.Split('_');
                if (parts.Length != 4)
                    continue;
                
                // Extract IDs
                int sectionId = int.Parse(parts[0].Substring(1));
                int timeSlotId = int.Parse(parts[1].Substring(1));
                int classroomId = int.Parse(parts[2].Substring(1));
                int teacherId = int.Parse(parts[3].Substring(1));

                // Find related information
                var timeSlot = problem.TimeSlots.FirstOrDefault(t => t.Id == timeSlotId);
                if (timeSlot == null)
                    continue;
                
                var classroom = problem.Classrooms.FirstOrDefault(r => r.Id == classroomId);
                if (classroom == null)
                    continue;

                // Create assignment
                var assignment = new SchedulingAssignment
                {
                    Id = assignmentId++,
                    SectionId = sectionId,
                    TimeSlotId = timeSlotId,
                    ClassroomId = classroomId,
                    TeacherId = teacherId,
                    DayOfWeek = timeSlot.DayOfWeek,
                    StartTime = timeSlot.StartTime,
                    EndTime = timeSlot.EndTime,
                    Building = classroom.Building,
                    ClassroomName = classroom.Name
                };

                assignments.Add(assignment);
            }

            solution.Assignments = assignments;
            return solution;
        }
    }
} 