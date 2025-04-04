using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Constraints.Hard
{
    public class TeacherConflictConstraint : IConstraint
    {
        public int Id { get; } = 1;
        public string Name { get; } = "Teacher Time Conflict";
        public string Description { get; } = "A teacher cannot teach two different courses at the same time";
        public bool IsHard { get; } = true;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 1.0;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level1_Hard;
        public string Category => "Scheduling Logic";
        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();

            // 按教师ID分组
            var teacherGroups = solution.Assignments.GroupBy(a => a.TeacherId);

            foreach (var group in teacherGroups)
            {
                var teacherId = group.Key;
                var assignments = group.ToList();

                // 检查每对分配是否有时间冲突
                for (int i = 0; i < assignments.Count; i++)
                {
                    for (int j = i + 1; j < assignments.Count; j++)
                    {
                        var a1 = assignments[i];
                        var a2 = assignments[j];

                        // 如果两个分配使用了相同的时间槽，则有冲突
                        if (a1.TimeSlotId == a2.TimeSlotId)
                        {
                            // 检查是否有重叠的教学周
                            var weekOverlap = a1.WeekPattern.Intersect(a2.WeekPattern).Any();

                            if (weekOverlap)
                            {
                                conflicts.Add(new SchedulingConflict
                                {
                                    ConstraintId = Id,
                                    Type = SchedulingConflictType.TeacherConflict,
                                    Description = $"Teacher {a1.TeacherName} has two courses scheduled at the same time: {a1.SectionCode} and {a2.SectionCode}",
                                    Severity = ConflictSeverity.Critical,
                                    InvolvedEntities = new Dictionary<string, List<int>>
                                    {
                                        { "Teachers", new List<int> { teacherId } },
                                        { "Sections", new List<int> { a1.SectionId, a2.SectionId } }
                                    },
                                    InvolvedTimeSlots = new List<int> { a1.TimeSlotId }
                                });
                            }
                        }
                    }
                }
            }

            // 如果没有冲突，得分为1，否则为0
            double score = conflicts.Count == 0 ? 1.0 : 0.0;

            return (score, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}