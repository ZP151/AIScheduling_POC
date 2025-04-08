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
        private readonly Dictionary<int, PrerequisiteType> _prerequisiteTypes; // 先修课程ID -> 先修类型

        public int Id { get; } = 5;
        public string Name { get; } = "Course Prerequisites";
        public string Description { get; } = "Ensures prerequisite courses are scheduled before advanced courses";
        public bool IsHard { get; } = true;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 1.0;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level1_Hard;
        public string Category => "Course Logic";

        /// <summary>
        /// 先修课程类型
        /// </summary>
        public enum PrerequisiteType
        {
            /// <summary>
            /// 上一学期先修课程（在当前学期开始前必须已修过）
            /// </summary>
            PreviousSemester,

            /// <summary>
            /// 同一学期先修课程（在同一学期中，课时安排时必须先修课排在前面）
            /// </summary>
            SameSemester
        }

        public PrerequisiteConstraint(
            Dictionary<int, List<int>> prerequisites,
            Dictionary<int, int> courseSectionMap,
            Dictionary<int, PrerequisiteType> prerequisiteTypes = null)
        {
            _prerequisites = prerequisites ?? throw new ArgumentNullException(nameof(prerequisites));
            _courseSectionMap = courseSectionMap ?? throw new ArgumentNullException(nameof(courseSectionMap));
            _prerequisiteTypes = prerequisiteTypes ?? new Dictionary<int, PrerequisiteType>();
        }

        public PrerequisiteConstraint()
        {
            _prerequisites = new Dictionary<int, List<int>>();
            _courseSectionMap = new Dictionary<int, int>();
            _prerequisiteTypes = new Dictionary<int, PrerequisiteType>();
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
                            // 获取先修课程类型，默认为上一学期先修
                            var prereqType = _prerequisiteTypes.TryGetValue(prereqCourseId, out var type)
                                ? type
                                : PrerequisiteType.PreviousSemester;

                            // 获取先修课程的班级
                            var prereqSectionIds = _courseSectionMap
                                .Where(kvp => kvp.Value == prereqCourseId)
                                .Select(kvp => kvp.Key)
                                .ToList();

                            // 获取先修课程班级的安排
                            var prereqAssignments = solution.Assignments
                                .Where(a => prereqSectionIds.Contains(a.SectionId))
                                .ToList();

                            // 根据先修课程类型处理
                            switch (prereqType)
                            {
                                case PrerequisiteType.PreviousSemester:
                                    // 对于上一学期先修课程，这些班级不应该出现在当前学期排课中
                                    // 但由于我们无法检查历史数据，此处暂时不做处理
                                    break;

                                case PrerequisiteType.SameSemester:
                                    // 对于同一学期先修课程
                                    // 1. 不能同时安排在同一时间段
                                    // 2. 理想情况下，先修课程应该排在后续课程之前的时间段

                                    // 检查是否安排在同一时间段
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

                                    // 检查时间先后顺序
                                    // 注：这部分可能需要根据实际时间槽的定义进行调整
                                    foreach (var prereqAssignment in prereqAssignments)
                                    {
                                        // 简单比较：如果先修课程的时间槽ID大于后续课程的时间槽ID，则认为顺序不合理
                                        // 这假设时间槽ID是按照时间顺序编号的
                                        if (prereqAssignment.TimeSlotId > assignment.TimeSlotId)
                                        {
                                            conflicts.Add(new SchedulingConflict
                                            {
                                                ConstraintId = Id,
                                                Type = SchedulingConflictType.CourseSequenceConflict,
                                                Description = $"Course sequence conflict: Prerequisite course {prereqCourseId} " +
                                                              $"is scheduled after its dependent course {courseId}",
                                                Severity = ConflictSeverity.Severe,
                                                InvolvedEntities = new Dictionary<string, List<int>>
                                                {
                                                    { "Courses", new List<int> { courseId, prereqCourseId } },
                                                    { "Sections", new List<int> { assignment.SectionId, prereqAssignment.SectionId } }
                                                },
                                                InvolvedTimeSlots = new List<int> { assignment.TimeSlotId, prereqAssignment.TimeSlotId }
                                            });
                                        }
                                    }
                                    break;
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
            var (score, _) = Evaluate(solution);
            return score >= 1.0;
        }
    }
}