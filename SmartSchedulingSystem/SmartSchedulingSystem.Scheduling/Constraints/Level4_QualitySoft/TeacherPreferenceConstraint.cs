﻿using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level4_QualitySoft
{
    public class TeacherPreferenceConstraint : IConstraint
    {
        private readonly Dictionary<(int TeacherId, int TimeSlotId), int> _preferences;

        public int Id { get; } = 4;
        public string Name { get; } = "Teacher Time Preference";
        public string Description { get; } = "Respect teacher preferences for teaching times";
        public bool IsHard { get; } = false;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 0.7;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level4_QualitySoft;
        public string Category => "Preference";
        
        // Add missing properties
        public string DefinitionId => "TeacherPreferenceConstraint";
        public string BasicRule => "TeacherPreference";
        
        public TeacherPreferenceConstraint(Dictionary<(int TeacherId, int TimeSlotId), int> preferences)
        {
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        }

        public TeacherPreferenceConstraint()
        {
            _preferences = new Dictionary<(int TeacherId, int TimeSlotId), int>();
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();
            double totalScore = 0;
            int evaluated = 0;

            foreach (var assignment in solution.Assignments)
            {
                var key = (assignment.TeacherId, assignment.TimeSlotId);

                if (_preferences.TryGetValue(key, out int preference))
                {
                    evaluated++;

                    // Preference levels range from 1-5, 5 being highest
                    // Map 1-5 to 0.2-1.0
                    double preferenceScore = preference / 5.0;

                    // If preference level is below 3, add conflict
                    if (preference < 3)
                    {
                        conflicts.Add(new SchedulingConflict
                        {
                            ConstraintId = Id,
                            Type = SchedulingConflictType.TeacherAvailabilityConflict,
                            Description = $"Teacher {assignment.TeacherName} has low preference (level {preference}) for timeslot used by course {assignment.SectionCode}",
                            Severity = preference < 2 ? ConflictSeverity.Moderate : ConflictSeverity.Minor,
                            InvolvedEntities = new Dictionary<string, List<int>>
                            {
                                { "Teachers", new List<int> { assignment.TeacherId } },
                                { "Sections", new List<int> { assignment.SectionId } }
                            },
                            InvolvedTimeSlots = new List<int> { assignment.TimeSlotId }
                        });
                    }

                    totalScore += preferenceScore;
                }
                else
                {
                    // If no preference record exists, assume neutral preference (0.6 points)
                    evaluated++;
                    totalScore += 0.6;
                }
            }

            // Calculate average score
            double averageScore = evaluated > 0 ? totalScore / evaluated : 1.0;

            return (averageScore, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}