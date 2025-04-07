using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters
{
    /// <summary>
    /// 将教师可用性约束转换为CP模型约束
    /// </summary>
    public class TeacherAvailabilityConstraintConverter : ICPConstraintConverter
    {
        public void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            // 处理教师不可用时间段
            foreach (var availability in problem.TeacherAvailabilities)
            {
                // 只处理教师不可用的时间段
                if (!availability.IsAvailable)
                {
                    // 找出在不可用时间段安排该教师的所有变量
                    var unavailableVars = variables
                        .Where(kv => kv.Key.Contains($"_t{availability.TimeSlotId}_") &&
                                    kv.Key.EndsWith($"_f{availability.TeacherId}"))
                        .Select(kv => kv.Value)
                        .ToList();

                    // 添加约束确保这些变量都为0（教师不在不可用时段教课）
                    foreach (var variable in unavailableVars)
                    {
                        model.Add(variable == 0);
                    }
                }
            }

            // 处理教师可授课程约束
            foreach (var teacher in problem.Teachers)
            {
                // 获取该教师可以教授的课程列表
                var teachableCourses = problem.TeacherCoursePreferences
                    .Where(tcp => tcp.TeacherId == teacher.Id && tcp.ProficiencyLevel >= 2)
                    .Select(tcp => tcp.CourseId)
                    .ToHashSet();

                // 找出所有课程
                var allCourseIds = problem.CourseSections.Select(c => c.CourseId).Distinct().ToList();

                // 找出该教师不能教授的课程
                var nonTeachableCourses = allCourseIds.Where(id => !teachableCourses.Contains(id)).ToList();

                // 对于每门教师不能教授的课程
                foreach (var courseId in nonTeachableCourses)
                {
                    // 找出该教师教授不能教授课程的所有班级
                    var sectionIds = problem.CourseSections
                        .Where(cs => cs.CourseId == courseId)
                        .Select(cs => cs.Id)
                        .ToList();

                    foreach (var sectionId in sectionIds)
                    {
                        // 找出该教师教授该班级的所有变量
                        var invalidVars = variables
                            .Where(kv => kv.Key.StartsWith($"c{sectionId}_") &&
                                       kv.Key.EndsWith($"_f{teacher.Id}"))
                            .Select(kv => kv.Value)
                            .ToList();

                        // 添加约束确保这些变量都为0
                        foreach (var variable in invalidVars)
                        {
                            model.Add(variable == 0);
                        }
                    }
                }
            }

            // 处理教师工作量约束
            foreach (var teacher in problem.Teachers)
            {
                // 如果教师有最大周课时限制
                if (teacher.MaxWeeklyHours > 0)
                {
                    // 计算一节课的课时数（假设为2学时）
                    int hoursPerSession = 2;

                    // 找出该教师教授的所有课程变量
                    var teacherVars = variables
                        .Where(kv => kv.Key.EndsWith($"_f{teacher.Id}"))
                        .Select(kv => kv.Value)
                        .ToList();

                    // 添加约束：总课时数不超过最大周课时
                    if (teacherVars.Count > 0)
                    {
                        model.Add(LinearExpr.Sum(teacherVars) * hoursPerSession <= teacher.MaxWeeklyHours);
                    }
                }

                // 如果教师有最大日课时限制
                if (teacher.MaxDailyHours > 0)
                {
                    // 按天分组时间槽
                    var dayTimeSlots = problem.TimeSlots
                        .GroupBy(ts => ts.DayOfWeek)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // 对每一天添加约束
                    foreach (var dayGroup in dayTimeSlots)
                    {
                        var dayOfWeek = dayGroup.Key;
                        var timeSlots = dayGroup.Value;

                        // 找出该教师在该天的所有课程变量
                        var dayVars = new List<IntVar>();
                        foreach (var timeSlot in timeSlots)
                        {
                            var vars = variables
                                .Where(kv => kv.Key.Contains($"_t{timeSlot.Id}_") &&
                                           kv.Key.EndsWith($"_f{teacher.Id}"))
                                .Select(kv => kv.Value);

                            dayVars.AddRange(vars);
                        }

                        // 添加约束：单日课时数不超过最大日课时
                        if (dayVars.Count > 0)
                        {
                            model.Add(LinearExpr.Sum(dayVars) * 2 <= teacher.MaxDailyHours);
                        }
                    }
                }
            }
        }
    }
}