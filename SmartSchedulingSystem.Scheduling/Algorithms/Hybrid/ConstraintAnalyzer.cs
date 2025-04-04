using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Engine.Hybrid
{
    /// <summary>
    /// 分析解的约束满足情况，用于指导局部搜索
    /// </summary>
    public class ConstraintAnalyzer
    {
        private readonly ConstraintManager _constraintManager;

        public ConstraintAnalyzer(ConstraintManager constraintManager)
        {
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
        }

        /// <summary>
        /// 分析解的约束满足情况
        /// </summary>
        public ConstraintAnalysisResult AnalyzeSolution(SchedulingSolution solution)
        {
            var result = new ConstraintAnalysisResult();

            // 分析软约束满足情况
            foreach (var constraint in _constraintManager.GetSoftConstraints())
            {
                var satisfactionLevel = constraint.Evaluate(solution);
                result.ConstraintSatisfaction[constraint] = satisfactionLevel.Score;
            }

            return result;
        }

        /// <summary>
        /// 表示约束分析结果
        /// </summary>
        public class ConstraintAnalysisResult
        {
            /// <summary>
            /// 各约束的满足度(0-1)
            /// </summary>
            public Dictionary<IConstraint, double> ConstraintSatisfaction { get; } = new Dictionary<IConstraint, double>();

            /// <summary>
            /// 获取满足度最低的约束
            /// </summary>
            public IConstraint GetWeakestConstraint()
            {
                if (ConstraintSatisfaction.Count == 0)
                {
                    return null;
                }

                return ConstraintSatisfaction
                    .OrderBy(kv => kv.Value * kv.Key.Weight) // 考虑权重和满足度
                    .First().Key;
            }

            /// <summary>
            /// 获取与指定约束相关的课程分配
            /// </summary>
            public List<SchedulingAssignment> GetAssignmentsAffectedByConstraint(
                SchedulingSolution solution, IConstraint constraint)
            {
                if (constraint == null)
                {
                    return new List<SchedulingAssignment>();
                }

                // 根据约束类型找出相关分配
                // 注意：真实实现中需要针对不同约束类型定制此方法

                // 简化版实现，对于大多数约束可能不够精确
                var affectedAssignments = new List<SchedulingAssignment>();
                foreach (var assignment in solution.Assignments)
                {
                    // 创建临时解进行测试
                    var tempSolution = solution.Clone();
                    var tempAssignment = tempSolution.Assignments.First(a => a.Id == assignment.Id);

                    // 临时修改分配(例如移动到另一个时间槽)
                    int originalTimeSlot = tempAssignment.TimeSlotId;
                    tempAssignment.TimeSlotId = (originalTimeSlot % 20) + 1; // 简单地改变到另一个时间槽

                    // 测试此变更对约束满足度的影响
                    double originalSatisfaction = constraint.Evaluate(solution).Score;
                    double newSatisfaction = constraint.Evaluate(tempSolution).Score;

                    // 如果变更对约束满足度有显著影响，则此分配与约束相关
                    if (Math.Abs(newSatisfaction - originalSatisfaction) > 0.01)
                    {
                        affectedAssignments.Add(assignment);
                    }

                    // 恢复原始状态
                    tempAssignment.TimeSlotId = originalTimeSlot;
                }

                return affectedAssignments;
            }
        }
    }
}