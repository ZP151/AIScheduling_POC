using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    /// <summary>
    /// 解决方案评估器接口
    /// </summary>
    public interface ISolutionEvaluator
    {
        SchedulingEvaluation Evaluate(SchedulingSolution solution);
    }

    /// <summary>
    /// 评估排课解决方案的类
    /// </summary>
    public class SolutionEvaluator : ISolutionEvaluator
    {
        private readonly ILogger<SolutionEvaluator> _logger;
        private readonly ConstraintManager _constraintManager;
        private readonly Utils.SchedulingParameters _parameters;

        // 缓存评估结果，减少重复计算
        private readonly Dictionary<int, Dictionary<int, double>> _constraintScoreCache = new Dictionary<int, Dictionary<int, double>>();

        public SolutionEvaluator(
            ILogger<SolutionEvaluator> logger,
            ConstraintManager constraintManager,
            Utils.SchedulingParameters parameters = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _parameters = parameters ?? new Utils.SchedulingParameters();
        }

        /// <summary>
        /// 评估解决方案，返回0-1分数（1为最佳）
        /// </summary>
        public SchedulingEvaluation Evaluate(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));
            
            var evaluation = new SchedulingEvaluation
            {
                SolutionId = solution.Id,
                HardConstraintEvaluations = new List<ConstraintEvaluation>(),
                SoftConstraintEvaluations = new List<ConstraintEvaluation>(),
                Conflicts = new List<SchedulingConflict>()
            };
            try
            {
                // 检查缓存
                if (solution.Id > 0 && _constraintScoreCache.TryGetValue(solution.Id, out var cachedScores))
                {
                    // 如果所有约束都有缓存，直接返回
                    int totalConstraints = _constraintManager.GetAllConstraints().Count;
                    if (cachedScores.Count == totalConstraints)
                    {
                        double hardScore = EvaluateHardConstraintsFromCache(cachedScores);

                        // 如果不满足硬约束，直接返回负无穷
                        if (hardScore < 1.0)
                        {
                            evaluation.IsFeasible = false;
                            evaluation.Score = double.NegativeInfinity;
                            return evaluation;
                        }

                        // 计算软约束评分
                        double softScore = EvaluateSoftConstraintsFromCache(cachedScores);
                        evaluation.IsFeasible = true;
                        evaluation.Score = softScore;
                        return evaluation;
                    }
                }

                // 如果没有完整缓存，重新计算
                double hardConstraintScore = EvaluateHardConstraints(solution);

                // 如果违反任何硬约束，返回负无穷大分数
                if (hardConstraintScore < 1.0)
                {
                    evaluation.IsFeasible = false;
                    evaluation.Score = double.NegativeInfinity;
                    return evaluation;
                }

                // 评估软约束
                double physicalSoftScore = EvaluatePhysicalSoftConstraints(solution);
                double qualitySoftScore = EvaluateQualitySoftConstraints(solution);

                // 加权组合得分
                double softConstraintScore =
                    (_parameters.PhysicalSoftConstraintWeight * physicalSoftScore) +
                    (_parameters.QualitySoftConstraintWeight * qualitySoftScore);

                _logger.LogDebug($"评分: 硬约束={hardConstraintScore:F4}, 物理软约束={physicalSoftScore:F4}, " +
                               $"质量软约束={qualitySoftScore:F4}, 总分={softConstraintScore:F4}");

                evaluation.IsFeasible = true;
                evaluation.Score = softConstraintScore;
                return evaluation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "评估解决方案时出错");
                evaluation.IsFeasible = false;
                evaluation.Score = double.NegativeInfinity;
                return evaluation;
            }
        }

        /// <summary>
        /// 评估硬约束
        /// </summary>
        public double EvaluateHardConstraints(SchedulingSolution solution)
        {
            var hardConstraints = _constraintManager.GetHardConstraints();

            if (hardConstraints.Count == 0)
                return 1.0; // 没有硬约束，视为满足

            // 检查是否所有硬约束都满足
            foreach (var constraint in hardConstraints)
            {
                try
                {
                    var (score, conflicts) = constraint.Evaluate(solution);

                    // 缓存结果
                    if (solution.Id > 0)
                    {
                        CacheConstraintScore(solution.Id, constraint.Id, score);
                    }

                    // 如果任一硬约束不满足，整体不满足
                    if (score < 1.0)
                    {
                        _logger.LogDebug($"硬约束'{constraint.Name}'不满足，得分={score:F4}");

                        if (conflicts != null && conflicts.Any())
                        {
                            _logger.LogDebug($"冲突：{conflicts.First().Description}");
                        }

                        return 0.0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"评估硬约束'{constraint.Name}'时出错");
                    return 0.0; // 出错视为约束不满足
                }
            }

            return 1.0; // 所有硬约束都满足
        }
        /// <summary>
        /// 综合评估所有软约束
        /// </summary>
        public double EvaluateSoftConstraints(SchedulingSolution solution)
        {
            double physicalSoftScore = EvaluatePhysicalSoftConstraints(solution);
            double qualitySoftScore = EvaluateQualitySoftConstraints(solution);

            // 使用参数配置的权重计算综合软约束得分
            return (_parameters.PhysicalSoftConstraintWeight * physicalSoftScore) +
                   (_parameters.QualitySoftConstraintWeight * qualitySoftScore);
        }
        /// <summary>
        /// 评估物理软约束
        /// </summary>
        public double EvaluatePhysicalSoftConstraints(SchedulingSolution solution)
        {
            var physicalSoftConstraints = _constraintManager.GetSoftConstraints()
                .Where(c => c.Hierarchy == ConstraintHierarchy.Level3_PhysicalSoft)
                .ToList();

            if (physicalSoftConstraints.Count == 0)
                return 1.0; // 没有物理软约束，视为满分

            double totalScore = 0;
            double totalWeight = physicalSoftConstraints.Sum(c => c.Weight);

            if (totalWeight == 0)
                return 1.0; // 权重总和为0，视为满分

            foreach (var constraint in physicalSoftConstraints)
            {
                try
                {
                    var (score, conflicts) = constraint.Evaluate(solution);

                    // 缓存结果
                    if (solution.Id > 0)
                    {
                        CacheConstraintScore(solution.Id, constraint.Id, score);
                    }

                    totalScore += score * constraint.Weight;

                    _logger.LogDebug($"物理软约束'{constraint.Name}'得分={score:F4}，" +
                                   $"权重={constraint.Weight:F2}，" +
                                   $"冲突数={conflicts?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"评估物理软约束'{constraint.Name}'时出错");
                    // 出错时该约束得0分
                }
            }

            double weightedScore = totalScore / totalWeight;
            return weightedScore;
        }

        /// <summary>
        /// 评估质量软约束
        /// </summary>
        public double EvaluateQualitySoftConstraints(SchedulingSolution solution)
        {
            var qualitySoftConstraints = _constraintManager.GetSoftConstraints()
                .Where(c => c.Hierarchy == ConstraintHierarchy.Level4_QualitySoft)
                .ToList();

            if (qualitySoftConstraints.Count == 0)
                return 1.0; // 没有质量软约束，视为满分

            double totalScore = 0;
            double totalWeight = qualitySoftConstraints.Sum(c => c.Weight);

            if (totalWeight == 0)
                return 1.0; // 权重总和为0，视为满分

            foreach (var constraint in qualitySoftConstraints)
            {
                try
                {
                    var (score, conflicts) = constraint.Evaluate(solution);

                    // 缓存结果
                    if (solution.Id > 0)
                    {
                        CacheConstraintScore(solution.Id, constraint.Id, score);
                    }

                    totalScore += score * constraint.Weight;

                    _logger.LogDebug($"质量软约束'{constraint.Name}'得分={score:F4}，" +
                                   $"权重={constraint.Weight:F2}，" +
                                   $"冲突数={conflicts?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"评估质量软约束'{constraint.Name}'时出错");
                    // 出错时该约束得0分
                }
            }

            double weightedScore = totalScore / totalWeight;
            return weightedScore;
        }

        /// <summary>
        /// 从缓存评估硬约束
        /// </summary>
        private double EvaluateHardConstraintsFromCache(Dictionary<int, double> cachedScores)
        {
            var hardConstraints = _constraintManager.GetHardConstraints();

            if (hardConstraints.Count == 0)
                return 1.0; // 没有硬约束，视为满足

            foreach (var constraint in hardConstraints)
            {
                if (cachedScores.TryGetValue(constraint.Id, out double score))
                {
                    if (score < 1.0)
                    {
                        return 0.0; // 有硬约束不满足
                    }
                }
                else
                {
                    // 缓存中缺少约束评分，视为不满足
                    return 0.0;
                }
            }

            return 1.0; // 所有硬约束都满足
        }

        /// <summary>
        /// 从缓存评估软约束
        /// </summary>
        private double EvaluateSoftConstraintsFromCache(Dictionary<int, double> cachedScores)
        {
            var physicalSoftConstraints = _constraintManager.GetSoftConstraints()
                .Where(c => c.Hierarchy == ConstraintHierarchy.Level3_PhysicalSoft)
                .ToList();

            var qualitySoftConstraints = _constraintManager.GetSoftConstraints()
                .Where(c => c.Hierarchy == ConstraintHierarchy.Level4_QualitySoft)
                .ToList();

            // 计算物理软约束得分
            double physicalTotalScore = 0;
            double physicalTotalWeight = physicalSoftConstraints.Sum(c => c.Weight);

            foreach (var constraint in physicalSoftConstraints)
            {
                if (cachedScores.TryGetValue(constraint.Id, out double score))
                {
                    physicalTotalScore += score * constraint.Weight;
                }
                else
                {
                    // 缓存中缺少约束评分，认为得分为0
                    physicalTotalScore += 0;
                }
            }

            // 计算质量软约束得分
            double qualityTotalScore = 0;
            double qualityTotalWeight = qualitySoftConstraints.Sum(c => c.Weight);

            foreach (var constraint in qualitySoftConstraints)
            {
                if (cachedScores.TryGetValue(constraint.Id, out double score))
                {
                    qualityTotalScore += score * constraint.Weight;
                }
                else
                {
                    // 缓存中缺少约束评分，认为得分为0
                    qualityTotalScore += 0;
                }
            }

            // 如果没有约束，返回1.0
            double physicalSoftScore = physicalTotalWeight > 0
                ? physicalTotalScore / physicalTotalWeight
                : 1.0;

            double qualitySoftScore = qualityTotalWeight > 0
                ? qualityTotalScore / qualityTotalWeight
                : 1.0;

            // 加权组合得分
            return (_parameters.PhysicalSoftConstraintWeight * physicalSoftScore) +
                   (_parameters.QualitySoftConstraintWeight * qualitySoftScore);
        }
        /// <summary>
        /// 缓存约束评分
        /// </summary>
        /// <param name="solutionId">解决方案ID</param>
        /// <param name="constraintId">约束ID</param>
        /// <param name="score">约束得分</param>
        private void CacheConstraintScore(int solutionId, int constraintId, double score)
        {
            // 如果解决方案ID无效，不进行缓存
            if (solutionId <= 0)
                return;

            // 确保字典中存在该解决方案的缓存
            if (!_constraintScoreCache.ContainsKey(solutionId))
            {
                _constraintScoreCache[solutionId] = new Dictionary<int, double>();
            }

            // 缓存约束得分
            _constraintScoreCache[solutionId][constraintId] = score;

            // 可选：限制缓存大小，防止内存泄漏
            // 这里简单地限制最多缓存100个解决方案
            if (_constraintScoreCache.Count > 100)
            {
                // 移除最早的缓存
                var oldestSolutionId = _constraintScoreCache.Keys.Min();
                _constraintScoreCache.Remove(oldestSolutionId);
            }
        }
        // Add this method to the SolutionEvaluator class

        /// <summary>
        /// Get all active constraints
        /// </summary>
        public IEnumerable<IConstraint> GetAllActiveConstraints()
        {
            return _constraintManager.GetAllConstraints().Where(c => c.IsActive);
        }

        /// <summary>
        /// Get active hard constraints
        /// </summary>
        public IEnumerable<IConstraint> GetActiveHardConstraints()
        {
            return _constraintManager.GetHardConstraints();
        }

        /// <summary>
        /// Get active soft constraints
        /// </summary>
        public IEnumerable<IConstraint> GetActiveSoftConstraints()
        {
            return _constraintManager.GetSoftConstraints();
        }

        /// <summary>
        /// Get active constraints by hierarchy level
        /// </summary>
        public IEnumerable<IConstraint> GetActiveConstraintsByHierarchy(ConstraintHierarchy hierarchy)
        {
            return _constraintManager.GetAllConstraints()
                .Where(c => c.IsActive && c.Hierarchy == hierarchy);
        }

        /// <summary>
        /// 获取指定层级的软约束
        /// </summary>
        private List<IConstraint> GetSoftConstraintsByHierarchy(ConstraintHierarchy hierarchy)
        {
            return _constraintManager.GetAllConstraints()
                .Where(c => c.IsActive && !c.IsHard && c.Hierarchy == hierarchy)
                .ToList();
        }

        /// <summary>
        /// 评估并返回软约束评估结果
        /// </summary>
        private List<ConstraintEvaluation> EvaluateAndGetSoftConstraintEvaluations(SchedulingSolution solution)
        {
            var evaluations = new List<ConstraintEvaluation>();
            
            // 获取活跃的软约束
            var softConstraints = _constraintManager.GetSoftConstraints()
                .Where(c => c.IsActive)
                .ToList();
            
            // 按约束层级分组评估
            var physicalSoftConstraints = softConstraints.Where(c => c.Hierarchy == ConstraintHierarchy.Level3_PhysicalSoft).ToList();
            var qualitySoftConstraints = softConstraints.Where(c => c.Hierarchy == ConstraintHierarchy.Level4_QualitySoft).ToList();
            
            // 评估物理软约束
            foreach (var constraint in physicalSoftConstraints)
            {
                try
                {
                    var (score, conflicts) = constraint.Evaluate(solution);
                    var weight = constraint.Weight * _parameters.PhysicalSoftConstraintWeight;
                    var evaluation = new ConstraintEvaluation
                    {
                        Constraint = constraint,
                        Score = score,
                        Conflicts = conflicts
                    };
                    
                    evaluations.Add(evaluation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"评估约束 {constraint.Name} 时出错");
                }
            }
            
            // 评估质量软约束
            foreach (var constraint in qualitySoftConstraints)
            {
                try
                {
                    var (score, conflicts) = constraint.Evaluate(solution);
                    var weight = constraint.Weight * _parameters.QualitySoftConstraintWeight;
                    var evaluation = new ConstraintEvaluation
                    {
                        Constraint = constraint,
                        Score = score,
                        Conflicts = conflicts
                    };
                    
                    evaluations.Add(evaluation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"评估约束 {constraint.Name} 时出错");
                }
            }
            
            return evaluations;
        }
    }
}
