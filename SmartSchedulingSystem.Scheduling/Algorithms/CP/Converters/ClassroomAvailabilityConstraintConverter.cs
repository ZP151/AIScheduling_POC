using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters
{
    /// <summary>
    /// 将教室可用性约束转换为CP模型约束
    /// </summary>
    public class ClassroomAvailabilityConstraintConverter : ICPConstraintConverter
    {
        public void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            // 处理教室不可用时间段
            foreach (var availability in problem.ClassroomAvailabilities)
            {
                // 只处理教室不可用的时间段
                if (!availability.IsAvailable)
                {
                    // 找出在不可用时间段使用该教室的所有变量
                    var unavailableVars = variables
                        .Where(kv => kv.Key.Contains($"_t{availability.TimeSlotId}_r{availability.ClassroomId}_"))
                        .Select(kv => kv.Value)
                        .ToList();

                    // 添加约束确保这些变量都为0（教室不在不可用时段使用）
                    foreach (var variable in unavailableVars)
                    {
                        model.Add(variable == 0);
                    }
                }
            }

            // 考虑教室维护或预订时间
            // 这里可以添加更多特定场景的约束
            // 例如，对于特定日期的教室维护或预订
            // 这部分可以根据实际需求扩展
        }
    }
}