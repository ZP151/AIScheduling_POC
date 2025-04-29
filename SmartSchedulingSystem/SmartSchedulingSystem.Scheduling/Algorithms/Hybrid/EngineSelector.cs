using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.Hybrid
{
    /// <summary>
    /// Tool for selecting or allocating computational resources to different engines based on problem characteristics
    /// </summary>
    public class EngineSelector
    {
        /// <summary>
        /// Determine weights for different engines based on problem characteristics
        /// </summary>
        public EngineWeights DetermineWeights(SchedulingProblem problem)
        {
            // Analyze problem characteristics
            var features = ExtractProblemFeatures(problem);

            // Calculate engine weights based on features
            var weights = new EngineWeights
            {
                // Problems with high hard constraint ratio are more suitable for CP engine
                CPWeight = 0.3 + 0.5 * features.HardConstraintRatio,

                // Default LS engine weight, ensure CP+LS=1
                LSWeight = 0.7 - 0.5 * features.HardConstraintRatio
            };

            return weights;
        }

        /// <summary>
        /// Extract problem features
        /// </summary>
        private ProblemFeatures ExtractProblemFeatures(SchedulingProblem problem)
        {
            var features = new ProblemFeatures();

            // Calculate constraint ratio
            if (problem.Constraints != null && problem.Constraints.Count > 0)
            {
                int hardConstraintCount = problem.Constraints.Count(c => c.IsHard);
                features.HardConstraintRatio = (double)hardConstraintCount / problem.Constraints.Count;
            }

            // Calculate problem size
            features.ProblemSize = CalculateProblemSize(problem);

            // Evaluate constraint complexity
            features.ConstraintComplexity = CalculateConstraintComplexity(problem);

            return features;
        }

        /// <summary>
        /// Calculate problem size (0-1 range)
        /// </summary>
        private double CalculateProblemSize(SchedulingProblem problem)
        {
            // Simple version: Estimate based on course count
            int courseCount = problem.CourseSections?.Count ?? 0;

            // Define course count thresholds
            const int smallProblem = 20;
            const int mediumProblem = 100;
            const int largeProblem = 500;

            if (courseCount <= smallProblem)
            {
                return (double)courseCount / smallProblem * 0.33; // 0-0.33
            }
            else if (courseCount <= mediumProblem)
            {
                return 0.33 + (courseCount - smallProblem) / (double)(mediumProblem - smallProblem) * 0.33; // 0.33-0.66
            }
            else
            {
                double sizeFactor = Math.Min(1.0, (double)courseCount / largeProblem);
                return 0.66 + sizeFactor * 0.34; // 0.66-1.0
            }
        }

        /// <summary>
        /// Calculate constraint complexity (0-1 range)
        /// </summary>
        private double CalculateConstraintComplexity(SchedulingProblem problem)
        {
            // Simplified version: Estimate based on constraint count and types

            double complexityScore = 0.5; // Default medium complexity

            if (problem.Constraints != null && problem.Constraints.Count > 0)
            {
                // Constraint count factor (0-0.5)
                int constraintCount = problem.Constraints.Count;
                double countFactor = Math.Min(0.5, constraintCount / 20.0);

                // Constraint type diversity factor (0-0.5)
                var constraintTypes = problem.Constraints.Select(c => c.GetType().Name).Distinct().Count();
                double diversityFactor = Math.Min(0.5, constraintTypes / 10.0);

                complexityScore = countFactor + diversityFactor;
            }

            return complexityScore;
        }
    }

    /// <summary>
    /// Represents weights for each engine
    /// </summary>
    public class EngineWeights
    {
        /// <summary>
        /// CP engine weight
        /// </summary>
        public double CPWeight { get; set; } = 0.5;

        /// <summary>
        /// LS engine weight
        /// </summary>
        public double LSWeight { get; set; } = 0.5;
    }

    /// <summary>
    /// Represents problem features
    /// </summary>
    public class ProblemFeatures
    {
        /// <summary>
        /// Hard constraint ratio
        /// </summary>
        public double HardConstraintRatio { get; set; } = 0.5;

        /// <summary>
        /// Problem size (0-1)
        /// </summary>
        public double ProblemSize { get; set; } = 0.5;

        /// <summary>
        /// Constraint complexity (0-1)
        /// </summary>
        public double ConstraintComplexity { get; set; } = 0.5;
    }
}