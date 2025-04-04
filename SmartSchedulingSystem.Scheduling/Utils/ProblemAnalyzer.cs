using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Utils
{
    /// <summary>
    /// 分析排课问题，提取问题特征，为算法参数调整和引擎选择提供依据
    /// </summary>
    public class ProblemAnalyzer
    {
        /// <summary>
        /// 分析排课问题并返回特征信息
        /// </summary>
        public ProblemFeatures AnalyzeProblem(SchedulingProblem problem)
        {
            if (problem == null)
            {
                throw new ArgumentNullException(nameof(problem));
            }

            var features = new ProblemFeatures
            {
                CourseSectionCount = problem.CourseSections?.Count ?? 0,
                TeacherCount = problem.Teachers?.Count ?? 0,
                ClassroomCount = problem.Classrooms?.Count ?? 0,
                TimeSlotCount = problem.TimeSlots?.Count ?? 0,
                ConstraintCount = problem.Constraints?.Count ?? 0
            };

            // 分析约束特征
            if (problem.Constraints != null && problem.Constraints.Count > 0)
            {
                features.HardConstraintCount = problem.Constraints.Count(c => c.IsHard);
                features.SoftConstraintCount = problem.Constraints.Count - features.HardConstraintCount;
                features.HardConstraintRatio = (double)features.HardConstraintCount / features.ConstraintCount;
            }

            // 计算问题复杂度
            features.OverallComplexity = CalculateComplexity(features);

            return features;
        }

        /// <summary>
        /// 简单计算问题复杂度
        /// </summary>
        private double CalculateComplexity(ProblemFeatures features)
        {
            // 简单版：根据课程数量和约束数量估算复杂度
            const int smallCourseSections = 20;
            const int mediumCourseSections = 100;

            double sizeComplexity;
            if (features.CourseSectionCount <= smallCourseSections)
            {
                sizeComplexity = features.CourseSectionCount / (double)smallCourseSections * 0.5; // 0-0.5
            }
            else if (features.CourseSectionCount <= mediumCourseSections)
            {
                sizeComplexity = 0.5 + (features.CourseSectionCount - smallCourseSections) /
                    (double)(mediumCourseSections - smallCourseSections) * 0.5; // 0.5-1.0
            }
            else
            {
                sizeComplexity = 1.0; // 大规模问题
            }

            // 考虑约束因素(约束越多越复杂)
            double constraintFactor = Math.Min(1.0, features.ConstraintCount / 20.0);

            return (sizeComplexity * 0.7) + (constraintFactor * 0.3);
        }

        /// <summary>
        /// 为给定问题推荐算法参数
        /// </summary>
        public SchedulingParameters RecommendParameters(ProblemFeatures features)
        {
            var parameters = new SchedulingParameters
            {
                // 根据问题规模设置初始解数量
                InitialSolutionCount = features.CourseSectionCount < 50 ? 5 : 3,

                // 设置CP求解时间限制(秒)
                CpTimeLimit = 60,

                // 设置LS迭代次数
                MaxLsIterations = 1000,

                // 设置模拟退火参数
                InitialTemperature = 1.0,
                CoolingRate = 0.995,

                // 设置并行化参数
                EnableParallelOptimization = features.CourseSectionCount > 50,
                MaxParallelism = Math.Max(2, Environment.ProcessorCount / 2)
            };

            return parameters;
        }
    }

    /// <summary>
    /// 排课问题特征信息
    /// </summary>
    public class ProblemFeatures
    {
        // 基本规模特征
        public int CourseSectionCount { get; set; }
        public int TeacherCount { get; set; }
        public int ClassroomCount { get; set; }
        public int TimeSlotCount { get; set; }

        // 约束特征
        public int ConstraintCount { get; set; }
        public int HardConstraintCount { get; set; }
        public int SoftConstraintCount { get; set; }
        public double HardConstraintRatio { get; set; }

        // 综合复杂度
        public double OverallComplexity { get; set; }
    }
}