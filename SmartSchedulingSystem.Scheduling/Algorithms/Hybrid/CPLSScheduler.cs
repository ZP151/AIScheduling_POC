using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Engine.LS;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Engine.Hybrid
{
    /// <summary>
    /// 结合约束规划(CP)和局部搜索(LS)的混合排课引擎
    /// </summary>
    public class CPLSScheduler
    {
        private readonly CPScheduler _cpScheduler;
        private readonly LocalSearchOptimizer _localSearchOptimizer;
        private readonly SolutionEvaluator _evaluator;
        private readonly ParameterAdjuster _parameterAdjuster;
        private readonly SchedulingParameters _parameters;

        public CPLSScheduler(
            CPScheduler cpScheduler,
            LocalSearchOptimizer localSearchOptimizer,
            SolutionEvaluator evaluator,
            ParameterAdjuster parameterAdjuster,
            SchedulingParameters parameters)
        {
            _cpScheduler = cpScheduler ?? throw new ArgumentNullException(nameof(cpScheduler));
            _localSearchOptimizer = localSearchOptimizer ?? throw new ArgumentNullException(nameof(localSearchOptimizer));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _parameterAdjuster = parameterAdjuster ?? throw new ArgumentNullException(nameof(parameterAdjuster));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        /// <summary>
        /// 生成排课方案
        /// </summary>
        public SchedulingResult GenerateSchedule(SchedulingProblem problem)
        {
            // 1. 调整参数
            _parameterAdjuster.AdjustParameters(problem);

            // 2. 使用CP生成多个满足所有硬约束的初始解
            List<SchedulingSolution> initialSolutions;
            try
            {
                initialSolutions = _cpScheduler.GenerateInitialSolutions(
                    problem, _parameters.InitialSolutionCount);
            }
            catch (Exception ex)
            {
                // CP失败，返回错误结果
                return new SchedulingResult
                {
                    Status = SchedulingStatus.Failure,
                    Message = $"生成初始解失败: {ex.Message}",
                    Solutions = new List<SchedulingSolution>()
                };
            }

            // 如果没有找到初始解，返回错误结果
            if (initialSolutions.Count == 0)
            {
                return new SchedulingResult
                {
                    Status = SchedulingStatus.Failure,
                    Message = "无法找到满足所有硬约束的初始解",
                    Solutions = new List<SchedulingSolution>()
                };
            }

            // 3. 使用局部搜索优化每个初始解
            List<SchedulingSolution> optimizedSolutions = new List<SchedulingSolution>();

            // 如果启用并行优化
            if (_parameters.EnableParallelOptimization)
            {
                // 使用并行处理优化多个解
                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _parameters.MaxParallelism };
                Parallel.ForEach(initialSolutions, parallelOptions, solution =>
                {
                    var optimizedSolution = _localSearchOptimizer.OptimizeSolution(solution);
                    lock (optimizedSolutions)
                    {
                        optimizedSolutions.Add(optimizedSolution);
                    }
                });
            }
            else
            {
                // 顺序优化每个解
                foreach (var solution in initialSolutions)
                {
                    var optimizedSolution = _localSearchOptimizer.OptimizeSolution(solution);
                    optimizedSolutions.Add(optimizedSolution);
                }
            }

            // 4. 对优化后的解进行评分和排序
            optimizedSolutions = optimizedSolutions
                .OrderByDescending(s => _evaluator.Evaluate(s))
                .ToList();

            // 5. 返回结果
            return new SchedulingResult
            {
                Status = SchedulingStatus.Success,
                Message = "成功生成排课方案",
                Solutions = optimizedSolutions
            };
        }
    }
}