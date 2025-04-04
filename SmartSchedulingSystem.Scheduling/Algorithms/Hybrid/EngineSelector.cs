using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Engine.Hybrid
{
    /// <summary>
    /// 根据问题特征选择或分配计算资源给不同引擎的工具
    /// </summary>
    public class EngineSelector
    {
        /// <summary>
        /// 根据问题特征确定不同引擎的权重
        /// </summary>
        public EngineWeights DetermineWeights(SchedulingProblem problem)
        {
            // 分析问题特征
            var features = ExtractProblemFeatures(problem);

            // 根据特征计算各引擎权重
            var weights = new EngineWeights
            {
                // 硬约束比例高的问题更适合CP引擎
                CPWeight = 0.3 + 0.5 * features.HardConstraintRatio,

                // 默认LS引擎权重，确保CP+LS=1
                LSWeight = 0.7 - 0.5 * features.HardConstraintRatio
            };

            return weights;
        }

        /// <summary>
        /// 提取问题特征
        /// </summary>
        private ProblemFeatures ExtractProblemFeatures(SchedulingProblem problem)
        {
            var features = new ProblemFeatures();

            // 计算约束比例
            if (problem.Constraints != null && problem.Constraints.Count > 0)
            {
                int hardConstraintCount = problem.Constraints.Count(c => c.IsHard);
                features.HardConstraintRatio = (double)hardConstraintCount / problem.Constraints.Count;
            }

            // 计算问题规模
            features.ProblemSize = CalculateProblemSize(problem);

            // 评估约束复杂度
            features.ConstraintComplexity = CalculateConstraintComplexity(problem);

            return features;
        }

        /// <summary>
        /// 计算问题规模(0-1范围)
        /// </summary>
        private double CalculateProblemSize(SchedulingProblem problem)
        {
            // 简单版：根据课程数量估算
            int courseCount = problem.CourseSections?.Count ?? 0;

            // 定义课程数量阈值
            const int smallProblem = 20;
            const int mediumProblem = 100;
            const int largeProblem = 500;

            if (courseCount <= smallProblem)
            {
                return (double)courseCount / smallProblem * 0.33; // 0-0.33
            }
            else if (courseCount <= mediumProblem)
            {
                return 0.33 + (courseCount - smallProblem) / (double)(mediumProblem - smallProblem) * 0.33; // 0.33-0.66
            }
            else
            {
                double sizeFactor = Math.Min(1.0, (double)courseCount / largeProblem);
                return 0.66 + sizeFactor * 0.34; // 0.66-1.0
            }
        }

        /// <summary>
        /// 计算约束复杂度(0-1范围)
        /// </summary>
        private double CalculateConstraintComplexity(SchedulingProblem problem)
        {
            // 简化版：根据约束数量和类型估算

            double complexityScore = 0.5; // 默认中等复杂度

            if (problem.Constraints != null && problem.Constraints.Count > 0)
            {
                // 约束数量因子(0-0.5)
                int constraintCount = problem.Constraints.Count;
                double countFactor = Math.Min(0.5, constraintCount / 20.0);

                // 约束类型多样性因子(0-0.5)
                var constraintTypes = problem.Constraints.Select(c => c.GetType().Name).Distinct().Count();
                double diversityFactor = Math.Min(0.5, constraintTypes / 10.0);

                complexityScore = countFactor + diversityFactor;
            }

            return complexityScore;
        }
    }

    /// <summary>
    /// 表示各引擎的权重
    /// </summary>
    public class EngineWeights
    {
        /// <summary>
        /// CP引擎权重
        /// </summary>
        public double CPWeight { get; set; } = 0.5;

        /// <summary>
        /// LS引擎权重
        /// </summary>
        public double LSWeight { get; set; } = 0.5;
    }

    /// <summary>
    /// 表示问题特征
    /// </summary>
    public class ProblemFeatures
    {
        /// <summary>
        /// 硬约束比例
        /// </summary>
        public double HardConstraintRatio { get; set; } = 0.5;

        /// <summary>
        /// 问题规模(0-1)
        /// </summary>
        public double ProblemSize { get; set; } = 0.5;

        /// <summary>
        /// 约束复杂度(0-1)
        /// </summary>
        public double ConstraintComplexity { get; set; } = 0.5;
    }
}