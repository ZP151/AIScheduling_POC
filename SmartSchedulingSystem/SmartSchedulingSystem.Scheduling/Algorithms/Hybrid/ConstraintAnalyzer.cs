using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Engine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.Hybrid
{
    /// <summary>
    /// Analyzes constraint satisfaction of solutions, used to guide local search
    /// </summary>
    public class ConstraintAnalyzer
    {
        private readonly ILogger<ConstraintAnalyzer> _logger;
        private readonly ConstraintManager _constraintManager;
        private readonly Random _random = new Random();

        public ConstraintAnalyzer(
            ILogger<ConstraintAnalyzer> logger,
            ConstraintManager constraintManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
        }

        /// <summary>
        /// Analyze constraint satisfaction of a solution
        /// </summary>
        public ConstraintAnalysisResult AnalyzeSolution(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            try
            {
                _logger.LogDebug("Starting to analyze constraint satisfaction");

                var result = new ConstraintAnalysisResult();

                // Get all soft constraints
                var softConstraints = _constraintManager.GetSoftConstraints();

                // Analyze satisfaction of each constraint
                foreach (var constraint in softConstraints)
                {
                    try
                    {
                        var (score, conflicts) = constraint.Evaluate(solution);

                        // Record satisfaction and conflicts
                        result.ConstraintSatisfaction[constraint] = score;
                        result.ConstraintConflicts[constraint] = conflicts;

                        _logger.LogDebug($"Constraint '{constraint.Name}' satisfaction: {score:F4}, conflict count: {conflicts?.Count ?? 0}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error evaluating constraint '{constraint.Name}'");
                        // Mark constraint with error as low satisfaction, prioritize optimization
                        result.ConstraintSatisfaction[constraint] = 0.0;
                        result.ConstraintConflicts[constraint] = new List<SchedulingConflict>();
                    }
                }

                _logger.LogDebug($"Constraint analysis completed, total {softConstraints.Count} soft constraints");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing constraint satisfaction");
                throw;
            }
        }
    }

    /// <summary>
    /// Represents constraint analysis results
    /// </summary>
    public class ConstraintAnalysisResult
    {
        /// <summary>
        /// Satisfaction level of each constraint (0-1)
        /// </summary>
        public Dictionary<IConstraint, double> ConstraintSatisfaction { get; } = new Dictionary<IConstraint, double>();

        /// <summary>
        /// Conflicts of each constraint
        /// </summary>
        public Dictionary<IConstraint, List<SchedulingConflict>> ConstraintConflicts { get; } =
            new Dictionary<IConstraint, List<SchedulingConflict>>();

        /// <summary>
        /// Get the constraint with lowest satisfaction
        /// </summary>
        public IConstraint GetWeakestConstraint()
        {
            if (ConstraintSatisfaction.Count == 0)
            {
                return null;
            }

            // Calculate optimization priority based on satisfaction and weight
            var prioritizedConstraints = ConstraintSatisfaction
                .Select(kv => new
                {
                    Constraint = kv.Key,
                    // Calculate weighted priority: (1-satisfaction) * weight
                    // Lower satisfaction and higher weight means higher priority
                    Priority = (1.0 - kv.Value) * kv.Key.Weight
                })
                .Where(item => item.Priority > 0) // Only consider constraints not fully satisfied
                .OrderByDescending(item => item.Priority)
                .ToList();

            if (prioritizedConstraints.Count == 0)
            {
                return null; // All constraints fully satisfied
            }

            // Randomly select from top 3 highest priority constraints
            // This avoids always optimizing the same constraint, increasing search diversity
            int selectCount = Math.Min(3, prioritizedConstraints.Count);

            if (selectCount == 1)
            {
                return prioritizedConstraints[0].Constraint;
            }
            else
            {
                int randomIndex = new Random().Next(selectCount);
                return prioritizedConstraints[randomIndex].Constraint;
            }
        }

        /// <summary>
        /// Get course assignments affected by specified constraint
        /// </summary>
        public List<SchedulingAssignment> GetAssignmentsAffectedByConstraint(
            SchedulingSolution solution,
            IConstraint constraint)
        {
            if (constraint == null || solution == null || solution.Assignments.Count == 0)
            {
                return new List<SchedulingAssignment>();
            }

            // If constraint has no conflicts, randomly select assignments
            if (!ConstraintConflicts.TryGetValue(constraint, out var conflicts) ||
                conflicts == null ||
                conflicts.Count == 0)
            {
                // If no clear conflict information, randomly select 2-3 assignments
                int count = Math.Min(3, solution.Assignments.Count);

                return solution.Assignments
                    .OrderBy(a => Guid.NewGuid())
                    .Take(count)
                    .ToList();
            }

            // Get all assignment IDs involved in conflicts
            var affectedSectionIds = new HashSet<int>();
            var affectedTeacherIds = new HashSet<int>();
            var affectedClassroomIds = new HashSet<int>();
            var affectedTimeSlotIds = new HashSet<int>();

            foreach (var conflict in conflicts)
            {
                // Collect all entity IDs involved in conflicts
                if (conflict.InvolvedEntities != null)
                {
                    if (conflict.InvolvedEntities.TryGetValue("Sections", out var sections))
                        foreach (var id in sections)
                            affectedSectionIds.Add(id);

                    if (conflict.InvolvedEntities.TryGetValue("Teachers", out var teachers))
                        foreach (var id in teachers)
                            affectedTeacherIds.Add(id);

                    if (conflict.InvolvedEntities.TryGetValue("Classrooms", out var classrooms))
                        foreach (var id in classrooms)
                            affectedClassroomIds.Add(id);
                }

                // Collect involved time slots
                if (conflict.InvolvedTimeSlots != null)
                    foreach (var id in conflict.InvolvedTimeSlots)
                        affectedTimeSlotIds.Add(id);
            }

            // Find all assignments related to these IDs
            var affectedAssignments = solution.Assignments
                .Where(a =>
                    affectedSectionIds.Contains(a.SectionId) ||
                    affectedTeacherIds.Contains(a.TeacherId) ||
                    affectedClassroomIds.Contains(a.ClassroomId) ||
                    affectedTimeSlotIds.Contains(a.TimeSlotId))
                .ToList();

            // If no related assignments found, randomly select
            if (affectedAssignments.Count == 0)
            {
                int count = Math.Min(3, solution.Assignments.Count);

                return solution.Assignments
                    .OrderBy(a => Guid.NewGuid())
                    .Take(count)
                    .ToList();
            }

            return affectedAssignments;
        }
    }
}