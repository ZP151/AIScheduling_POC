using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Constraints.QualitySoft
{
    public class TeacherWorkloadConstraint : IConstraint
    {
        public int Id { get; } = 5;
        public string Name { get; } = "Teacher Workload";
        public string Description { get; } = "Ensures teacher workload is within acceptable limits";
        public bool IsHard { get; } = false;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 0.8;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level3_QualitySoft;
        public string Category => "Resource Utilization";

        private readonly Dictionary<int, int> _maxWeeklyHours;
        private readonly Dictionary<int, int> _maxDailyHours;

        public TeacherWorkloadConstraint(Dictionary<int, int> maxWeeklyHours, Dictionary<int, int> maxDailyHours)
        {
            _maxWeeklyHours = maxWeeklyHours;
            _maxDailyHours = maxDailyHours;
        }

        public TeacherWorkloadConstraint()
        {
            _maxWeeklyHours = new Dictionary<int, int>();
            _maxDailyHours = new Dictionary<int, int>();
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            var conflicts = new List<SchedulingConflict>();
            var teacherGroups = solution.Assignments.GroupBy(a => a.TeacherId);

            int compliantCount = 0;
            int totalTeachers = 0;

            foreach (var group in teacherGroups)
            {
                int teacherId = group.Key;
                totalTeachers++;

                var assignments = group.ToList();

                int totalHours = assignments.Count * 2; // 假设每节课2学时

                int maxPerDay = assignments
                    .GroupBy(a => a.DayOfWeek)
                    .Select(g => g.Count() * 2)
                    .DefaultIfEmpty(0)
                    .Max();

                _maxWeeklyHours.TryGetValue(teacherId, out var weeklyLimit);
                _maxDailyHours.TryGetValue(teacherId, out var dailyLimit);

                bool isCompliant = (weeklyLimit == 0 || totalHours <= weeklyLimit) &&
                                   (dailyLimit == 0 || maxPerDay <= dailyLimit);

                if (!isCompliant)
                {
                    conflicts.Add(new SchedulingConflict
                    {
                        ConstraintId = Id,
                        Type = SchedulingConflictType.TeacherWorkloadExceeded,
                        Description = $"Teacher {assignments.First().TeacherName} exceeds workload limit.",
                        Severity = ConflictSeverity.Moderate,
                        InvolvedEntities = new Dictionary<string, List<int>>
                {
                    { "Teachers", new List<int> { teacherId } }
                }
                    });
                }
                else
                {
                    compliantCount++;
                }
            }

            double score = totalTeachers > 0 ? (double)compliantCount / totalTeachers : 1.0;
            return (score, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}
