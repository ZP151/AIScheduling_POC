using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using SmartSchedulingSystem.Scheduling.Algorithms;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;

namespace SchedulingSystem.Scheduling.Algorithms
{
    /// <summary>
    /// Simulated annealing algorithm implementation for optimizing scheduling solutions
    /// </summary>
    public class SimulatedAnnealingAlgorithm : ISchedulingAlgorithm
    {
        private readonly ISolutionEvaluator _evaluator;
        private readonly IConstraintManager _constraintManager;
        private readonly IConflictResolver _conflictResolver;
        private readonly ILogger<SimulatedAnnealingAlgorithm> _logger;
        private readonly Random _random;

        private SchedulingProblem _problem;
        private SchedulingParameters _parameters;

        public SimulatedAnnealingAlgorithm(
            ISolutionEvaluator evaluator,
            IConstraintManager constraintManager,
            IConflictResolver conflictResolver,
            ILogger<SimulatedAnnealingAlgorithm> logger,
            int? randomSeed = null)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _conflictResolver = conflictResolver ?? throw new ArgumentNullException(nameof(conflictResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _random = new Random(randomSeed ?? Environment.TickCount);
        }

        public void Initialize(
            SchedulingProblem problem,
            SchedulingParameters parameters,
            IConstraintManager constraintManager,
            IConflictResolver conflictResolver,
            ISolutionEvaluator evaluator)
        {
            _problem = problem ?? throw new ArgumentNullException(nameof(problem));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            // We already have these injected, so we don't need to reassign them
        }

        public Task<SchedulingSolution> GenerateInitialSolutionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Simulated annealing should not be used to generate initial solutions directly.");
        }

        /// <summary>
        /// Optimize an existing scheduling solution using simulated annealing
        /// </summary>
        public async Task<SchedulingSolution> OptimizeSolutionAsync(SchedulingSolution solution, CancellationToken cancellationToken = default)
        {
            if (_problem == null || _parameters == null)
                throw new InvalidOperationException("Must call Initialize before optimizing solution");

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            _logger.LogInformation("Starting simulated annealing optimization");

            // Extract SA parameters
            double initialTemperature = _parameters.AlgorithmConfig.SimulatedAnnealingInitialTemperature;
            double coolingRate = _parameters.AlgorithmConfig.SimulatedAnnealingCoolingRate;
            int iterationsPerTemperature = _parameters.AlgorithmConfig.SimulatedAnnealingIterationsPerTemperature;
            double minTemperature = _parameters.AlgorithmConfig.SimulatedAnnealingMinTemperature;

            // Copy the current solution as both current and best solution
            var currentSolution = solution.Clone();
            var bestSolution = solution.Clone();

            // Evaluate the initial solution
            var currentEvaluation = _evaluator.Evaluate(currentSolution);
            var bestEvaluation = currentEvaluation;

            _logger.LogDebug($"Initial solution score: {currentEvaluation.Score:F4}, Feasible: {currentEvaluation.IsFeasible}");

            // Initialize temperature
            double temperature = initialTemperature;

            // Counter for non-improving moves
            int nonImprovingMoves = 0;

            // Main SA loop
            while (temperature > minTemperature && !cancellationToken.IsCancellationRequested)
            {
                // Reset improvements counter for this temperature
                bool improvementAtCurrentTemp = false;

                // Perform several iterations at current temperature
                for (int i = 0; i < iterationsPerTemperature && !cancellationToken.IsCancellationRequested; i++)
                {
                    // Generate a neighbor solution
                    var neighborSolution = GenerateNeighbor(currentSolution);

                    // Evaluate the neighbor
                    var neighborEvaluation = _evaluator.Evaluate(neighborSolution);

                    // Decide whether to accept the neighbor
                    bool accept = false;

                    // If the neighbor is better, always accept it
                    if (IsBetterSolution(neighborEvaluation, currentEvaluation))
                    {
                        accept = true;
                        improvementAtCurrentTemp = true;
                        nonImprovingMoves = 0;
                    }
                    // Otherwise, accept with a probability that decreases with temperature
                    else
                    {
                        double delta = CalculateDelta(neighborEvaluation, currentEvaluation);
                        double acceptanceProbability = Math.Exp(delta / temperature);

                        if (_random.NextDouble() < acceptanceProbability)
                        {
                            accept = true;
                            nonImprovingMoves++;
                        }
                    }

                    // Update current solution if accepted
                    if (accept)
                    {
                        currentSolution = neighborSolution;
                        currentEvaluation = neighborEvaluation;

                        // Update best solution if better
                        if (IsBetterSolution(currentEvaluation, bestEvaluation))
                        {
                            bestSolution = currentSolution.Clone();
                            bestEvaluation = currentEvaluation;

                            _logger.LogDebug($"Temperature {temperature:F2}: New best solution found! Score: {bestEvaluation.Score:F4}, Feasible: {bestEvaluation.IsFeasible}");
                        }
                    }
                }

                // Reduce temperature
                temperature *= coolingRate;

                // Log progress periodically
                if (temperature < initialTemperature / 2 &&
                    temperature > initialTemperature / 2 * coolingRate)
                {
                    _logger.LogInformation($"Simulated annealing 50% complete. Current temperature: {temperature:F2}, Best score: {bestEvaluation.Score:F4}");
                }

                // Early stopping if no improvements for too long
                if (!improvementAtCurrentTemp)
                {
                    nonImprovingMoves += iterationsPerTemperature;

                    // If no improvements for a long time, break early
                    if (nonImprovingMoves > iterationsPerTemperature * 10)
                    {
                        _logger.LogInformation($"Early stopping after {nonImprovingMoves} non-improving moves");
                        break;
                    }
                }
                else
                {
                    nonImprovingMoves = 0;
                }

                // Periodically reset to best solution to avoid getting stuck
                if (_random.NextDouble() < 0.1) // 10% chance
                {
                    currentSolution = bestSolution.Clone();
                    currentEvaluation = bestEvaluation;
                }
            }

            _logger.LogInformation($"Simulated annealing optimization complete. Best score: {bestEvaluation.Score:F4}, Feasible: {bestEvaluation.IsFeasible}");
            return bestSolution;
        }

        /// <summary>
        /// Generate a neighbor solution by making a small change to the given solution
        /// </summary>
        private SchedulingSolution GenerateNeighbor(SchedulingSolution solution)
        {
            // Clone the solution
            var neighbor = solution.Clone();

            // Choose a random mutation operation
            int operation = _random.Next(4);

            switch (operation)
            {
                case 0: // Swap time slots between two assignments
                    SwapTimeSlots(neighbor);
                    break;
                case 1: // Swap classrooms between two assignments
                    SwapClassrooms(neighbor);
                    break;
                case 2: // Swap teachers between two assignments
                    SwapTeachers(neighbor);
                    break;
                case 3: // Reassign one assignment to a new time slot, classroom or teacher
                    ReassignRandomAssignment(neighbor);
                    break;
            }

            return neighbor;
        }

        /// <summary>
        /// Swap time slots between two random assignments
        /// </summary>
        private void SwapTimeSlots(SchedulingSolution solution)
        {
            if (solution.Assignments.Count < 2)
                return;

            // Choose two random assignments
            int idx1 = _random.Next(solution.Assignments.Count);
            int idx2;
            do
            {
                idx2 = _random.Next(solution.Assignments.Count);
            } while (idx2 == idx1);

            var assignment1 = solution.Assignments[idx1];
            var assignment2 = solution.Assignments[idx2];

            // Create new assignments with swapped time slots
            var newAssignment1 = assignment1.Clone();
            newAssignment1.TimeSlotId = assignment2.TimeSlotId;
            newAssignment1.DayOfWeek = assignment2.DayOfWeek;
            newAssignment1.StartTime = assignment2.StartTime;
            newAssignment1.EndTime = assignment2.EndTime;

            var newAssignment2 = assignment2.Clone();
            newAssignment2.TimeSlotId = assignment1.TimeSlotId;
            newAssignment2.DayOfWeek = assignment1.DayOfWeek;
            newAssignment2.StartTime = assignment1.StartTime;
            newAssignment2.EndTime = assignment1.EndTime;

            // Remove old assignments
            solution.RemoveAssignment(assignment1.Id);
            solution.RemoveAssignment(assignment2.Id);

            // Try to add new assignments
            bool success1 = solution.AddAssignment(newAssignment1);
            bool success2 = solution.AddAssignment(newAssignment2);

            // If either addition failed, restore original assignments
            if (!success1 || !success2)
            {
                // Remove any new assignments that might have been added
                if (success1)
                    solution.RemoveAssignment(newAssignment1.Id);
                if (success2)
                    solution.RemoveAssignment(newAssignment2.Id);

                // Restore original assignments
                solution.AddAssignment(assignment1);
                solution.AddAssignment(assignment2);
            }
        }

        /// <summary>
        /// Swap classrooms between two random assignments
        /// </summary>
        private void SwapClassrooms(SchedulingSolution solution)
        {
            if (solution.Assignments.Count < 2)
                return;

            // Choose two random assignments
            int idx1 = _random.Next(solution.Assignments.Count);
            int idx2;
            do
            {
                idx2 = _random.Next(solution.Assignments.Count);
            } while (idx2 == idx1);

            var assignment1 = solution.Assignments[idx1];
            var assignment2 = solution.Assignments[idx2];

            // Create new assignments with swapped classrooms
            var newAssignment1 = assignment1.Clone();
            newAssignment1.ClassroomId = assignment2.ClassroomId;
            newAssignment1.ClassroomName = assignment2.ClassroomName;

            var newAssignment2 = assignment2.Clone();
            newAssignment2.ClassroomId = assignment1.ClassroomId;
            newAssignment2.ClassroomName = assignment1.ClassroomName;

            // Remove old assignments
            solution.RemoveAssignment(assignment1.Id);
            solution.RemoveAssignment(assignment2.Id);

            // Try to add new assignments
            bool success1 = solution.AddAssignment(newAssignment1);
            bool success2 = solution.AddAssignment(newAssignment2);

            // If either addition failed, restore original assignments
            if (!success1 || !success2)
            {
                // Remove any new assignments that might have been added
                if (success1)
                    solution.RemoveAssignment(newAssignment1.Id);
                if (success2)
                    solution.RemoveAssignment(newAssignment2.Id);

                // Restore original assignments
                solution.AddAssignment(assignment1);
                solution.AddAssignment(assignment2);
            }
        }

        /// <summary>
        /// Swap teachers between two random assignments
        /// </summary>
        private void SwapTeachers(SchedulingSolution solution)
        {
            if (solution.Assignments.Count < 2)
                return;

            // Choose two random assignments
            int idx1 = _random.Next(solution.Assignments.Count);
            int idx2;
            do
            {
                idx2 = _random.Next(solution.Assignments.Count);
            } while (idx2 == idx1);

            var assignment1 = solution.Assignments[idx1];
            var assignment2 = solution.Assignments[idx2];

            // Create new assignments with swapped teachers
            var newAssignment1 = assignment1.Clone();
            newAssignment1.TeacherId = assignment2.TeacherId;
            newAssignment1.TeacherName = assignment2.TeacherName;

            var newAssignment2 = assignment2.Clone();
            newAssignment2.TeacherId = assignment1.TeacherId;
            newAssignment2.TeacherName = assignment1.TeacherName;

            // Remove old assignments