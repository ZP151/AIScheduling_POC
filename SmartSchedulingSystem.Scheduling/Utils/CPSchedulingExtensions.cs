using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Utils
{
    /// <summary>
    /// 为约束规划模型提供扩展方法，简化约束规划阶段的模型构建和约束添加
    /// </summary>
    public static class CPSchedulingExtensions
    {
        /// <summary>
        /// 向模型添加每门课程必须且只能分配一次的约束
        /// </summary>
        public static void AddOneCourseOneAssignmentConstraints(this CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            foreach (var course in problem.CourseSections)
            {
                // 收集与此课程相关的所有分配变量
                var courseVars = variables
                    .Where(kv => kv.Key.StartsWith($"c{course.Id}_"))
                    .Select(kv => kv.Value)
                    .ToList();

                // 确保每门课程恰好分配一次
                model.Add(LinearExpr.Sum(courseVars) == 1);
            }
        }

        /// <summary>
        /// 向模型添加教室冲突约束(同一时间一个教室只能安排一门课)
        /// </summary>
        public static void AddClassroomConflictConstraints(this CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            foreach (var room in problem.Classrooms)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 收集在此时间段使用此教室的所有变量
                    var conflictingVars = variables
                        .Where(kv => kv.Key.Contains($"_t{timeSlot.Id}_r{room.Id}_"))
                        .Select(kv => kv.Value)
                        .ToList();

                    // 确保在此时间段此教室最多分配一门课
                    if (conflictingVars.Count > 0)
                    {
                        model.Add(LinearExpr.Sum(conflictingVars) <= 1);
                    }
                }
            }
        }

        /// <summary>
        /// 向模型添加教师冲突约束(同一时间一个教师只能教一门课)
        /// </summary>
        public static void AddTeacherConflictConstraints(this CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            foreach (var teacher in problem.Teachers)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 收集在此时间段由此教师教授的所有变量
                    var conflictingVars = variables
                        .Where(kv => kv.Key.Contains($"_t{timeSlot.Id}_") && kv.Key.EndsWith($"_f{teacher.Id}"))
                        .Select(kv => kv.Value)
                        .ToList();

                    // 确保在此时间段此教师最多教授一门课
                    if (conflictingVars.Count > 0)
                    {
                        model.Add(LinearExpr.Sum(conflictingVars) <= 1);
                    }
                }
            }
        }
    }
}