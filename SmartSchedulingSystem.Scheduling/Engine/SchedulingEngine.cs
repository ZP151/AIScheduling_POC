using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Engine.Hybrid;
using SmartSchedulingSystem.Scheduling.Engine.LS;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    /// <summary>
    /// 排课引擎核心类，负责协调并执行完整的排课过程
    /// </summary>
    public class SchedulingEngine
    {
        private readonly ILogger<SchedulingEngine> _logger;
        private readonly ConstraintManager _constraintManager;
        private readonly CPScheduler _cpScheduler;
        private readonly LocalSearchOptimizer _localSearchOptimizer;
        private readonly CPLSScheduler _cplsScheduler;
        private readonly ProblemAnalyzer _problemAnalyzer;
        private readonly SolutionEvaluator _solutionEvaluator;

        public SchedulingEngine(
            ILogger<SchedulingEngine> logger,
            ConstraintManager constraintManager,
            CPScheduler cpScheduler,
            LocalSearchOptimizer localSearchOptimizer,
            CPLSScheduler cplsScheduler,
            ProblemAnalyzer problemAnalyzer,
            SolutionEvaluator solutionEvaluator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _cpScheduler = cpScheduler ?? throw new ArgumentNullException(nameof(cpScheduler));
            _localSearchOptimizer = localSearchOptimizer ?? throw new ArgumentNullException(nameof(localSearchOptimizer));
            _cplsScheduler = cplsScheduler ?? throw new ArgumentNullException(nameof(cplsScheduler));
            _problemAnalyzer = problemAnalyzer ?? throw new ArgumentNullException(nameof(problemAnalyzer));
            _solutionEvaluator = solutionEvaluator ?? throw new ArgumentNullException(nameof(solutionEvaluator));
        }

        /// <summary>
        /// 生成排课方案
        /// </summary>
        public SchedulingResult GenerateSchedule(SchedulingProblem problem, SchedulingParameters parameters = null)
        {
            try
            {
                _logger.LogInformation("开始生成排课方案...");

                // 分析问题
                var features = _problemAnalyzer.AnalyzeProblem(problem);
                _logger.LogInformation("问题特征: 课程数={CourseCount}, 教师数={TeacherCount}, 教室数={ClassroomCount}, 复杂度={Complexity}",
                    features.CourseSectionCount, features.TeacherCount, features.ClassroomCount, features.OverallComplexity);

                // 如果没有提供参数，使用推荐参数
                parameters ??= _problemAnalyzer.RecommendParameters(features);

                // 使用混合引擎生成排课方案
                var result = _cplsScheduler.GenerateSchedule(problem);

                if (result.Status == SchedulingStatus.Success)
                {
                    _logger.LogInformation("成功生成排课方案，共{SolutionCount}个方案", result.Solutions.Count);
                }
                else
                {
                    _logger.LogWarning("排课方案生成失败: {Message}", result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成排课方案时发生异常");
                return new SchedulingResult
                {
                    Status = SchedulingStatus.Error,
                    Message = $"发生异常: {ex.Message}",
                    Solutions = new List<SchedulingSolution>()
                };
            }
        }

        /// <summary>
        /// 评估排课方案
        /// </summary>
        public SchedulingEvaluation EvaluateSchedule(SchedulingSolution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            try
            {
                _logger.LogInformation("开始评估排课方案...");

                // 使用评估器评估方案
                double score = _solutionEvaluator.Evaluate(solution);
                var hardConstraintSatisfaction = _solutionEvaluator.EvaluateHardConstraints(solution);
                var softConstraintSatisfaction = _solutionEvaluator.EvaluateSoftConstraints(solution);

                return new SchedulingEvaluation
                {
                    SolutionId = solution.Id,
                    Score = score,
                    HardConstraintsSatisfied = hardConstraintSatisfaction >= 1.0,
                    HardConstraintsSatisfactionLevel = hardConstraintSatisfaction,
                    SoftConstraintsSatisfactionLevel = softConstraintSatisfaction
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "评估排课方案时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 优化现有排课方案
        /// </summary>
        public SchedulingSolution OptimizeSchedule(SchedulingSolution solution, SchedulingParameters parameters = null)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            try
            {
                _logger.LogInformation("开始优化排课方案...");

                // 使用局部搜索优化器优化方案
                var optimizedSolution = _localSearchOptimizer.OptimizeSolution(solution);

                _logger.LogInformation("排课方案优化完成");

                return optimizedSolution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "优化排课方案时发生异常");
                throw;
            }
        }
    }
}