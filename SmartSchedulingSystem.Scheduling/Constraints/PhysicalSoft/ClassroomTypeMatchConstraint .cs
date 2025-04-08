// 4. 教室类型匹配约束 - 软约束
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;

namespace SchedulSmartSchedulingSystemingSystem.Scheduling.Constraints.Soft
{
    public class ClassroomTypeMatchConstraint : IConstraint
    {
        private readonly Dictionary<int, string> _courseSectionTypes; // 班级ID -> 课程类型
        private readonly Dictionary<int, string> _classroomTypes; // 教室ID -> 教室类型

        public int Id { get; } = 7;
        public string Name { get; } = "Classroom Type Match";
        public string Description { get; } = "Ensures courses are scheduled in appropriate type of classrooms";
        public bool IsHard { get; } = false;
        public bool IsActive { get; set; } = true;
        public double Weight { get; set; } = 0.7;
        public ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level2_PhysicalSoft;
        public string Category => "Physical Resources";

        public ClassroomTypeMatchConstraint(
            Dictionary<int, string> courseSectionTypes,
            Dictionary<int, string> classroomTypes)
        {
            _courseSectionTypes = courseSectionTypes ?? throw new ArgumentNullException(nameof(courseSectionTypes));
            _classroomTypes = classroomTypes ?? throw new ArgumentNullException(nameof(classroomTypes));
        }

        public ClassroomTypeMatchConstraint()
        {
            _courseSectionTypes = new Dictionary<int, string>();
            _classroomTypes = new Dictionary<int, string>();

        }

        public (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var conflicts = new List<SchedulingConflict>();
            int totalAssignments = 0;
            int matchingAssignments = 0;

            foreach (var assignment in solution.Assignments)
            {
                totalAssignments++;

                // 获取课程类型和教室类型
                bool hasCourseType = _courseSectionTypes.TryGetValue(assignment.SectionId, out string courseType);
                bool hasRoomType = _classroomTypes.TryGetValue(assignment.ClassroomId, out string classroomType);

                if (hasCourseType && hasRoomType)
                {
                    bool isMatching = IsTypeMatching(courseType, classroomType);

                    if (isMatching)
                    {
                        matchingAssignments++;
                    }
                    else
                    {
                        // 不匹配，添加冲突
                        conflicts.Add(new SchedulingConflict
                        {
                            ConstraintId = Id,
                            Type = SchedulingConflictType.ClassroomTypeMismatch,
                            Description = $"Classroom type mismatch: Course {assignment.SectionCode} of type '{courseType}' " +
                                         $"is scheduled in classroom {assignment.ClassroomName} of type '{classroomType}'",
                            Severity = ConflictSeverity.Moderate,
                            InvolvedEntities = new Dictionary<string, List<int>>
                            {
                                { "Sections", new List<int> { assignment.SectionId } },
                                { "Classrooms", new List<int> { assignment.ClassroomId } }
                            }
                        });
                    }
                }
                else
                {
                    // 如果没有类型信息，假设匹配（避免过度惩罚）
                    matchingAssignments++;
                }
            }

            // 计算匹配率作为得分
            double score = totalAssignments > 0 ? (double)matchingAssignments / totalAssignments : 1.0;

            return (score, conflicts);
        }

        private bool IsTypeMatching(string courseType, string classroomType)
        {
            // 实验课必须在实验室
            if (courseType.Contains("Lab") && !classroomType.Contains("Lab"))
                return false;

            // 计算机课必须在计算机房
            if (courseType.Contains("Computer") && !classroomType.Contains("Computer"))
                return false;

            // 大课需要大教室
            if (courseType.Contains("Large") && !classroomType.Contains("Large"))
                return false;

            // 小组讨论需要讨论室
            if (courseType.Contains("Discussion") && !classroomType.Contains("Discussion"))
                return false;

            // 普通课程可以在普通教室或更好的教室
            if (courseType.Contains("Regular"))
                return true;

            // 默认认为匹配
            return true;
        }

        public bool IsSatisfied(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}