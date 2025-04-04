// 2. 课程先决条件约束 - 硬约束
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Hard
{
    public class PrerequisiteConstraint : IConstraint
    {
        private readonly Dictionary<int, List<int>> _prerequisites; // 课程ID -> 先修课程ID列表
        private readonly Dictionary<int, int> _courseSectionMap; // 班级ID -> 课程ID

        public int Id { get; } = 5;
        public string Name { get; } = "Course Prerequisites";
        public string Description { get; } = "Ensures prerequisite courses are scheduled before advanced courses";
        public bool IsHard { get; } = true;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 1.0;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level1_Hard;
        public string Category => "Course Logic";

        public PrerequisiteConstraint(
            Dictionary<int, List<int>> prerequisites,
            Dictionary<int, int> courseSectionMap)
        {
            _prerequisites = prerequisites ?? throw new ArgumentNullException(nameof(prerequisites));
            _courseSectionMap = courseSectionMap ?? throw new ArgumentNullException(nameof(courseSectionMap));
        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();

            // 为每个班级找到对应的课程ID
            var sectionCourseMap = new Dictionary<int, int>();
            foreach (var assignment in solution.Assignments)
            {
                if (_courseSectionMap.TryGetValue(assignment.SectionId, out int courseId))
                {
                    sectionCourseMap[assignment.SectionId] = courseId;
                }
            }

            // 检查先修课程约束
            foreach (var assignment in solution.Assignments)
            {
                // 获取班级对应的课程ID
                if (sectionCourseMap.TryGetValue(assignment.SectionId, out int courseId))
                {
                    // 检查此课程是否有先修课程
                    if (_prerequisites.TryGetValue(courseId, out List<int> prereqCourseIds))
                    {
                        foreach (var prereqCourseId in prereqCourseIds)
                        {
                            // 获取先修课程的班级
                            var prereqSectionIds = _courseSectionMap
                                .Where(kvp => kvp.Value == prereqCourseId)
                                .Select(kvp => kvp.Key)
                                .ToList();

                            // 获取先修课程班级的安排
                            var prereqAssignments = solution.Assignments
                                .Where(a => prereqSectionIds.Contains(a.SectionId))
                                .ToList();

                            // 如果先修课程没有安排或者安排在同一时段，则有冲突
                            if (!prereqAssignments.Any())
                            {
                                // 先修课程没有安排，这可能是另一学期的课程，暂不考虑冲突
                                continue;
                            }

                            // 检查是否有先修课程安排在同一时间
                            bool sameTimeSlot = prereqAssignments.Any(pa => pa.TimeSlotId == assignment.TimeSlotId);

                            if (sameTimeSlot)
                            {
                                conflicts.Add(new SchedulingConflict
                                {
                                    ConstraintId = Id,
                                    Type = SchedulingConflictType.PrerequisiteConflict,
                                    Description = $"Prerequisite conflict: Course {courseId} and its prerequisite {prereqCourseId} " +
                                                 $"are scheduled at the same time",
                                    Severity = ConflictSeverity.Critical,
                                    InvolvedEntities = new Dictionary<string, List<int>>
                                    {
                                        { "Courses", new List<int> { courseId, prereqCourseId } },
                                        { "Sections", prereqSectionIds.Concat(new[] { assignment.SectionId }).ToList() }
                                    },
                                    InvolvedTimeSlots = new List<int> { assignment.TimeSlotId }
                                });
                            }
                        }
                    }
                }
            }

            // 如果没有冲突，得分为1，否则为0（硬约束）
            double score = conflicts.Count == 0 ? 1.0 : 0.0;

            return (score, conflicts);
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}