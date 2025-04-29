using Google.OrTools.Sat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// Enhanced CP callback for generating diverse solutions
    /// </summary>
    public class DiverseSolutionCallback : CpSolverSolutionCallback
    {
        private readonly Dictionary<string, IntVar> _variables;
        private readonly int _maxSolutions;
        private readonly CpModel _model;
        private int _solutionCount = 0;
        private readonly double _diversityThreshold;

        /// <summary>
        /// Collected solutions
        /// </summary>
        public List<Dictionary<string, long>> Solutions { get; } = new List<Dictionary<string, long>>();

        // Use HashSet to record features of found solutions
        private readonly HashSet<string> _solutionSignatures = new HashSet<string>();

        public DiverseSolutionCallback(
            Dictionary<string, IntVar> variables,
            int maxSolutions,
            CpModel model,
            double diversityThreshold = 0.2)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _maxSolutions = maxSolutions;
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _diversityThreshold = diversityThreshold;
        }

        public override void OnSolutionCallback()
        {
            // Calculate feature signature of current solution
            string signature = ComputeSolutionSignature();

            // If it's a new solution structure
            if (!_solutionSignatures.Contains(signature))
            {
                _solutionSignatures.Add(signature);

                // Collect solution
                var solution = new Dictionary<string, long>();
                foreach (var entry in _variables)
                {
                    solution[entry.Key] = Value(entry.Value);
                }

                // Check diversity with existing solutions
                if (IsSufficientlyDiverse(solution))
                {
                    Solutions.Add(solution);
                    _solutionCount++;

                    // Add constraint to exclude current solution to promote diversity
                    AddDiversificationConstraint();
                }
            }

            // If enough solutions found, stop search
            if (_solutionCount >= _maxSolutions)
            {
                StopSearch();
            }
        }

        /// <summary>
        /// Calculate solution feature signature for identifying different solution structures
        /// </summary>
        private string ComputeSolutionSignature()
        {
            // Only consider variables with value 1 (indicating selected assignments)
            var assignmentVars = _variables
                .Where(kv => kv.Key.Contains("_") && Value(kv.Value) == 1)
                .Select(kv => kv.Key)
                .OrderBy(k => k)
                .ToList();

            var sb = new StringBuilder();
            foreach (var varName in assignmentVars)
            {
                sb.Append(varName).Append(';');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Check if current solution is sufficiently diverse from existing solutions
        /// </summary>
        private bool IsSufficientlyDiverse(Dictionary<string, long> newSolution)
        {
            // If it's the first solution, accept directly
            if (Solutions.Count == 0)
            {
                return true;
            }

            // Calculate diversity between current solution and each existing solution
            foreach (var existingSolution in Solutions)
            {
                double diversityScore = CalculateDiversity(newSolution, existingSolution);

                // If too similar to any existing solution, reject this solution
                if (diversityScore < _diversityThreshold)
                {
                    return false;
                }
            }

            // Sufficiently different from all existing solutions
            return true;
        }

        /// <summary>
        /// Calculate diversity between two solutions (0-1 range, 1 means completely different)
        /// </summary>
        private double CalculateDiversity(Dictionary<string, long> solution1, Dictionary<string, long> solution2)
        {
            // Calculate variables with value 1 in both solutions (indicating selected assignments)
            var selectedVars1 = solution1
                .Where(kv => kv.Value == 1)
                .Select(kv => kv.Key)
                .ToHashSet();

            var selectedVars2 = solution2
                .Where(kv => kv.Value == 1)
                .Select(kv => kv.Key)
                .ToHashSet();

            // Calculate number of commonly selected variables
            int commonVars = selectedVars1.Intersect(selectedVars2).Count();

            // Calculate total number of selected variables
            int totalVars = selectedVars1.Count + selectedVars2.Count - commonVars;

            // Diversity = 1 - proportion of common parts
            return totalVars > 0 ? 1.0 - ((double)commonVars / totalVars) : 0.0;
        }

        /// <summary>
        /// Add constraint to exclude current solution
        /// </summary>
        private void AddDiversificationConstraint()
        {
            try
            {
                // Get all assignment variables with value 1
                var activeVars = new List<IntVar>();
                var activeVarIndices = new List<string>();

                foreach (var entry in _variables)
                {
                    if (Value(entry.Value) == 1)
                    {
                        activeVars.Add(entry.Value);
                        activeVarIndices.Add(entry.Key);
                    }
                }

                // Create constraint: sum(activeVars) <= activeVars.Count - 1
                // This ensures next solution differs from current solution in at least one assignment
                if (activeVars.Count > 0)
                {
                    var constraint = LinearExpr.Sum(activeVars.ToArray());
                    _model.Add(constraint <= activeVars.Count - 1);
                }
            }
            catch (Exception ex)
            {
                // In actual project should use logging for this exception
                Console.WriteLine($"Error adding diversity constraint: {ex.Message}");
            }
        }

        /// <summary>
        /// Get number of solutions found
        /// </summary>
        public int SolutionCount => _solutionCount;
    }
}