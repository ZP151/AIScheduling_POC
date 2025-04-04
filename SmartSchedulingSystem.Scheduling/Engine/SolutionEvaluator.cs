using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Engine;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    public interface ISolutionEvaluator
    {
        SchedulingEvaluation Evaluate(SchedulingSolution solution);
        double EvaluateConstraint(IConstraint constraint, SchedulingSolution solution);
        double EvaluateHardConstraints(SchedulingSolution solution);
        double EvaluateSoftConstraints(SchedulingSolution solution);
    }

    public class SolutionEvaluator : ISolutionEvaluator
    {
        private readonly IConstraintManager _constraintManager;

        public SolutionEvaluator(IConstraintManager constraintManager)
        {
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
        }

        /// <summary>
        /// 评估解决方案
        /// </summary>
        public double Evaluate(SchedulingSolution solution)
        {
            double hardConstraintScore = EvaluateHardConstraints(solution);

            // 如果违反任何硬约束，返回负分数
            if (hardConstraintScore < 1.0)
            {
                return double.NegativeInfinity;
            }

            // 评估软约束
            double softConstraintScore = EvaluateSoftConstraints(solution);

            return softConstraintScore;
        }

        /// <summary>
        /// 评估硬约束
        /// </summary>
        public double EvaluateHardConstraints(SchedulingSolution solution)
        {
            var hardConstraints = _constraintManager.GetHardConstraints();

            if (hardConstraints.Count == 0)
                return 1.0; // 没有硬约束，视为满足

            foreach (var constraint in hardConstraints)
            {
                if (!constraint.IsSatisfied(solution))
                {
                    return 0.0; // 任一硬约束不满足，则整体不满足
                }
            }

            return 1.0; // 所有硬约束都满足
        }

        /// <summary>
        /// 评估软约束
        /// </summary>
        public double EvaluateSoftConstraints(SchedulingSolution solution)
        {
            var softConstraints = _constraintManager.GetSoftConstraints();

            if (softConstraints.Count == 0)
                return 1.0; // 没有软约束，视为满分

            double totalScore = 0;
            double totalWeight = softConstraints.Sum(c => c.Weight);

            if (totalWeight == 0)
                return 1.0; // 权重总和为0，视为满分

            foreach (var constraint in softConstraints)
            {
                var (satisfaction, conflicts) = constraint.Evaluate(solution);
                totalScore += satisfaction * constraint.Weight;
            }

            return totalScore / totalWeight; // 返回加权平均分
        }

        public double EvaluateConstraint(IConstraint constraint, SchedulingSolution solution)
        {
            return EvaluateConstraintInternal(constraint, solution).Score;
        }

        private ConstraintEvaluation EvaluateConstraintInternal(IConstraint constraint, SchedulingSolution solution)
        {
            var result = new ConstraintEvaluation
            {
                Constraint = constraint,
                Conflicts = new List<SchedulingConflict>()
            };

            try
            {
                var (score, conflicts) = constraint.Evaluate(solution);
                result.Score = score; 
                if (conflicts != null)
                {
                    result.Conflicts.AddRange(conflicts);
                }
            }
            catch (Exception ex)
            {
                // 出错时将其视为评估失败
                result.Score = 0.0;
                result.Conflicts.Add(new SchedulingConflict
                {
                    Type = SchedulingConflictType.ConstraintEvaluationError,
                    Description = $"评估约束失败：{ex.Message}",
                    Severity = ConflictSeverity.Severe
                });
            }

            return result;
        }

        SchedulingEvaluation ISolutionEvaluator.Evaluate(SchedulingSolution solution)
        {
            throw new NotImplementedException();
        }
    }
}