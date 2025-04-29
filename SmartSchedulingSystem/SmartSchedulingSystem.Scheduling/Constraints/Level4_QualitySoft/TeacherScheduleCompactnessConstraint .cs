using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level4_QualitySoft
{
    public class TeacherScheduleCompactnessConstraint : IConstraint
    {
        public int Id { get; } = 6;
        public string Name { get; } = "Teacher Schedule Compactness";
        public string Description { get; } = "Ensures consecutive teaching where appropriate";
        public bool IsHard { get; } = false;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 0.7;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level4_QualitySoft;
        public string Category => "Preference";
        public string DefinitionId => "TeacherScheduleCompactnessConstraint";
        public string BasicRule => "TeacherComfort";

        private readonly int _maxConsecutiveHours;

        public TeacherScheduleCompactnessConstraint(int maxConsecutiveHours = 3)
        {
            _maxConsecutiveHours = maxConsecutiveHours;
        }

        public TeacherScheduleCompactnessConstraint()
        {
            _maxConsecutiveHours = 3; // Default value
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            var conflicts = new List<SchedulingConflict>();
            int totalTeachers = 0;
            int totalDays = 0;
            int optimalDays = 0;

            var teacherGroups = solution.Assignments
                .GroupBy(a => a.TeacherId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (teacherId, assignments) in teacherGroups)
            {
                totalTeachers++;

                var dailyGroups = assignments
                    .GroupBy(a => a.DayOfWeek)
                    .ToDictionary(g => g.Key, g => g.OrderBy(a => a.StartTime).ToList());

                foreach (var (day, dayAssignments) in dailyGroups)
                {
                    totalDays++;

                    int maxConsecutive = 1;
                    int currentConsecutive = 1;

                    for (int i = 1; i < dayAssignments.Count; i++)
                    {
                        var prev = dayAssignments[i - 1];
                        var curr = dayAssignments[i];

                        if ((curr.StartTime - prev.EndTime).TotalMinutes <= 15)
                            currentConsecutive++;
                        else
                            currentConsecutive = 1;

                        maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
                    }

                    if (maxConsecutive >= _maxConsecutiveHours)
                    {
                        optimalDays++;
                    }
                    else
                    {
                        conflicts.Add(new SchedulingConflict
                        {
                            ConstraintId = Id,
                            Type = SchedulingConflictType.Other,
                            Description = $"Teacher {assignments.First().TeacherName} has non-compact schedule on Day {day}.",
                            Severity = ConflictSeverity.Minor,
                            InvolvedEntities = new Dictionary<string, List<int>>
                    {
                        { "Teachers", new List<int> { teacherId } }
                    }
                        });
                    }
                }
            }

            double score = totalDays > 0 ? (double)optimalDays / totalDays : 1.0;
            return (score, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}
