using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Utils;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms.Hybrid
{
    /// <summary>
    /// 参数调整器 - 根据问题特性和中间结果动态调整参数
    /// </summary>
    public class ParameterAdjuster
    {
        private readonly Utils.SchedulingParameters _parameters;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parameters">排课参数</param>
        public ParameterAdjuster(Utils.SchedulingParameters parameters)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        /// <summary>
        /// 基于问题特征调整参数
        /// </summary>
        public void AdjustParameters(SchedulingProblem problem)
        {
            // 计算问题的基本特征
            int courseCount = problem.CourseSections.Count;
            int teacherCount = problem.Teachers.Count;
            int classroomCount = problem.Classrooms.Count;
            int timeSlotCount = problem.TimeSlots.Count;

            // 计算问题规模指标 (0-1范围)
            double problemSizeMetric = CalculateProblemSizeMetric(courseCount, teacherCount, classroomCount, timeSlotCount);

            // 计算约束复杂度
            double constraintComplexity = CalculateConstraintComplexity(problem);

            // 调整参数
            AdjustCPParameters(problemSizeMetric, constraintComplexity);
            AdjustLSParameters(problemSizeMetric, constraintComplexity);
            AdjustParallelizationParameters(problemSizeMetric);

            // 输出调整后的参数信息
            LogParameters();
        }

        /// <summary>
        /// 计算问题规模指标 (0-1范围)
        /// </summary>
        private double CalculateProblemSizeMetric(int courseCount, int teacherCount, int classroomCount, int timeSlotCount)
        {
            // 根据经验值确定大小问题的阈值
            const int smallCourseCount = 20;
            const int mediumCourseCount = 100;
            const int largeCourseCount = 500;

            // 根据课程数量计算规模指标
            double sizeMetric;
            if (courseCount <= smallCourseCount)
            {
                sizeMetric = (double)courseCount / smallCourseCount * 0.25; // 0-0.25
            }
            else if (courseCount <= mediumCourseCount)
            {
                sizeMetric = 0.25 + (courseCount - smallCourseCount) / (double)(mediumCourseCount - smallCourseCount) * 0.5; // 0.25-0.75
            }
            else
            {
                sizeMetric = Math.Min(0.75 + (courseCount - mediumCourseCount) / (double)(largeCourseCount - mediumCourseCount) * 0.25, 1.0); // 0.75-1.0
            }

            return sizeMetric;
        }

        /// <summary>
        /// 计算约束复杂度
        /// </summary>
        private double CalculateConstraintComplexity(SchedulingProblem problem)
        {
            // 简化版：根据硬约束比例估算约束复杂度
            // 实际实现中可以考虑更多因素，如约束间的相互作用

            const double defaultComplexity = 0.5; // 默认中等复杂度

            // 如果约束信息不可用，返回默认值
            if (problem.Constraints == null || problem.Constraints.Count == 0)
            {
                return defaultComplexity;
            }

            // 计算硬约束比例
            int hardConstraintCount = problem.Constraints.Count(c => c.IsHard);
            double hardConstraintRatio = (double)hardConstraintCount / problem.Constraints.Count;

            // 硬约束比例越高，问题越复杂
            return 0.3 + hardConstraintRatio * 0.7; // 0.3-1.0范围
        }

        /// <summary>
        /// 调整CP参数
        /// </summary>
        private void AdjustCPParameters(double problemSizeMetric, double constraintComplexity)
        {
            // 根据问题规模调整初始解数量
            if (problemSizeMetric < 0.4) // 小型问题
            {
                _parameters.InitialSolutionCount = 10;
            }
            else if (problemSizeMetric < 0.7) // 中型问题
            {
                _parameters.InitialSolutionCount = 5;
            }
            else // 大型问题
            {
                _parameters.InitialSolutionCount = 3;
            }

            // 根据约束复杂度调整CP求解时间限制
            _parameters.CpTimeLimit = (int)(30 + constraintComplexity * 270); // 30-300秒
        }

        /// <summary>
        /// 调整LS参数
        /// </summary>
        private void AdjustLSParameters(double problemSizeMetric, double constraintComplexity)
        {
            // 根据问题规模和约束复杂度调整局部搜索迭代次数
            _parameters.MaxLsIterations = (int)(500 + 4500 * problemSizeMetric * constraintComplexity); // 500-5000

            // 调整模拟退火参数
            _parameters.InitialTemperature = 0.5 + 0.5 * constraintComplexity; // 0.5-1.0
            _parameters.CoolingRate = 0.999 - 0.001 * problemSizeMetric; // 0.999-0.998
        }

        /// <summary>
        /// 调整并行化参数
        /// </summary>
        private void AdjustParallelizationParameters(double problemSizeMetric)
        {
            // 小型问题可能不需要并行化
            _parameters.EnableParallelOptimization = problemSizeMetric >= 0.4;

            // 根据问题规模调整最大并行度
            // 大型问题使用更多线程
            int availableProcessors = Environment.ProcessorCount;
            _parameters.MaxParallelism = problemSizeMetric < 0.7
                ? Math.Max(2, availableProcessors / 2)
                : Math.Max(4, availableProcessors - 1);
        }

        /// <summary>
        /// 输出调整后的参数
        /// </summary>
        private void LogParameters()
        {
            Console.WriteLine("调整后的参数:");
            Console.WriteLine($"初始解数量: {_parameters.InitialSolutionCount}");
            Console.WriteLine($"CP求解时间限制: {_parameters.CpTimeLimit}秒");
            Console.WriteLine($"LS最大迭代次数: {_parameters.MaxLsIterations}");
            Console.WriteLine($"初始温度: {_parameters.InitialTemperature}");
            Console.WriteLine($"冷却率: {_parameters.CoolingRate}");
            Console.WriteLine($"启用并行优化: {_parameters.EnableParallelOptimization}");
            Console.WriteLine($"最大并行度: {_parameters.MaxParallelism}");
        }
    }
}