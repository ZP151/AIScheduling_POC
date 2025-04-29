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
    /// Solution evaluator interface
    /// </summary>
    public interface ISolutionEvaluator
    {
        SchedulingEvaluation Evaluate(SchedulingSolution solution);
    }

    /// <summary>
    /// Class for evaluating scheduling solutions
    /// </summary>
    public class SolutionEvaluator : ISolutionEvaluator
    {
        private readonly ILogger<SolutionEvaluator> _logger;
        private readonly ConstraintManager _constraintManager;
        private readonly Utils.SchedulingParameters _parameters;

        // Cache evaluation results to reduce duplicate calculations
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
        /// Evaluate solution, return 0-1 score (1 is best)
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
                // Check cache
                if (solution.Id > 0 && _constraintScoreCache.TryGetValue(solution.Id, out var cachedScores))
                {
                    // If all constraints have cached scores, return immediately
                    int totalConstraints = _constraintManager.GetAllConstraints().Count;
                    if (cachedScores.Count == totalConstraints)
                    {
                        double hardScore = EvaluateHardConstraintsFromCache(cachedScores);

                        // If hard constraints are not satisfied, return negative infinity
                        if (hardScore < 1.0)
                        {
                            evaluation.IsFeasible = false;
                            evaluation.Score = double.NegativeInfinity;
                            return evaluation;
                        }

                        // Calculate soft constraint score
                        double softScore = EvaluateSoftConstraintsFromCache(cachedScores);
                        evaluation.IsFeasible = true;
                        evaluation.Score = softScore;
                        return evaluation;
                    }
                }

                // If not all constraints have cached scores, recalculate
                double hardConstraintScore = EvaluateHardConstraints(solution);

                // If any hard constraint is violated, return negative infinity score
                if (hardConstraintScore < 1.0)
                {
                    evaluation.IsFeasible = false;
                    evaluation.Score = double.NegativeInfinity;
                    return evaluation;
                }

                // Evaluate soft constraints
                double physicalSoftScore = EvaluatePhysicalSoftConstraints(solution);
                double qualitySoftScore = EvaluateQualitySoftConstraints(solution);

                // Weighted combination score
                double softConstraintScore =
                    (_parameters.PhysicalSoftConstraintWeight * physicalSoftScore) +
                    (_parameters.QualitySoftConstraintWeight * qualitySoftScore);

                _logger.LogDebug($"Score: hard constraint={hardConstraintScore:F4}, physical soft constraint={physicalSoftScore:F4}, " +
                               $"quality soft constraint={qualitySoftScore:F4}, total score={softConstraintScore:F4}");

                evaluation.IsFeasible = true;
                evaluation.HardConstraintsSatisfactionLevel = hardConstraintScore;
                evaluation.SoftConstraintsSatisfactionLevel = softConstraintScore;
                evaluation.Score = softConstraintScore;
                return evaluation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating solution");
                evaluation.IsFeasible = false;
                evaluation.Score = double.NegativeInfinity;
                return evaluation;
            }
        }

        /// <summary>
        /// Evaluate hard constraints
        /// </summary>
        public double EvaluateHardConstraints(SchedulingSolution solution)
        {
            var hardConstraints = _constraintManager.GetHardConstraints();

            if (hardConstraints.Count == 0)
                return 1.0; // No hard constraints,is regarded as satisfying

            // Check if all hard constraints are satisfied
            foreach (var constraint in hardConstraints)
            {
                try
                {
                    var (score, conflicts) = constraint.Evaluate(solution);

                    // Cache result
                    if (solution.Id > 0)
                    {
                        CacheConstraintScore(solution.Id, constraint.Id, score);
                    }

                    // If any hard constraint is not satisfied, the overall solution is not satisfied
                    if (score < 1.0)
                    {
                        _logger.LogDebug($"Hard constraint '{constraint.Name}' is not satisfied, score={score:F4}");

                        if (conflicts != null && conflicts.Any())
                        {
                            _logger.LogDebug($"Conflict: {conflicts.First().Description}");
                        }

                        return 0.0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error evaluating hard constraint '{constraint.Name}'");
                    return 0.0; // Error视为约束不满足
                }
            }

            return 1.0; // All hard constraints are satisfied
        }
        /// <summary>
        /// Evaluate all soft constraints
        /// </summary>
        public double EvaluateSoftConstraints(SchedulingSolution solution)
        {
            double physicalSoftScore = EvaluatePhysicalSoftConstraints(solution);
            double qualitySoftScore = EvaluateQualitySoftConstraints(solution);

            // Calculate combined soft constraint score using parameter weights
            return (_parameters.PhysicalSoftConstraintWeight * physicalSoftScore) +
                   (_parameters.QualitySoftConstraintWeight * qualitySoftScore);
        }
        /// <summary>
        /// Evaluate physical soft constraints
        /// </summary>
        public double EvaluatePhysicalSoftConstraints(SchedulingSolution solution)
        {
            var physicalSoftConstraints = _constraintManager.GetSoftConstraints()
                .Where(c => c.Hierarchy == ConstraintHierarchy.Level3_PhysicalSoft)
                .ToList();

            if (physicalSoftConstraints.Count == 0)
                return 1.0; // No physical soft constraints, regarded as满分

            double totalScore = 0;
            double totalWeight = physicalSoftConstraints.Sum(c => c.Weight);

            if (totalWeight == 0)
                return 1.0; // Total weight is 0, regarded as满分

            foreach (var constraint in physicalSoftConstraints)
            {
                try
                {
                    var (score, conflicts) = constraint.Evaluate(solution);

                    // Cache result
                    if (solution.Id > 0)
                    {
                        CacheConstraintScore(solution.Id, constraint.Id, score);
                    }

                    totalScore += score * constraint.Weight;

                    _logger.LogDebug($"Physical soft constraint '{constraint.Name}' score={score:F4}," +
                                   $"weight={constraint.Weight:F2}," +
                                   $"conflict count={conflicts?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error evaluating physical soft constraint '{constraint.Name}'");
                    // Error scores 0 points for considering the constraint
                }
            }

            double weightedScore = totalScore / totalWeight;
            return weightedScore;
        }

        /// <summary>
        /// Evaluate quality soft constraints
        /// </summary>
        public double EvaluateQualitySoftConstraints(SchedulingSolution solution)
        {
            var qualitySoftConstraints = _constraintManager.GetSoftConstraints()
                .Where(c => c.Hierarchy == ConstraintHierarchy.Level4_QualitySoft)
                .ToList();

            if (qualitySoftConstraints.Count == 0)
                return 1.0; // No soft constraints on quality, considered a perfect score

            double totalScore = 0;
            double totalWeight = qualitySoftConstraints.Sum(c => c.Weight);

            if (totalWeight == 0)
                return 1.0; // Total weight is 0, regarded as满分

            foreach (var constraint in qualitySoftConstraints)
            {
                try
                {
                    var (score, conflicts) = constraint.Evaluate(solution);

                    // Cache result
                    if (solution.Id > 0)
                    {
                        CacheConstraintScore(solution.Id, constraint.Id, score);
                    }

                    totalScore += score * constraint.Weight;

                    _logger.LogDebug($"Quality soft constraint '{constraint.Name}' score={score:F4}," +
                                   $"weight={constraint.Weight:F2}," +
                                   $"conflict count={conflicts?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error evaluating quality soft constraint '{constraint.Name}'");
                    // Error scores 0 points for considering the constraint
                }
            }

            double weightedScore = totalScore / totalWeight;
            return weightedScore;
        }

        /// <summary>
        /// Evaluate hard constraints from cache
        /// </summary>
        private double EvaluateHardConstraintsFromCache(Dictionary<int, double> cachedScores)
        {
            var hardConstraints = _constraintManager.GetHardConstraints();

            if (hardConstraints.Count == 0)
                return 1.0; // No hard constraints, regarded as satisfying

            foreach (var constraint in hardConstraints)
            {
                if (cachedScores.TryGetValue(constraint.Id, out double score))
                {
                    if (score < 1.0)
                    {
                        return 0.0; // There is a hard constraint not satisfied
                    }
                }
                else
                {
                    // Cache missing constraint score, regarded as not satisfied
                    return 0.0;
                }
            }

            return 1.0; // All hard constraints are satisfied
        }

        /// <summary>
        /// Evaluate soft constraints from cache
        /// </summary>
        private double EvaluateSoftConstraintsFromCache(Dictionary<int, double> cachedScores)
        {
            var physicalSoftConstraints = _constraintManager.GetSoftConstraints()
                .Where(c => c.Hierarchy == ConstraintHierarchy.Level3_PhysicalSoft)
                .ToList();

            var qualitySoftConstraints = _constraintManager.GetSoftConstraints()
                .Where(c => c.Hierarchy == ConstraintHierarchy.Level4_QualitySoft)
                .ToList();

            // Calculate physical soft constraint score
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
                    // Cache missing constraint score, regarded as 0 points
                    physicalTotalScore += 0;
                }
            }

            // Calculate quality soft constraint score
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
                    // Cache missing constraint score, regarded as 0 points
                    qualityTotalScore += 0;
                }
            }

            // If there are no constraints, return 1.0
            double physicalSoftScore = physicalTotalWeight > 0
                ? physicalTotalScore / physicalTotalWeight
                : 1.0;

            double qualitySoftScore = qualityTotalWeight > 0
                ? qualityTotalScore / qualityTotalWeight
                : 1.0;

            // Weighted combination score
            return (_parameters.PhysicalSoftConstraintWeight * physicalSoftScore) +
                   (_parameters.QualitySoftConstraintWeight * qualitySoftScore);
        }
        /// <summary>
        /// Cache constraint score
        /// </summary>
        /// <param name="solutionId">Solution ID</param>
        /// <param name="constraintId">Constraint ID</param>
        /// <param name="score">Constraint score</param>
        private void CacheConstraintScore(int solutionId, int constraintId, double score)
        {
            // If solution ID is invalid, do not cache
            if (solutionId <= 0)
                return;

            // Ensure the dictionary contains the solution cache
            if (!_constraintScoreCache.ContainsKey(solutionId))
            {
                _constraintScoreCache[solutionId] = new Dictionary<int, double>();
            }

            // Cache constraint score
            _constraintScoreCache[solutionId][constraintId] = score;

            // Optional: Limit cache size to prevent memory leaks
            // Here we simply limit the maximum cache size to 100 solutions
            if (_constraintScoreCache.Count > 100)
            {
                // Remove the oldest cache
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
        /// Get soft constraints by hierarchy level
        /// </summary>
        private List<IConstraint> GetSoftConstraintsByHierarchy(ConstraintHierarchy hierarchy)
        {
            return _constraintManager.GetAllConstraints()
                .Where(c => c.IsActive && !c.IsHard && c.Hierarchy == hierarchy)
                .ToList();
        }

        /// <summary>
        /// Evaluate and return soft constraint evaluations
        /// </summary>
        private List<ConstraintEvaluation> EvaluateAndGetSoftConstraintEvaluations(SchedulingSolution solution)
        {
            var evaluations = new List<ConstraintEvaluation>();
            
            // Get active soft constraints
            var softConstraints = _constraintManager.GetSoftConstraints()
                .Where(c => c.IsActive)
                .ToList();
            
            // Evaluate by constraint hierarchy
            var physicalSoftConstraints = softConstraints.Where(c => c.Hierarchy == ConstraintHierarchy.Level3_PhysicalSoft).ToList();
            var qualitySoftConstraints = softConstraints.Where(c => c.Hierarchy == ConstraintHierarchy.Level4_QualitySoft).ToList();
            
            // Evaluate physical soft constraints
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
                    _logger.LogError(ex, $"Error evaluating constraint {constraint.Name}");
                }
            }
            
            // Evaluate quality soft constraints
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
                    _logger.LogError(ex, $"Error evaluating constraint {constraint.Name}");
                }
            }
            
            return evaluations;
        }
    }
}
