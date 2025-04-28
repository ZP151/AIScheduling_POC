using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters
{
    /// <summary>
    /// Converting prerequisite constraints to CP model constraints
    /// </summary>
    public class PrerequisiteConstraintConverter : ICPConstraintConverter
    {
        /// <summary>
        /// Get the constraint level of the constraint converter
        /// </summary>
        public Engine.ConstraintApplicationLevel ConstraintLevel => Engine.ConstraintApplicationLevel.Basic;

        private readonly Dictionary<int, List<int>> _prerequisites; // course ID -> prerequisite course ID list
        private readonly Dictionary<int, int> _courseSectionMap; // section ID -> course ID

        public PrerequisiteConstraintConverter(
            Dictionary<int, List<int>> prerequisites,
            Dictionary<int, int> courseSectionMap)
        {
            _prerequisites = prerequisites ?? throw new ArgumentNullException(nameof(prerequisites));
            _courseSectionMap = courseSectionMap ?? throw new ArgumentNullException(nameof(courseSectionMap));
        }

        public void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            // Get the sections for each course
            var courseToSections = new Dictionary<int, List<int>>();

            foreach (var kvp in _courseSectionMap)
            {
                int sectionId = kvp.Key;
                int courseId = kvp.Value;

                if (!courseToSections.ContainsKey(courseId))
                {
                    courseToSections[courseId] = new List<int>();
                }

                courseToSections[courseId].Add(sectionId);
            }

            // Process each time slot
            foreach (var timeSlot in problem.TimeSlots)
            {
                // Process each prerequisite relationship
                foreach (var prereqEntry in _prerequisites)
                {
                    int courseId = prereqEntry.Key;
                    var prereqCourseIds = prereqEntry.Value;

                    // Skip courses with no prerequisites
                    if (prereqCourseIds == null || prereqCourseIds.Count == 0)
                        continue;

                    // Check if this course has sections in the schedule
                    if (!courseToSections.TryGetValue(courseId, out var sectionIds) || sectionIds.Count == 0)
                        continue;

                    // For each prerequisite course
                    foreach (var prereqCourseId in prereqCourseIds)
                    {
                        // Check if the prerequisite course has sections in the schedule
                        if (!courseToSections.TryGetValue(prereqCourseId, out var prereqSectionIds) || prereqSectionIds.Count == 0)
                            continue;

                        // Add constraint: The current course and its prerequisite course cannot be in the same time slot
                        foreach (var sectionId in sectionIds)
                        {
                            foreach (var prereqSectionId in prereqSectionIds)
                            {
                                // Find all variables that involve these two sections in this time slot
                                var courseVars = variables
                                    .Where(kv => kv.Key.StartsWith($"c{sectionId}_t{timeSlot.Id}_"))
                                    .Select(kv => kv.Value)
                                    .ToList();

                                var prereqVars = variables
                                    .Where(kv => kv.Key.StartsWith($"c{prereqSectionId}_t{timeSlot.Id}_"))
                                    .Select(kv => kv.Value)
                                    .ToList();

                                // If both have variables, add a constraint that they cannot both be 1
                                if (courseVars.Count > 0 && prereqVars.Count > 0)
                                {
                                    foreach (var courseVar in courseVars)
                                    {
                                        foreach (var prereqVar in prereqVars)
                                        {
                                            // Add constraint: courseVar + prereqVar <= 1
                                            model.Add(courseVar + prereqVar <= 1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}