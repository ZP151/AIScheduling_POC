using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 将教师冲突约束转换为CP模型约束
    /// </summary>
    public class TeacherConflictConstraintConverter : ICPConstraintConverter
    {
        public void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));  
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            // 遍历所有教师
            foreach (var teacher in problem.Teachers)
            {
                // 遍历所有时间槽
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 找出所有在该时间槽由该教师教授的变量
                    var conflictingVars = variables
                        .Where(kv => kv.Key.Contains($"_t{timeSlot.Id}_") &&
                                    kv.Key.EndsWith($"_f{teacher.Id}"))
                        .Select(kv => kv.Value)
                        .ToList();

                    // 如果有多个变量，添加约束确保最多只有一个为1（教师在同一时间段最多教一门课）
                    if (conflictingVars.Count > 1)
                    {
                        model.Add(LinearExpr.Sum(conflictingVars) <= 1);
                    }
                }
            }

            // 考虑连续时间槽的教师可用性（例如，如果教师需要在不同校区间移动）
            // 这里假设problem.TimeSlots是按时间顺序排列的
            var orderedTimeSlots = problem.TimeSlots.OrderBy(ts => ts.DayOfWeek).ThenBy(ts => ts.StartTime).ToList();

            for (int i = 0; i < orderedTimeSlots.Count - 1; i++)
            {
                var currentSlot = orderedTimeSlots[i];
                var nextSlot = orderedTimeSlots[i + 1];

                // 只处理同一天的连续时间槽
                if (currentSlot.DayOfWeek == nextSlot.DayOfWeek)
                {
                    // 计算两个时间槽之间的间隔（分钟）
                    var interval = (nextSlot.StartTime - currentSlot.EndTime).TotalMinutes;

                    // 如果间隔太短（比如少于15分钟），添加约束防止教师在不同建筑/校区连续上课
                    if (interval < 15)
                    {
                        foreach (var teacher in problem.Teachers)
                        {
                            // 找出教师在当前时间槽的所有变量
                            var currentSlotVars = variables
                                .Where(kv => kv.Key.Contains($"_t{currentSlot.Id}_") &&
                                           kv.Key.EndsWith($"_f{teacher.Id}"))
                                .ToList();

                            // 找出教师在下一个时间槽的所有变量
                            var nextSlotVars = variables
                                .Where(kv => kv.Key.Contains($"_t{nextSlot.Id}_") &&
                                           kv.Key.EndsWith($"_f{teacher.Id}"))
                                .ToList();

                            // 对每对可能的分配组合，检查它们是否在不同建筑/校区
                            foreach (var currentVar in currentSlotVars)
                            {
                                string currentKey = currentVar.Key;
                                int currentRoomId = ExtractRoomId(currentKey);

                                foreach (var nextVar in nextSlotVars)
                                {
                                    string nextKey = nextVar.Key;
                                    int nextRoomId = ExtractRoomId(nextKey);

                                    // 如果在不同建筑/校区，添加约束防止同时分配
                                    if (AreRoomsInDifferentBuildings(currentRoomId, nextRoomId, problem))
                                    {
                                        // 如果两个变量都为1，则违反约束，所以两者之和最多为1
                                        model.Add(currentVar.Value + nextVar.Value <= 1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从变量名称中提取教室ID
        /// </summary>
        private int ExtractRoomId(string variableName)
        {
            // 变量名格式: c{courseId}_t{timeSlotId}_r{roomId}_f{teacherId}
            var parts = variableName.Split('_');
            if (parts.Length >= 3 && parts[2].StartsWith("r"))
            {
                if (int.TryParse(parts[2].Substring(1), out int roomId))
                {
                    return roomId;
                }
            }

            throw new ArgumentException($"无法从变量名 {variableName} 中提取教室ID");
        }

        /// <summary>
        /// 判断两个教室是否在不同建筑/校区
        /// </summary>
        private bool AreRoomsInDifferentBuildings(int roomId1, int roomId2, SchedulingProblem problem)
        {
            var room1 = problem.Classrooms.FirstOrDefault(r => r.Id == roomId1);
            var room2 = problem.Classrooms.FirstOrDefault(r => r.Id == roomId2);

            if (room1 == null || room2 == null)
            {
                return false; // 如果找不到教室信息，假设在同一建筑
            }

            // 如果教室在不同校区，一定在不同建筑
            if (room1.CampusId != room2.CampusId)
            {
                return true;
            }

            // 检查是否在同一建筑
            return room1.Building != room2.Building;
        }
    }
}