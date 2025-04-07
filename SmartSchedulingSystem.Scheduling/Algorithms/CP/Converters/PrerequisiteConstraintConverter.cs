using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters
{
    /// <summary>
    /// 将先修课程约束转换为CP模型约束
    /// </summary>
    public class PrerequisiteConstraintConverter : ICPConstraintConverter
    {
        private readonly Dictionary<int, List<int>> _prerequisites; // 课程ID -> 先修课程ID列表
        private readonly Dictionary<int, int> _courseSectionMap; // 班级ID -> 课程ID

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

            // 获取每个课程对应的班级
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

            // 处理每个时间槽
            foreach (var timeSlot in problem.TimeSlots)
            {
                // 处理每个先修关系
                foreach (var prereqEntry in _prerequisites)
                {
                    int courseId = prereqEntry.Key;
                    var prereqCourseIds = prereqEntry.Value;

                    // 跳过没有先修课程的课程
                    if (prereqCourseIds == null || prereqCourseIds.Count == 0)
                        continue;

                    // 检查这个课程是否有班级在课表中
                    if (!courseToSections.TryGetValue(courseId, out var sectionIds) || sectionIds.Count == 0)
                        continue;

                    // 对于每个先修课程
                    foreach (var prereqCourseId in prereqCourseIds)
                    {
                        // 检查先修课程是否有班级在课表中
                        if (!courseToSections.TryGetValue(prereqCourseId, out var prereqSectionIds) || prereqSectionIds.Count == 0)
                            continue;

                        // 添加约束：当前课程和其先修课程不能在同一时间槽
                        foreach (var sectionId in sectionIds)
                        {
                            foreach (var prereqSectionId in prereqSectionIds)
                            {
                                // 找出涉及这两个班级在此时间槽的所有变量
                                var courseVars = variables
                                    .Where(kv => kv.Key.StartsWith($"c{sectionId}_t{timeSlot.Id}_"))
                                    .Select(kv => kv.Value)
                                    .ToList();

                                var prereqVars = variables
                                    .Where(kv => kv.Key.StartsWith($"c{prereqSectionId}_t{timeSlot.Id}_"))
                                    .Select(kv => kv.Value)
                                    .ToList();

                                // 如果两者都有变量，添加它们不能同时为1的约束
                                if (courseVars.Count > 0 && prereqVars.Count > 0)
                                {
                                    foreach (var courseVar in courseVars)
                                    {
                                        foreach (var prereqVar in prereqVars)
                                        {
                                            // 添加约束：courseVar + prereqVar <= 1
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