using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 将教室冲突约束转换为CP模型约束
    /// </summary>
    public class ClassroomConflictConstraintConverter : ICPConstraintConverter
    {
        public void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            // 遍历所有教室
            foreach (var classroom in problem.Classrooms)
            {
                // 遍历所有时间槽
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 找出所有在该时间槽使用该教室的变量
                    var conflictingVars = variables
                        .Where(kv => kv.Key.Contains($"_t{timeSlot.Id}_r{classroom.Id}_"))
                        .Select(kv => kv.Value)
                        .ToList();

                    // 如果有多个变量，添加约束确保最多只有一个为1（同一时间最多一门课在该教室）
                    if (conflictingVars.Count > 1)
                    {
                        model.Add(LinearExpr.Sum(conflictingVars) <= 1);
                    }
                }
            }

            // 处理教室可用性约束
            foreach (var availability in problem.ClassroomAvailabilities)
            {
                if (!availability.IsAvailable)
                {
                    // 找出在不可用时间段使用该教室的所有变量
                    var unavailableVars = variables
                        .Where(kv => kv.Key.Contains($"_t{availability.TimeSlotId}_r{availability.ClassroomId}_"))
                        .Select(kv => kv.Value)
                        .ToList();

                    // 添加约束确保这些变量都为0（不使用不可用的教室）
                    foreach (var variable in unavailableVars)
                    {
                        model.Add(variable == 0);
                    }
                }
            }

            // 处理教室容量约束（作为硬约束）
            foreach (var course in problem.CourseSections)
            {
                foreach (var classroom in problem.Classrooms)
                {
                    // 如果教室容量小于课程预期学生数，禁止分配
                    if (classroom.Capacity < course.Enrollment)
                    {
                        // 找出所有该课程使用该教室的变量
                        var invalidAssignments = variables
                            .Where(kv => kv.Key.StartsWith($"c{course.Id}_") &&
                                       kv.Key.Contains($"_r{classroom.Id}_"))
                            .Select(kv => kv.Value)
                            .ToList();

                        // 添加约束确保这些变量都为0
                        foreach (var variable in invalidAssignments)
                        {
                            model.Add(variable == 0);
                        }
                    }
                }
            }

            // 处理教室类型约束（实验课必须在实验室等）
            foreach (var course in problem.CourseSections)
            {
                if (!string.IsNullOrEmpty(course.RequiredRoomType) &&
                    course.RequiredRoomType.Contains("lab", StringComparison.OrdinalIgnoreCase))
                {
                    // 找出所有非实验室教室
                    var nonLabRooms = problem.Classrooms
                        .Where(cr => !cr.Type.Contains("lab", StringComparison.OrdinalIgnoreCase))
                        .Select(cr => cr.Id)
                        .ToList();

                    // 禁止将实验课安排在非实验室
                    foreach (var roomId in nonLabRooms)
                    {
                        var invalidVars = variables
                            .Where(kv => kv.Key.StartsWith($"c{course.Id}_") &&
                                       kv.Key.Contains($"_r{roomId}_"))
                            .Select(kv => kv.Value)
                            .ToList();

                        foreach (var variable in invalidVars)
                        {
                            model.Add(variable == 0);
                        }
                    }
                }
            }
        }
    }
}