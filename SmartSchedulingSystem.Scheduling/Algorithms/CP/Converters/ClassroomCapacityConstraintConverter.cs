using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters
{
    /// <summary>
    /// 将教室容量约束转换为CP模型约束
    /// </summary>
    public class ClassroomCapacityConstraintConverter : ICPConstraintConverter
    {
        /// <summary>
        /// 获取约束转换器的约束级别
        /// </summary>
        public Engine.ConstraintApplicationLevel ConstraintLevel => Engine.ConstraintApplicationLevel.Basic;

        private readonly Dictionary<int, int> _classroomCapacities;
        private readonly Dictionary<int, int> _expectedEnrollments;

        public ClassroomCapacityConstraintConverter(
            IClassroomCapacityProvider capacityProvider,
            Dictionary<int, int> expectedEnrollments)
        {
            if (capacityProvider == null) throw new ArgumentNullException(nameof(capacityProvider));
            _classroomCapacities = capacityProvider.GetCapacities(); 
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
    public interface IClassroomCapacityProvider
    {
        Dictionary<int, int> GetCapacities();
    }
    public class TestClassroomCapacityProvider : IClassroomCapacityProvider
    {
        public Dictionary<int, int> GetCapacities() => new Dictionary<int, int>
        {
            [1] = 50,
            [2] = 40,
            [3] = 60,
            [4] = 35,
            [5] = 45,
            [6] = 55,
            [7] = 70,
            [8] = 30,
            [9] = 65,
            [10] = 50,
            [11] = 40,
            [12] = 60,
            [13] = 45,
            [14] = 50,
            [15] = 55
        };
    }
}