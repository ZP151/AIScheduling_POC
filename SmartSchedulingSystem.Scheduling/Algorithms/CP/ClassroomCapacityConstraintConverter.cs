using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 将教室容量约束转换为CP模型约束
    /// </summary>
    public class ClassroomCapacityConstraintConverter : ICPConstraintConverter
    {
        private readonly Dictionary<int, int> _classroomCapacities;
        private readonly Dictionary<int, int> _expectedEnrollments;

        public ClassroomCapacityConstraintConverter(
            Dictionary<int, int> classroomCapacities,
            Dictionary<int, int> expectedEnrollments)
        {
            _classroomCapacities = classroomCapacities ?? throw new ArgumentNullException(nameof(classroomCapacities));
            _expectedEnrollments = expectedEnrollments ?? throw new ArgumentNullException(nameof(expectedEnrollments));
        }

        public void AddToModel(CpModel model, Dictionary<string, IntVar> variables, SchedulingProblem problem)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            // 处理每个课程班级
            foreach (var section in problem.CourseSections)
            {
                // 获取班级的预期学生人数
                if (!_expectedEnrollments.TryGetValue(section.Id, out int enrollment))
                {
                    enrollment = section.Enrollment; // 使用班级的默认人数
                }

                // 处理每个教室
                foreach (var classroom in problem.Classrooms)
                {
                    // 获取教室容量
                    if (!_classroomCapacities.TryGetValue(classroom.Id, out int capacity))
                    {
                        capacity = classroom.Capacity; // 使用教室的默认容量
                    }

                    // 检查容量是否足够
                    if (capacity < enrollment)
                    {
                        // 找出所有将该班级安排到该教室的变量
                        var invalidVars = variables
                            .Where(kv => kv.Key.StartsWith($"c{section.Id}_") &&
                                       kv.Key.Contains($"_r{classroom.Id}_"))
                            .Select(kv => kv.Value)
                            .ToList();

                        // 添加约束：这些变量必须为0（禁止该班级使用该教室）
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