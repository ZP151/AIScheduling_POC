using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms.AlgorithmsImpl
{

    public class StructureOptimizer
    {
        private readonly Random _random = new Random();

        public void ApplyInitialPerturbation(
            SchedulingSolution solution,
            SchedulingProblem problem,
            ISolutionEvaluator evaluator,
            IConstraintManager constraintManager)
        {
            var assignments = solution.Assignments.OrderBy(_ => _random.Next()).ToList();

            foreach (var assignment in assignments)
            {
                var alternatives = FieldReplacer.GenerateFeasibleAlternatives(assignment, problem, solution, constraintManager);
                var best = alternatives
                    .Select(a => new { Option = a, Score = evaluator.EvaluateWithReplacement(solution, assignment, a).Score })
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault();

                if (best != null && best.Score > evaluator.Evaluate(solution).Score)
                {
                    solution.ReplaceAssignment(assignment, best.Option);
                    break;
                }
            }
        }

        public void OptimizeSolutionStep(
            SchedulingSolution solution,
            SchedulingProblem problem,
            ISolutionEvaluator evaluator,
            IConstraintManager constraintManager,
            double temperature,
            SchedulingAlgorithmConfig config)
        {
            var assignments = solution.Assignments.OrderBy(_ => _random.Next()).ToList();

            foreach (var assignment in assignments)
            {
                var alternatives = FieldReplacer.GenerateFeasibleAlternatives(assignment, problem, solution, constraintManager);

                foreach (var alt in alternatives)
                {
                    var currentScore = evaluator.Evaluate(solution).Score;
                    var newScore = evaluator.EvaluateWithReplacement(solution, assignment, alt).Score;
                    var delta = newScore - currentScore;

                    if (AnnealingAcceptance.ShouldAccept(delta, temperature))
                    {
                        solution.ReplaceAssignment(assignment, alt);
                        return;
                    }
                }
            }
        }
    }
}

