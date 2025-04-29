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
    /// Local search optimizer, used to optimize soft constraint satisfaction of solutions that already satisfy hard constraints
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
        /// Optimize multiple initial solutions
        /// </summary>
        /// <param name="initialSolutions">List of initial solutions</param>
        /// <returns>List of optimized solutions</returns>
        public List<SchedulingSolution> OptimizeSolutions(List<SchedulingSolution> initialSolutions)
        {
            if (initialSolutions == null || initialSolutions.Count == 0)
            {
                _logger.LogWarning("Input initial solution list is empty");
                return new List<SchedulingSolution>();
            }

            _logger.LogInformation($"Starting to optimize {initialSolutions.Count} initial solutions");

            // Optimize solutions in parallel
            var optimizedSolutions = new List<SchedulingSolution>();

            if (_parameters.EnableParallelOptimization)
            {
                // Use parallel processing
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
                            _logger.LogError(ex, $"Error optimizing solution {solution.Id}");
                            return solution; // If optimization fails, return original solution
                        }
                    })
                    .ToList();
            }
            else
            {
                // Serial processing
                foreach (var solution in initialSolutions)
                {
                    try
                    {
                        optimizedSolutions.Add(OptimizeSolution(solution));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error optimizing solution {solution.Id}");
                        optimizedSolutions.Add(solution); // If optimization fails, add original solution
                    }
                }
            }

            _logger.LogInformation($"Completed optimization of {optimizedSolutions.Count} solutions");

            return optimizedSolutions;
        }
        /// <summary>
        /// Optimize specified solution
        /// </summary>
        /// <param name="initialSolution">Initial solution</param>
        /// <returns>Optimized solution</returns>
        public SchedulingSolution OptimizeSolution(SchedulingSolution initialSolution)
        {
            _logger.LogInformation("Starting local search optimization...");

            // Deep copy initial solution
            var currentSolution = initialSolution.Clone();
            var bestSolution = initialSolution.Clone();

            // Save current constraint application level
            var currentConstraintLevel = SmartSchedulingSystem.Scheduling.Engine.GlobalConstraintManager.Current?.GetCurrentApplicationLevel() 
                                       ?? Engine.ConstraintApplicationLevel.Basic;
            
            _logger.LogDebug($"Using constraint level {currentConstraintLevel} for local search optimization");

            // First evaluation of solution
            var currentEvaluation = _evaluator.Evaluate(currentSolution);
            double bestScore = currentEvaluation.Score;

            _logger.LogInformation("Initial solution score: {Score}", bestScore);

            // Reset simulated annealing controller
            _saController.Reset();

            int iteration = 0;
            int noImprovementCount = 0;
            const int MAX_NO_IMPROVEMENT = 100;

            // Pre-calculate and cache initial satisfaction for each constraint
            var constraintScores = new Dictionary<int, double>();
            var allConstraints = _evaluator.GetAllActiveConstraints().ToList();

            foreach (var constraint in allConstraints)
            {
                var (score, _) = constraint.Evaluate(currentSolution);
                constraintScores[constraint.Id] = score;
            }

            // Iterative optimization
            while (!_saController.Cool())
            {
                iteration++;

                try
                {
                    // Find constraint with lowest satisfaction
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
                        _logger.LogDebug("No constraints found needing optimization, skipping iteration");
                        continue;
                    }

                    // Find corresponding constraint object
                    var targetConstraint = allConstraints.FirstOrDefault(c => c.Id == weakestConstraintId);
                    if (targetConstraint == null)
                    {
                        _logger.LogWarning("Cannot find constraint with ID {id}", weakestConstraintId);
                        continue;
                    }

                    // Analyze constraint and generate moves
                    var constraintAnalysis = _constraintAnalyzer.AnalyzeSolution(currentSolution);
                    var assignments = constraintAnalysis.GetAssignmentsAffectedByConstraint(currentSolution, targetConstraint);

                    if (assignments.Count == 0)
                    {
                        assignments = currentSolution.Assignments
                            .OrderBy(a => Guid.NewGuid())
                            .Take(3)
                            .ToList();
                    }

                    // Select a random assignment to modify
                    var targetAssignment = assignments.OrderBy(a => Guid.NewGuid()).First();
                    var moves = _moveGenerator.GenerateValidMoves(currentSolution, targetAssignment, 5);

                    if (moves.Count == 0)
                    {
                        _logger.LogDebug("Iteration {Iteration}: No valid moves found", iteration);
                        continue;
                    }
                    IMove bestMove = SelectBestMove(moves, currentSolution);

                    // Apply move and evaluate
                    var newSolution = bestMove.Apply(currentSolution);
                    
                    // Ensure solution after applying move still satisfies current constraint level requirements
                    var hardConstraintsSatisfied = _evaluator.EvaluateHardConstraints(newSolution) >= 1.0;
                    if (!hardConstraintsSatisfied)
                    {
                        _logger.LogDebug("Iteration {Iteration}: Move {MoveDescription} violates hard constraints, rejected", 
                            iteration, bestMove.GetDescription());
                        continue;
                    }
                    
                    double newScore = _evaluator.Evaluate(newSolution).Score;

                    // Decide whether to accept new solution
                    bool acceptMove = _saController.ShouldAccept(bestScore, newScore);

                    if (acceptMove)
                    {
                        _logger.LogDebug("Iteration {Iteration}: Accepting move {MoveDescription}, new score: {NewScore}",
                            iteration, bestMove.GetDescription(), newScore);

                        currentSolution = newSolution;
                        
                        // Save constraint level used by current solution
                        currentSolution.ConstraintLevel = currentConstraintLevel;

                        // Update constraint score cache
                        foreach (var constraint in allConstraints)
                        {
                            var (score, _) = constraint.Evaluate(currentSolution);
                            constraintScores[constraint.Id] = score;
                        }

                        // If new solution is better, update best solution
                        if (newScore > bestScore)
                        {
                            bestSolution = newSolution;
                            bestScore = newScore;
                            _logger.LogInformation("Iteration {Iteration}: Found better solution, score: {Score}", iteration, bestScore);
                            noImprovementCount = 0;
                        }
                        else
                        {
                            noImprovementCount++;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Iteration {Iteration}: Rejected move {MoveDescription}, new score: {NewScore}",
                            iteration, bestMove.GetDescription(), newScore);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in iteration {Iteration}", iteration);
                }
            }

            _logger.LogInformation("Local search optimization completed, best score: {Score}", bestScore);
            return bestSolution;
        }

        /// <summary>
        /// Evaluate and select the best move
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
        /// Optimize solution with specified parameters
        /// </summary>
        /// <param name="initialSolution">Initial solution</param>
        /// <param name="maxIterations">Maximum number of iterations</param>
        /// <param name="initialTemperature">Initial temperature</param>
        /// <param name="coolingRate">Cooling rate</param>
        /// <returns>Optimized solution</returns>
        public SchedulingSolution OptimizeSolution(
            SchedulingSolution initialSolution, 
            int maxIterations, 
            double initialTemperature, 
            double coolingRate)
        {
            _logger.LogInformation("Using specified parameters to start local search optimization...");
            _logger.LogInformation($"Parameters: maxIterations={maxIterations}, initialTemperature={initialTemperature}, coolingRate={coolingRate}");
            
            // Save original parameters
            var originalMaxIterations = _parameters.MaxLsIterations;
            var originalTemperature = _parameters.InitialTemperature;
            var originalCoolingRate = _parameters.CoolingRate;
            
            try
            {
                // Apply new parameters
                _parameters.MaxLsIterations = maxIterations;
                _parameters.InitialTemperature = initialTemperature;
                _parameters.CoolingRate = coolingRate;
                
                // Reset simulated annealing controller and apply new parameters
                _saController.Reset(initialTemperature, coolingRate);
                
                // Call standard optimization method
                return OptimizeSolution(initialSolution);
            }
            finally
            {
                // Restore original parameters
                _parameters.MaxLsIterations = originalMaxIterations;
                _parameters.InitialTemperature = originalTemperature;
                _parameters.CoolingRate = originalCoolingRate;
            }
        }

    }
}