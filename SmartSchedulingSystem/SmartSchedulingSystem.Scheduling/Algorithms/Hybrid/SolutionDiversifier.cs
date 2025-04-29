using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.Hybrid
{
    /// <summary>
    /// Tool class for generating and evaluating solution diversity
    /// </summary>
    public class SolutionDiversifier
    {
        private readonly Random _random = new Random();
        private readonly ILogger<SolutionDiversifier> _logger;

        public SolutionDiversifier(ILogger<SolutionDiversifier> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Filter diverse solution set
        /// </summary>
        /// <param name="solutions">List of candidate solutions</param>
        /// <param name="count">Number of solutions needed</param>
        /// <param name="evaluator">Solution evaluator</param>
        /// <returns>Diverse solution set</returns>
        public List<SchedulingSolution> FilterDiverseSolutions(List<SchedulingSolution> solutions, int count, SolutionEvaluator evaluator)
        {
            if (solutions.Count <= count)
            {
                _logger.LogInformation($"Number of solutions {solutions.Count} does not exceed required count {count}, no filtering needed");
                return solutions;
            }

            _logger.LogInformation($"Starting to filter {count} diverse solutions, original solution count: {solutions.Count}");

            var diverseSolutions = new List<SchedulingSolution>();

            // First add the solution with the highest score
            var bestSolution = solutions.OrderByDescending(s => evaluator.Evaluate(s).Score).First();
            diverseSolutions.Add(bestSolution);
            solutions.Remove(bestSolution);

            _logger.LogDebug($"Added highest scoring solution: #{bestSolution.Id}, Score: {evaluator.Evaluate(bestSolution).Score:F2}");

            // Then add the solution with the maximum difference from the existing solutions
            while (diverseSolutions.Count < count && solutions.Count > 0)
            {
                // Calculate the minimum distance between each remaining solution and the selected solutions
                var solutionDistances = solutions.Select(solution =>
                {
                    double minDistance = diverseSolutions.Min(s => CalculateDistance(s, solution));
                    return new { Solution = solution, MinDistance = minDistance };
                }).ToList();

                // Select the solution with the maximum difference
                var mostDiverseSolution = solutionDistances.OrderByDescending(x => x.MinDistance).First().Solution;
                double maxDistance = solutionDistances.Max(x => x.MinDistance);
                
                diverseSolutions.Add(mostDiverseSolution);
                solutions.Remove(mostDiverseSolution);
                _logger.LogDebug($"Added diverse solution #{mostDiverseSolution.Id}, Minimum distance to selected solutions: {maxDistance:F2}");
            }

            _logger.LogInformation($"Diversity filtering completed, {diverseSolutions.Count} solutions selected");
            return diverseSolutions;
        }
        
        /// <summary>
        /// Diversify solutions
        /// </summary>
        /// <param name="problem">Scheduling problem</param>
        /// <param name="solutions">List of original solutions</param>
        /// <param name="count">Number of solutions to return</param>
        /// <returns>Diversified solutions</returns>
        public IEnumerable<SchedulingSolution> DiversifySolutions(SchedulingProblem problem, List<SchedulingSolution> solutions, int count)
        {
            if (solutions == null || solutions.Count == 0)
            {
                _logger.LogWarning("Cannot diversify empty solution list");
                return new List<SchedulingSolution>();
            }
            
            if (solutions.Count <= count)
            {
                _logger.LogInformation($"Number of solutions {solutions.Count} does not exceed required count {count}, no diversification needed");
                return solutions.ToList();
            }
            
            _logger.LogInformation($"Starting to filter {solutions.Count} solutions for diversification, target count: {count}");
            
            // First filter out solutions that satisfy all hard constraints
            var validSolutions = new List<SchedulingSolution>();
            foreach (var solution in solutions)
            {
                bool satisfiesHardConstraints = true;
                
                // Check teacher availability constraint
                if (problem.TeacherAvailabilities.Count > 0)
                {
                    foreach (var assignment in solution.Assignments)
                    {
                        var teacherAvailability = problem.TeacherAvailabilities
                            .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId && 
                                                  ta.TimeSlotId == assignment.TimeSlotId);
                        
                        if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                        {
                            // If the teacher is not available at this time slot, the solution does not satisfy the hard constraint
                            satisfiesHardConstraints = false;
                            _logger.LogDebug($"Solution #{solution.Id} violates teacher availability constraint: Teacher {assignment.TeacherId} is not available at time slot {assignment.TimeSlotId}");
                            break;
                        }
                    }
                }
                
                // Check classroom availability constraint
                if (satisfiesHardConstraints && problem.ClassroomAvailabilities.Count > 0)
                {
                    foreach (var assignment in solution.Assignments)
                    {
                        var classroomAvailability = problem.ClassroomAvailabilities
                            .FirstOrDefault(ca => ca.ClassroomId == assignment.ClassroomId && 
                                                  ca.TimeSlotId == assignment.TimeSlotId);
                        
                        if (classroomAvailability != null && !classroomAvailability.IsAvailable)
                        {
                            // If the classroom is not available at this time slot, the solution does not satisfy the hard constraint
                            satisfiesHardConstraints = false;
                            _logger.LogDebug($"Solution #{solution.Id} violates classroom availability constraint: Classroom {assignment.ClassroomId} is not available at time slot {assignment.TimeSlotId}");
                            break;
                        }
                    }
                }
                
                // If the solution satisfies all hard constraints, add it to the valid solution list
                if (satisfiesHardConstraints)
                {
                    validSolutions.Add(solution);
                }
            }
            
            _logger.LogInformation($"Hard constraint check completed, {validSolutions.Count}/{solutions.Count} solutions satisfy all hard constraints");
            
            // If there are not enough valid solutions, use the original solutions to make up the shortage
            if (validSolutions.Count < count)
            {
                var invalidSolutions = solutions.Except(validSolutions).ToList();
                validSolutions.AddRange(invalidSolutions.Take(count - validSolutions.Count));
                _logger.LogWarning($"Valid solution count insufficient, adding {count - validSolutions.Count} from invalid solutions as supplement");
            }
            
            // Create a diverse solution set
            var diverseSolutions = new List<SchedulingSolution>();
            
            // Use Id instead of Score to sort, to avoid sorting issues caused by Score being 0
            var remainingSolutions = validSolutions.OrderByDescending(s => s.Id).ToList();
            
            var bestSolution = remainingSolutions.First();
            diverseSolutions.Add(bestSolution);
            remainingSolutions.Remove(bestSolution);
            _logger.LogDebug($"Added first solution: #{bestSolution.Id}");
            
            // Then add the solution with the maximum difference from the existing solutions
            while (diverseSolutions.Count < count && remainingSolutions.Count > 0)
            {
                // Calculate the minimum distance between each remaining solution and the selected solutions
                var solutionDistances = remainingSolutions.Select(solution =>
                {
                    double minDistance = diverseSolutions.Min(s => CalculateDistance(s, solution));
                    return new { Solution = solution, MinDistance = minDistance };
                }).ToList();
                
                // Select the solution with the maximum difference
                var mostDiverseSolution = solutionDistances.OrderByDescending(x => x.MinDistance).First().Solution;
                double maxDistance = solutionDistances.Max(x => x.MinDistance);
                
                diverseSolutions.Add(mostDiverseSolution);
                remainingSolutions.Remove(mostDiverseSolution);
                _logger.LogDebug($"Added diverse solution #{mostDiverseSolution.Id}, Minimum distance to selected solutions: {maxDistance:F2}");
            }
            
            _logger.LogInformation($"Diversity filtering completed, {diverseSolutions.Count} solutions selected");
            return diverseSolutions;
        }

        /// <summary>
        /// Calculate the distance between two solutions (0-1)
        /// </summary>
        public double CalculateDistance(SchedulingSolution solution1, SchedulingSolution solution2)
        {
            if (solution1 == null || solution2 == null)
            {
                throw new ArgumentNullException("Solution cannot be null");
            }

            // Compare the course assignments between the two solutions
            int differentAssignments = 0;
            int totalAssignments = Math.Max(solution1.Assignments.Count, solution2.Assignments.Count);

            // Create a mapping of course assignments from the first solution (course ID -> assignment)
            var solution1Map = solution1.Assignments.ToDictionary(a => a.SectionId);

            // Compare each assignment in the second solution with the corresponding assignment in the first solution
            foreach (var assignment2 in solution2.Assignments)
            {
                if (solution1Map.TryGetValue(assignment2.SectionId, out var assignment1))
                {
                    // Check if time, classroom, and teacher are the same
                    if (assignment1.TimeSlotId != assignment2.TimeSlotId ||
                        assignment1.ClassroomId != assignment2.ClassroomId ||
                        assignment1.TeacherId != assignment2.TeacherId)
                    {
                        differentAssignments++;
                    }
                }
                else
                {
                    // The first solution does not have a corresponding course assignment
                    differentAssignments++;
                }
            }

            // Add the assignments in the first solution that are not in the second solution
            var solution2SectionIds = solution2.Assignments.Select(a => a.SectionId).ToHashSet();
            differentAssignments += solution1.Assignments.Count(a => !solution2SectionIds.Contains(a.SectionId));

            // Calculate the difference ratio
            return totalAssignments > 0 ? (double)differentAssignments / totalAssignments : 0;
        }

        /// <summary>
        /// Randomly modify a solution to increase diversity
        /// </summary>
        /// <param name="solution">Original solution</param>
        /// <param name="diversityLevel">Diversity level</param>
        /// <param name="problem">Scheduling problem instance, used to check constraints</param>
        /// <returns>Diversified solution</returns>
        public SchedulingSolution DiversifySolution(SchedulingSolution solution, double diversityLevel, SchedulingProblem problem = null)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            _logger.LogInformation($"Starting to diversify solution #{solution.Id}, Diversity level: {diversityLevel:F2}");
            
            // Create a deep copy of the solution
            var newSolution = solution.Clone();

            // Determine the number of assignments to modify based on the diversity level
            int assignmentsToModify = (int)Math.Ceiling(newSolution.Assignments.Count * diversityLevel);
            _logger.LogDebug($"Plan to modify {assignmentsToModify}/{newSolution.Assignments.Count} assignments");

            // Randomly select the assignments to modify
            var assignmentsToChange = newSolution.Assignments
                .OrderBy(x => _random.Next())
                .Take(assignmentsToModify)
                .ToList();

            int modifiedCount = 0;
            
            // Modify the selected assignments
            foreach (var assignment in assignmentsToChange)
            {
                // Save the original value in case the constraint is violated
                int originalTimeSlotId = assignment.TimeSlotId;
                int originalClassroomId = assignment.ClassroomId;
                int originalTeacherId = assignment.TeacherId;

                // Try up to 10 times to find a modification that satisfies the constraint
                bool validModificationFound = false;
                for (int attempt = 0; attempt < 10 && !validModificationFound; attempt++)
                {
                    // Randomly select the modification type (time, classroom, teacher)
                    int modificationType = _random.Next(3);
                    
                    // Try to modify
                    bool modified = false;
                    
                    if (modificationType == 0 && problem?.TimeSlots != null && problem.TimeSlots.Count > 1)
                    {
                        // Modify time slot
                        var availableTimeSlots = problem.TimeSlots
                            .Where(ts => ts.Id != assignment.TimeSlotId)
                            .ToList();
                            
                        if (availableTimeSlots.Count > 0)
                        {
                            var newTimeSlot = availableTimeSlots[_random.Next(availableTimeSlots.Count)];
                            assignment.TimeSlotId = newTimeSlot.Id;
                            assignment.DayOfWeek = newTimeSlot.DayOfWeek;
                            assignment.StartTime = newTimeSlot.StartTime;
                            assignment.EndTime = newTimeSlot.EndTime;
                            modified = true;
                            _logger.LogDebug($"Modified time slot: {originalTimeSlotId} -> {newTimeSlot.Id}");
                        }
                    }
                    else if (modificationType == 1 && problem?.Classrooms != null && problem.Classrooms.Count > 1)
                    {
                        // Modify classroom
                        var courseSection = problem.CourseSections.FirstOrDefault(cs => cs.Id == assignment.SectionId);
                        var availableClassrooms = problem.Classrooms
                            .Where(c => c.Id != assignment.ClassroomId && 
                                   (courseSection == null || c.Capacity >= courseSection.Enrollment))
                            .ToList();
                            
                        if (availableClassrooms.Count > 0)
                        {
                            var newClassroom = availableClassrooms[_random.Next(availableClassrooms.Count)];
                            assignment.ClassroomId = newClassroom.Id;
                            assignment.ClassroomName = newClassroom.Name;
                            modified = true;
                            _logger.LogDebug($"Modified classroom: {originalClassroomId} -> {newClassroom.Id}");
                        }
                    }
                    else if (problem?.Teachers != null && problem.Teachers.Count > 1)
                    {
                        // Modify teacher
                        var availableTeachers = problem.Teachers
                            .Where(t => t.Id != assignment.TeacherId)
                            .ToList();
                            
                        if (availableTeachers.Count > 0)
                        {
                            var newTeacher = availableTeachers[_random.Next(availableTeachers.Count)];
                            assignment.TeacherId = newTeacher.Id;
                            assignment.TeacherName = newTeacher.Name;
                            modified = true;
                            _logger.LogDebug($"Modified teacher: {originalTeacherId} -> {newTeacher.Id}");
                        }
                    }
                    
                    if (modified)
                    {
                        // Check if the constraint is satisfied
                        bool constraintsSatisfied = CheckConstraints(newSolution, assignment, problem);
                        
                        if (constraintsSatisfied)
                        {
                            validModificationFound = true;
                            modifiedCount++;
                        }
                        else
                        {
                            // Restore original value
                            assignment.TimeSlotId = originalTimeSlotId;
                            assignment.ClassroomId = originalClassroomId;
                            assignment.TeacherId = originalTeacherId;
                            _logger.LogDebug("Modified violated constraint, restored original value");
                        }
                    }
                }
            }
            
            _logger.LogInformation($"Diversification completed, actually modified {modifiedCount}/{assignmentsToModify} assignments");
            return newSolution;
        }
        
        /// <summary>
        /// Check if the constraint is satisfied
        /// </summary>
        private bool CheckConstraints(SchedulingSolution solution, SchedulingAssignment modifiedAssignment, SchedulingProblem problem)
        {
            if (problem == null)
                return true;
                
            try
            {
                // Check teacher time conflict
                var teacherTimeConflict = solution.Assignments
                    .Where(a => a != modifiedAssignment && a.TeacherId == modifiedAssignment.TeacherId && a.TimeSlotId == modifiedAssignment.TimeSlotId)
                    .Any();
                    
                if (teacherTimeConflict)
                {
                    _logger.LogDebug($"Teacher time conflict found: Teacher {modifiedAssignment.TeacherId} has another course at time slot {modifiedAssignment.TimeSlotId}");
                    return false;
                }
                
                // Check classroom time conflict
                var roomTimeConflict = solution.Assignments
                    .Where(a => a != modifiedAssignment && a.ClassroomId == modifiedAssignment.ClassroomId && a.TimeSlotId == modifiedAssignment.TimeSlotId)
                    .Any();
                    
                if (roomTimeConflict)
                {
                    _logger.LogDebug($"Classroom time conflict found: Classroom {modifiedAssignment.ClassroomId} has another course at time slot {modifiedAssignment.TimeSlotId}");
                    return false;
                }
                
                // Check teacher availability
                var teacherAvailability = problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == modifiedAssignment.TeacherId && ta.TimeSlotId == modifiedAssignment.TimeSlotId);
                    
                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                {
                    _logger.LogDebug($"Teacher {modifiedAssignment.TeacherId} is not available at time slot {modifiedAssignment.TimeSlotId}");
                    return false;
                }
                
                // Check classroom availability
                var roomAvailability = problem.ClassroomAvailabilities
                    .FirstOrDefault(ca => ca.ClassroomId == modifiedAssignment.ClassroomId && ca.TimeSlotId == modifiedAssignment.TimeSlotId);
                    
                if (roomAvailability != null && !roomAvailability.IsAvailable)
                {
                    _logger.LogDebug($"Classroom {modifiedAssignment.ClassroomId} is not available at time slot {modifiedAssignment.TimeSlotId}");
                    return false;
                }
                
                // Check classroom capacity
                var courseSection = problem.CourseSections.FirstOrDefault(cs => cs.Id == modifiedAssignment.SectionId);
                var classroom = problem.Classrooms.FirstOrDefault(c => c.Id == modifiedAssignment.ClassroomId);
                
                if (courseSection != null && classroom != null && classroom.Capacity < courseSection.Enrollment)
                {
                    _logger.LogDebug($"Classroom {classroom.Id} capacity {classroom.Capacity} is insufficient to accommodate {courseSection.Enrollment} students");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during constraint check");
                return false;
            }
        }
    }
} 