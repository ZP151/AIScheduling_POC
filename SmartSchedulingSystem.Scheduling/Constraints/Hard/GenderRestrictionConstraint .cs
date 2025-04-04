// 1. 性别限制约束 - 硬约束
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Hard
{
    public class GenderRestrictionConstraint : IConstraint
    {
        private readonly Dictionary<int, string> _sectionGenderRestrictions;

        public int Id { get; } = 4;
        public string Name { get; } = "Gender Restriction";
        public string Description { get; } = "Ensures courses are scheduled according to gender requirements";
        public bool IsHard { get; } = true;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 1.0;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level1_Hard;
        public string Category => "Administrative Rules";

        public GenderRestrictionConstraint(Dictionary<int, string> sectionGenderRestrictions)
        {
            _sectionGenderRestrictions = sectionGenderRestrictions ?? throw new ArgumentNullException(nameof(sectionGenderRestrictions));
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();

            // 查找所有有性别限制的课程分配
            foreach (var assignment in solution.Assignments)
            {
                // 如果该课程有性别限制要求
                if (_sectionGenderRestrictions.TryGetValue(assignment.SectionId, out string restriction)
                    && !string.IsNullOrEmpty(restriction))
                {
                    // 检查当前教室是否满足性别限制
                    bool isMaleOnly = restriction.Equals("Male", StringComparison.OrdinalIgnoreCase);
                    bool isFemaleOnly = restriction.Equals("Female", StringComparison.OrdinalIgnoreCase);

                    // 假设我们有一个方法来检查同一时段在同一建筑物的教室安排情况
                    var conflictingSections = GetConflictingSections(solution, assignment);

                    foreach (var conflictingSection in conflictingSections)
                    {
                        // 如果有不同性别限制的课程在同一时段同一建筑
                        if (_sectionGenderRestrictions.TryGetValue(conflictingSection.SectionId, out string otherRestriction)
                            && !string.IsNullOrEmpty(otherRestriction)
                            && !otherRestriction.Equals(restriction, StringComparison.OrdinalIgnoreCase))
                        {
                            conflicts.Add(new SchedulingConflict
                            {
                                ConstraintId = Id,
                                Type = SchedulingConflictType.GenderRestrictionConflict,
                                Description = $"Gender restriction conflict: {restriction} only section {assignment.SectionCode} " +
                                             $"and {otherRestriction} only section {conflictingSection.SectionCode} " +
                                             $"are scheduled in the same building at the same time",
                                Severity = ConflictSeverity.Critical,
                                InvolvedEntities = new Dictionary<string, List<int>>
                                {
                                    { "Sections", new List<int> { assignment.SectionId, conflictingSection.SectionId } }
                                },
                                InvolvedTimeSlots = new List<int> { assignment.TimeSlotId }
                            });
                        }
                    }
                }
            }

            // 如果没有冲突，得分为1，否则为0（硬约束）
            double score = conflicts.Count == 0 ? 1.0 : 0.0;

            return (score, conflicts);
        }

        private List<SchedulingAssignment> GetConflictingSections(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            // 获取在同一时间段、同一建筑物的其他课程
            string buildingName = assignment.ClassroomName.Split('-')[0].Trim();

            return solution.Assignments
                .Where(a => a.SectionId != assignment.SectionId
                         && a.TimeSlotId == assignment.TimeSlotId
                         && a.ClassroomName.StartsWith(buildingName))
                .ToList();
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}