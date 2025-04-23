using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Algorithms.Hybrid;
using SmartSchedulingSystem.Scheduling.Engine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using SmartSchedulingSystem.Scheduling.Utils;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS
{
    /// <summary>
    /// 局部搜索优化器，用于优化一个已经满足硬约束的解的软约束满足度
    /// </summary>
    public class LocalSearchOptimizer
    {
        private readonly MoveGenerator _moveGenerator;
        private readonly SimulatedAnnealingController _saController;
        private readonly ConstraintAnalyzer _constraintAnalyzer;
        private readonly SolutionEvaluator _evaluator;
        private readonly ILogger<LocalSearchOptimizer> _logger;
        private readonly Utils.SchedulingParameters _parameters;
        private readonly Random _random;

        public LocalSearchOptimizer(
            MoveGenerator moveGenerator,
            SimulatedAnnealingController saController,
            ConstraintAnalyzer constraintAnalyzer,
            SolutionEvaluator evaluator,
            ILogger<LocalSearchOptimizer> logger,
            Utils.SchedulingParameters parameters = null)
        {
            _moveGenerator = moveGenerator ?? throw new ArgumentNullException(nameof(moveGenerator));
            _saController = saController ?? throw new ArgumentNullException(nameof(saController));
            _constraintAnalyzer = constraintAnalyzer ?? throw new ArgumentNullException(nameof(constraintAnalyzer));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parameters = parameters ?? new Utils.SchedulingParameters();
            _random = new Random();
        }
        /// <summary>
        /// 优化多个初始解
        /// </summary>
        /// <param name="initialSolutions">初始解列表</param>
        /// <returns>优化后的解列表</returns>
        public List<SchedulingSolution> OptimizeSolutions(List<SchedulingSolution> initialSolutions)
        {
            if (initialSolutions == null || initialSolutions.Count == 0)
            {
                _logger.LogWarning("传入的初始解列表为空");
                return new List<SchedulingSolution>();
            }

            _logger.LogInformation($"开始优化 {initialSolutions.Count} 个初始解");

            // 并行优化解
            var optimizedSolutions = new List<SchedulingSolution>();

            if (_parameters.EnableParallelOptimization)
            {
                // 使用并行处理
                optimizedSolutions = initialSolutions
                    .AsParallel()
                    .WithDegreeOfParallelism(_parameters.MaxParallelism > 0 ?
                        _parameters.MaxParallelism :
                        Environment.ProcessorCount)
                    .Select(solution =>
                    {
                        try
                        {
                            return OptimizeSolution(solution);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"优化解 {solution.Id} 时发生错误");
                            return solution; // 如果优化失败，返回原解
                        }
                    })
                    .ToList();
            }
            else
            {
                // 串行处理
                foreach (var solution in initialSolutions)
                {
                    try
                    {
                        optimizedSolutions.Add(OptimizeSolution(solution));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"优化解 {solution.Id} 时发生错误");
                        optimizedSolutions.Add(solution); // 如果优化失败，添加原解
                    }
                }
            }

            _logger.LogInformation($"完成 {optimizedSolutions.Count} 个解的优化");

            return optimizedSolutions;
        }
        /// <summary>
        /// 优化指定的解
        /// </summary>
        /// <param name="initialSolution">初始解</param>
        /// <returns>优化后的解</returns>
        public SchedulingSolution OptimizeSolution(SchedulingSolution initialSolution)
        {
            _logger.LogInformation("开始局部搜索优化...");

            // 深拷贝初始解
            var currentSolution = initialSolution.Clone();
            var bestSolution = initialSolution.Clone();

            // 保存当前约束应用级别
            var currentConstraintLevel = SmartSchedulingSystem.Scheduling.Engine.GlobalConstraintManager.Current?.GetCurrentApplicationLevel() 
                                       ?? Engine.ConstraintApplicationLevel.Basic;
            
            _logger.LogDebug($"使用约束级别 {currentConstraintLevel} 进行局部搜索优化");

            // 首次评估解
            var currentEvaluation = _evaluator.Evaluate(currentSolution);
            double bestScore = currentEvaluation.Score;

            _logger.LogInformation("初始解评分: {Score}", bestScore);

            // 重置模拟退火控制器
            _saController.Reset();

            int iteration = 0;
            int noImprovementCount = 0;
            const int MAX_NO_IMPROVEMENT = 100;

            // 预计算并缓存每个约束的初始满足度
            var constraintScores = new Dictionary<int, double>();
            var allConstraints = _evaluator.GetAllActiveConstraints().ToList();

            foreach (var constraint in allConstraints)
            {
                var (score, _) = constraint.Evaluate(currentSolution);
                constraintScores[constraint.Id] = score;
            }

            // 迭代优化
            while (!_saController.Cool())
            {
                iteration++;

                try
                {
                    // 找出满足度最低的约束
                    int weakestConstraintId = -1;
                    double lowestScore = double.MaxValue;

                    foreach (var entry in constraintScores)
                    {
                        if (entry.Value < lowestScore)
                        {
                            lowestScore = entry.Value;
                            weakestConstraintId = entry.Key;
                        }
                    }

                    if (weakestConstraintId == -1)
                    {
                        _logger.LogDebug("没有找到需要优化的约束，跳过迭代");
                        continue;
                    }

                    // 找到对应的约束对象
                    var targetConstraint = allConstraints.FirstOrDefault(c => c.Id == weakestConstraintId);
                    if (targetConstraint == null)
                    {
                        _logger.LogWarning("无法找到ID为{id}的约束", weakestConstraintId);
                        continue;
                    }

                    // 分析约束并生成移动
                    var constraintAnalysis = _constraintAnalyzer.AnalyzeSolution(currentSolution);
                    var assignments = constraintAnalysis.GetAssignmentsAffectedByConstraint(currentSolution, targetConstraint);

                    if (assignments.Count == 0)
                    {
                        assignments = currentSolution.Assignments
                            .OrderBy(a => Guid.NewGuid())
                            .Take(3)
                            .ToList();
                    }

                    // 选择一个随机分配进行修改
                    var targetAssignment = assignments.OrderBy(a => Guid.NewGuid()).First();
                    var moves = _moveGenerator.GenerateValidMoves(currentSolution, targetAssignment, 5);

                    if (moves.Count == 0)
                    {
                        _logger.LogDebug("迭代 {Iteration}: 未找到合法移动", iteration);
                        continue;
                    }
                    IMove bestMove = SelectBestMove(moves, currentSolution);

                    // 应用移动并评估
                    var newSolution = bestMove.Apply(currentSolution);
                    
                    // 确保应用移动后的解仍然满足当前约束级别的要求
                    var hardConstraintsSatisfied = _evaluator.EvaluateHardConstraints(newSolution) >= 1.0;
                    if (!hardConstraintsSatisfied)
                    {
                        _logger.LogDebug("迭代 {Iteration}: 移动 {MoveDescription} 违反了硬约束，拒绝", 
                            iteration, bestMove.GetDescription());
                        continue;
                    }
                    
                    double newScore = _evaluator.Evaluate(newSolution).Score;

                    // 决定是否接受新解
                    bool acceptMove = _saController.ShouldAccept(bestScore, newScore);

                    if (acceptMove)
                    {
                        _logger.LogDebug("迭代 {Iteration}: 接受移动 {MoveDescription}, 新评分: {NewScore}",
                            iteration, bestMove.GetDescription(), newScore);

                        currentSolution = newSolution;
                        
                        // 保存当前解使用的约束级别
                        currentSolution.ConstraintLevel = currentConstraintLevel;

                        // 更新约束分数缓存
                        foreach (var constraint in allConstraints)
                        {
                            var (score, _) = constraint.Evaluate(currentSolution);
                            constraintScores[constraint.Id] = score;
                        }

                        // 如果新解更好，更新最佳解
                        if (newScore > bestScore)
                        {
                            bestSolution = newSolution;
                            bestScore = newScore;
                            _logger.LogInformation("迭代 {Iteration}: 找到更好的解，评分: {Score}", iteration, bestScore);
                            noImprovementCount = 0;
                        }
                        else
                        {
                            noImprovementCount++;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("迭代 {Iteration}: 拒绝移动 {MoveDescription}, 当前温度: {Temperature}",
                            iteration, bestMove.GetDescription(), _saController.CurrentTemperature);
                        noImprovementCount++;
                    }

                    // 提前终止检查
                    if (noImprovementCount >= MAX_NO_IMPROVEMENT)
                    {
                        _logger.LogInformation("连续 {Count} 次无改进，提前终止搜索", MAX_NO_IMPROVEMENT);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "局部搜索迭代 {Iteration} 发生错误", iteration);
                }

                // 定期记录进度
                if (iteration % 50 == 0)
                {
                    _logger.LogInformation("已完成 {Iteration} 次迭代，当前最佳评分: {Score}, 温度: {Temperature}",
                        iteration, bestScore, _saController.CurrentTemperature);
                }
            }

            _logger.LogInformation("局部搜索完成，共 {Iteration} 次迭代，最终评分: {Score}", iteration, bestScore);
            
            // 确保最佳解记录了使用的约束级别
            bestSolution.ConstraintLevel = currentConstraintLevel;
            
            return bestSolution;
        }

        /// <summary>
        /// 评估并选择最佳移动
        /// </summary>
        private IMove SelectBestMove(List<IMove> moves, SchedulingSolution currentSolution)
        {
            IMove bestMove = moves.First();
            double bestMoveScore = double.MinValue;

            foreach (var move in moves)
            {
                var newSolution = move.Apply(currentSolution);
                double score = _evaluator.Evaluate(newSolution).Score;

                if (score > bestMoveScore)
                {
                    bestMove = move;
                    bestMoveScore = score;
                }
            }

            return bestMove;
        }

        /// <summary>
        /// 使用指定参数优化解决方案
        /// </summary>
        /// <param name="initialSolution">初始解</param>
        /// <param name="maxIterations">最大迭代次数</param>
        /// <param name="initialTemperature">初始温度</param>
        /// <param name="coolingRate">冷却率</param>
        /// <returns>优化后的解</returns>
        public SchedulingSolution OptimizeSolution(
            SchedulingSolution initialSolution, 
            int maxIterations, 
            double initialTemperature, 
            double coolingRate)
        {
            _logger.LogInformation("使用指定参数开始局部搜索优化...");
            _logger.LogInformation($"参数: maxIterations={maxIterations}, initialTemperature={initialTemperature}, coolingRate={coolingRate}");
            
            // 保存原始参数
            var originalMaxIterations = _parameters.MaxLsIterations;
            var originalTemperature = _parameters.InitialTemperature;
            var originalCoolingRate = _parameters.CoolingRate;
            
            try
            {
                // 应用新参数
                _parameters.MaxLsIterations = maxIterations;
                _parameters.InitialTemperature = initialTemperature;
                _parameters.CoolingRate = coolingRate;
                
                // 重置模拟退火控制器并应用新参数
                _saController.Reset(initialTemperature, coolingRate);
                
                // 调用标准优化方法
                return OptimizeSolution(initialSolution);
            }
            finally
            {
                // 恢复原始参数
                _parameters.MaxLsIterations = originalMaxIterations;
                _parameters.InitialTemperature = originalTemperature;
                _parameters.CoolingRate = originalCoolingRate;
            }
        }

    }
}