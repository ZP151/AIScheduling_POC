using SmartSchedulingSystem.Scheduling.Engine.LS.Moves;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Engine.Hybrid;
using SchedulingSystem.Scheduling.Engine.LS;

namespace SmartSchedulingSystem.Scheduling.Engine.LS
{
    /// <summary>
    /// 局部搜索优化器，用于优化一个已经满足硬约束的解的软约束满足度
    /// </summary>
    public class LocalSearchOptimizer
    {
        private readonly MoveGenerator _moveGenerator;
        private readonly SolutionEvaluator _evaluator;
        private readonly ConstraintAnalyzer _constraintAnalyzer;
        private readonly SimulatedAnnealingController _saController;
        private readonly SchedulingParameters _parameters;

        public LocalSearchOptimizer(
            MoveGenerator moveGenerator,
            SolutionEvaluator evaluator,
            ConstraintAnalyzer constraintAnalyzer,
            SimulatedAnnealingController saController,
            SchedulingParameters parameters)
        {
            _moveGenerator = moveGenerator ?? throw new ArgumentNullException(nameof(moveGenerator));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _constraintAnalyzer = constraintAnalyzer ?? throw new ArgumentNullException(nameof(constraintAnalyzer));
            _saController = saController ?? throw new ArgumentNullException(nameof(saController));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        /// <summary>
        /// 优化指定的解
        /// </summary>
        /// <param name="initialSolution">初始解</param>
        /// <returns>优化后的解</returns>
        public SchedulingSolution OptimizeSolution(SchedulingSolution initialSolution)
        {
            var currentSolution = initialSolution.Clone();
            var bestSolution = initialSolution.Clone();
            double bestScore = _evaluator.Evaluate(bestSolution);

            // 重置模拟退火控制器
            _saController.Reset();

            // 迭代优化
            while (!_saController.Cool())
            {
                // 1. 分析当前解的软约束满足情况
                var constraintAnalysis = _constraintAnalyzer.AnalyzeSolution(currentSolution);

                // 2. 选择一个需要优化的约束
                var targetConstraint = constraintAnalysis.GetWeakestConstraint();

                // 3. 找出与该约束相关的课程分配
                var assignments = constraintAnalysis.GetAssignmentsAffectedByConstraint(currentSolution, targetConstraint);

                // 4. 如果没有找到相关分配，随机选择一个
                if (assignments.Count == 0)
                {
                    assignments = currentSolution.Assignments
                        .OrderBy(a => Guid.NewGuid())
                        .Take(3)
                        .ToList();
                }

                // 5. 针对其中一个分配生成移动操作
                var targetAssignment = assignments.OrderBy(a => Guid.NewGuid()).First();
                var moves = _moveGenerator.GenerateValidMoves(currentSolution, targetAssignment, 5);

                // 6. 如果没有合法移动，继续下一轮
                if (moves.Count == 0)
                {
                    continue;
                }

                // 7. 评估每个移动，选择最佳移动
                IMove bestMove = SelectBestMove(moves, currentSolution);

                // 8. 应用移动生成新解
                var newSolution = bestMove.Apply(currentSolution);
                double newScore = _evaluator.Evaluate(newSolution);

                // 9. 决定是否接受新解
                if (_saController.ShouldAccept(bestScore, newScore))
                {
                    currentSolution = newSolution;

                    // 如果新解比当前最佳解更好，更新最佳解
                    if (newScore > bestScore)
                    {
                        bestSolution = newSolution;
                        bestScore = newScore;
                    }
                }
            }

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
                double score = _evaluator.Evaluate(newSolution);

                if (score > bestMoveScore)
                {
                    bestMove = move;
                    bestMoveScore = score;
                }
            }

            return bestMove;
        }
    }
}