using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using SmartSchedulingSystem.Scheduling.Algorithms;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;

namespace SchedulingSystem.Scheduling.Algorithms.AlgorithmsImpl
{
    /// <summary>
    /// Hybrid Genetic Algorithm + Simulated Annealing implementation
    /// Combines global search capabilities of GA with local search abilities of SA
    /// </summary>
    public class HybridGASAAlgorithm : ISchedulingAlgorithm
    {
        private readonly ISolutionEvaluator _evaluator;
        private readonly IConstraintManager _constraintManager;
        private readonly IConflictResolver _conflictResolver;
        private readonly SimulatedAnnealingAlgorithm _simulatedAnnealing;
        private readonly ILogger<HybridGASAAlgorithm> _logger;
        private readonly Random _random;

        private SchedulingProblem _problem;
        private SchedulingParameters _parameters;

        public HybridGASAAlgorithm(
            ISolutionEvaluator evaluator,
            IConstraintManager constraintManager,
            IConflictResolver conflictResolver,
            SimulatedAnnealingAlgorithm simulatedAnnealing,
            ILogger<HybridGASAAlgorithm> logger,
            int? randomSeed = null)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _conflictResolver = conflictResolver ?? throw new ArgumentNullException(nameof(conflictResolver));
            _simulatedAnnealing = simulatedAnnealing ?? throw new ArgumentNullException(nameof(simulatedAnnealing));
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
            // Initialize SA with the same parameters
            _simulatedAnnealing.Initialize(problem, parameters, constraintManager, conflictResolver, evaluator);
        }

        public Task<SchedulingSolution> GenerateInitialSolutionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Hybrid GA+SA algorithm should not be used to generate initial solutions directly.");
        }

        /// <summary>
        /// Optimize an existing scheduling solution using hybrid GA+SA
        /// </summary>
        public async Task<SchedulingSolution> OptimizeSolutionAsync(SchedulingSolution solution, CancellationToken cancellationToken = default)
        {
            if (_problem == null || _parameters == null)
                throw new InvalidOperationException("Must call Initialize before optimizing solution");

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            _logger.LogInformation("Starting hybrid GA+SA optimization");

            // Extract GA parameters
            int populationSize = _parameters.AlgorithmConfig.GeneticAlgorithmPopulationSize;
            double crossoverRate = _parameters.AlgorithmConfig.GeneticAlgorithmCrossoverRate;
            double mutationRate = _parameters.AlgorithmConfig.GeneticAlgorithmMutationRate;
            int maxGenerations = _parameters.AlgorithmConfig.GeneticAlgorithmMaxGenerations;
            int elitismCount = _parameters.AlgorithmConfig.GeneticAlgorithmElitismCount;

            // Initialize population with the given solution
            var population = new List<SchedulingSolution> { solution };

            // Generate initial population by creating variations of the initial solution
            _logger.LogDebug($"Generating initial population of size {populationSize}");

            // First, apply SA to the initial solution to get a good starting point
            var saSolution = await _simulatedAnnealing.OptimizeSolutionAsync(solution, cancellationToken);
            population.Add(saSolution);

            // Fill the rest of the population with variations
            while (population.Count < populationSize)
            {
                var newSolution = GenerateNeighbor(population[_random.Next(population.Count)]);
                population.Add(newSolution);
            }

            // Track best solution
            var bestSolution = solution;
            var bestEvaluation = _evaluator.Evaluate(bestSolution);

            // Update best solution if SA improved it
            var saEvaluation = _evaluator.Evaluate(saSolution);
            if (IsBetterSolution(saEvaluation, bestEvaluation))
            {
                bestSolution = saSolution;
                bestEvaluation = saEvaluation;
            }

            _logger.LogDebug($"Initial best solution score: {bestEvaluation.Score:F4}, Feasible: {bestEvaluation.IsFeasible}");

            // Main GA loop
            for (int generation = 0; generation < maxGenerations && !cancellationToken.IsCancellationRequested; generation++)
            {
                // Evaluate the entire population
                var evaluations = new List<(double Score, bool IsFeasible, SchedulingSolution Solution)>();

                foreach (var individual in population)
                {
                    var evaluation = _evaluator.Evaluate(individual);
                    evaluations.Add((evaluation.Score, evaluation.IsFeasible, individual));
                }

                // Sort by feasibility first, then by score
                evaluations = evaluations
                    .OrderByDescending(e => e.IsFeasible)
                    .ThenByDescending(e => e.Score)
                    .ToList();

                // Check if we have a new best solution
                if (evaluations[0].IsFeasible &&
                   (evaluations[0].Score > bestEvaluation.Score || !bestEvaluation.IsFeasible))
                {
                    bestSolution = evaluations[0].Solution.Clone();
                    bestEvaluation = _evaluator.Evaluate(bestSolution);

                    _logger.LogInformation($"Generation {generation}: New best solution found! Score: {bestEvaluation.Score:F4}");
                }
                else if (generation % 5 == 0)
                {
                    _logger.LogDebug($"Generation {generation}: Best score: {evaluations[0].Score:F4}, Feasible: {evaluations[0].IsFeasible}");
                }

                // Create new population starting with elitism (keep best individuals)
                var newPopulation = new List<SchedulingSolution>();

                // Add elites to the new population
                for (int i = 0; i < Math.Min(elitismCount, evaluations.Count); i++)
                {
                    newPopulation.Add(evaluations[i].Solution.Clone());
                }

                // Track SA usage
                int saUsageCount = 0;

                // Fill the rest of the population with crossover, mutation and SA
                while (newPopulation.Count < populationSize && !cancellationToken.IsCancellationRequested)
                {
                    SchedulingSolution offspring;

                    // Crossover with probability crossoverRate
                    if (_random.NextDouble() < crossoverRate && evaluations.Count >= 2)
                    {
                        // Tournament selection for parents
                        var parent1 = SelectParent(evaluations);
                        var parent2 = SelectParent(evaluations);

                        // Crossover
                        offspring = Crossover(parent1, parent2);
                    }
                    else
                    {
                        // Just clone a parent
                        offspring = SelectParent(evaluations).Clone();
                    }

                    // Mutation with probability mutationRate
                    if (_random.NextDouble() < mutationRate)
                    {
                        Mutate(offspring);
                    }

                    // Apply SA with adaptive probability that increases with generation
                    // This allows more exploration early and more exploitation later
                    double saProb = 0.1 + (0.5 * generation / maxGenerations);

                    if (_random.NextDouble() < saProb && saUsageCount < populationSize / 3)
                    {
                        try
                        {
                            // Create custom SA parameters for a shorter run
                            var saParams = _parameters.Clone();
                            saParams.AlgorithmConfig.SimulatedAnnealingInitialTemperature /= 2;
                            saParams.AlgorithmConfig.SimulatedAnnealingIterationsPerTemperature /= 2;

                            // Reinitialize SA with these parameters
                            _simulatedAnnealing.Initialize(_problem, saParams, _constraintManager, _conflictResolver, _evaluator);

                            var optimized = await _simulatedAnnealing.OptimizeSolutionAsync(offspring, cancellationToken);

                            // Only use the SA result if it's better
                            var offspringEval = _evaluator.Evaluate(offspring);
                            var optimizedEval = _evaluator.Evaluate(optimized);

                            if (IsBetterSolution(optimizedEval, offspringEval))
                            {
                                offspring = optimized;
                                saUsageCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error during simulated annealing, using original offspring");
                        }
                    }

                    newPopulation.Add(offspring);
                }

                // Replace old population
                population = newPopulation;

                // Check for early convergence
                if (generation % 5 == 0 && generation > 0)
                {
                    double populationDiversity = CalculatePopulationDiversity(population);
                    _logger.LogDebug($"Generation {generation}: Population diversity: {populationDiversity:F4}, SA usage: {saUsageCount}");

                    // If diversity is too low, inject some new random solutions
                    if (populationDiversity < 0.1) // Threshold for low diversity
                    {
                        _logger.LogInformation($"Low diversity detected ({populationDiversity:F4}), injecting new solutions");
                        InjectDiversity(population, Math.Max(3, populationSize / 10));
                    }
                }

                // Periodically apply SA to the best solution
                if (generation % 10 == 0 && generation > 0)
                {
                    try
                    {
                        // Restore original SA parameters
                        _simulatedAnnealing.Initialize(_problem, _parameters, _constraintManager, _conflictResolver, _evaluator);

                        var optimizedBest = await _simulatedAnnealing.OptimizeSolutionAsync(bestSolution, cancellationToken);
                        var optimizedEval = _evaluator.Evaluate(optimizedBest);

                        if (IsBetterSolution(optimizedEval, bestEvaluation))
                        {
                            bestSolution = optimizedBest;
                            bestEvaluation = optimizedEval;
                            _logger.LogInformation($"SA improved best solution to score: {bestEvaluation.Score:F4}");

                            // Add the improved solution back to population
                            population[0] = bestSolution.Clone();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error during SA application to best solution");
                    }
                }
            }

            // Final SA optimization on the best solution
            try
            {
                _logger.LogInformation("Applying final SA optimization to best solution");
                var finalSolution = await _simulatedAnnealing.OptimizeSolutionAsync(bestSolution, cancellationToken);
                var finalEvaluation = _evaluator.Evaluate(finalSolution);

                if (IsBetterSolution(finalEvaluation, bestEvaluation))
                {
                    bestSolution = finalSolution;
                    bestEvaluation = finalEvaluation;
                    _logger.LogInformation($"Final SA improved solution to score: {bestEvaluation.Score:F4}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during final SA optimization");
            }

            _logger.LogInformation($"Hybrid GA+SA optimization complete. Best score: {bestEvaluation.Score:F4}, Feasible: {bestEvaluation.IsFeasible}");
            return bestSolution;
        }

        /// <summary>
        /// Select a parent using tournament selection
        /// </summary>
        private SchedulingSolution SelectParent(List<(double Score, bool IsFeasible, SchedulingSolution Solution)> evaluations)
        {
            // Tournament selection - select k random individuals and take the best
            int tournamentSize = Math.Max(2, evaluations.Count / 5);
            var tournament = new List<(double Score, bool IsFeasible, SchedulingSolution Solution)>();

            for (int i = 0; i < tournamentSize; i++)
            {
                int idx = _random.Next(evaluations.Count);
                tournament.Add(evaluations[idx]);
            }

            // Select the best from the tournament
            var winner = tournament
                .OrderByDescending(e => e.IsFeasible)
                .ThenByDescending(e => e.Score)
                .First();

            return winner.Solution;
        }

        /// <summary>
        /// Create a new solution by crossing over two parent solutions
        /// </summary>
        private SchedulingSolution Crossover(SchedulingSolution parent1, SchedulingSolution parent2)
        {
            // Create child as a clone of parent1
            var child = parent1.Clone();

            // We'll use single-point crossover on the assignments
            // Get all section IDs from both parents
            var allSectionIds = parent1.Assignments
                .Select(a => a.SectionId)
                .Union(parent2.Assignments.Select(a => a.SectionId))
                .Distinct()
                .ToList();

            // Shuffle the section IDs for randomness
            ShuffleList(allSectionIds);

            // Determine crossover point (how many sections to take from parent1)
            int crossoverPoint = _random.Next(allSectionIds.Count + 1);

            // Take assignments for first crossoverPoint sections from parent1,
            // and the rest from parent2
            var sectionsFromParent1 = allSectionIds.Take(crossoverPoint).ToHashSet();
            var sectionsFromParent2 = allSectionIds.Skip(crossoverPoint).ToHashSet();

            // Start with an empty assignment list
            child.Assignments.Clear();

            // Add assignments from parent1
            foreach (var assignment in parent1.Assignments
                                      .Where(a => sectionsFromParent1.Contains(a.SectionId)))
            {
                child.Assignments.Add(assignment.Clone());
            }

            // Add assignments from parent2
            foreach (var assignment in parent2.Assignments
                                      .Where(a => sectionsFromParent2.Contains(a.SectionId)))
            {
                child.Assignments.Add(assignment.Clone());
            }

            // Deduplicate assignment IDs
            for (int i = 0; i < child.Assignments.Count; i++)
            {
                child.Assignments[i].Id = i + 1;
            }

            return child;
        }

        /// <summary>
        /// Mutate a solution by making random changes
        /// </summary>
        private void Mutate(SchedulingSolution solution)
        {
            if (solution.Assignments.Count == 0)
                return;

            // Select a random mutation operation
            int operation = _random.Next(3);

            switch (operation)
            {
                case 0: // Change time slot for a random assignment
                    MutateTimeSlot(solution);
                    break;
                case 1: // Change classroom for a random assignment
                    MutateClassroom(solution);
                    break;
                case 2: // Change teacher for a random assignment
                    MutateTeacher(solution);
                    break;
            }
        }

        /// <summary>
        /// Mutate by changing the time slot of a random assignment
        /// </summary>
        private void MutateTimeSlot(SchedulingSolution solution)
        {
            if (_problem.TimeSlots.Count <= 1)
                return;

            // Choose a random assignment
            int idx = _random.Next(solution.Assignments.Count);
            var assignment = solution.Assignments[idx];

            // Choose a new random time slot different from the current one
            int currentTimeSlotIdx = _problem.TimeSlots.FindIndex(ts => ts.Id == assignment.TimeSlotId);
            if (currentTimeSlotIdx < 0)
                return;

            int newTimeSlotIdx;
            do
            {
                newTimeSlotIdx = _random.Next(_problem.TimeSlots.Count);
            } while (newTimeSlotIdx == currentTimeSlotIdx);

            var newTimeSlot = _problem.TimeSlots[newTimeSlotIdx];

            // Create a new assignment with the updated time slot
            var newAssignment = assignment.Clone();
            newAssignment.TimeSlotId = newTimeSlot.Id;
            newAssignment.DayOfWeek = newTimeSlot.DayOfWeek;
            newAssignment.StartTime = newTimeSlot.StartTime;
            newAssignment.EndTime = newTimeSlot.EndTime;

            // Update the solution
            solution.RemoveAssignment(assignment.Id);

            // Try to add the new assignment, if it fails (due to conflicts), keep the original
            if (!solution.AddAssignment(newAssignment))
            {
                solution.AddAssignment(assignment);
            }
        }

        /// <summary>
        /// Mutate by changing the classroom of a random assignment
        /// </summary>
        private void MutateClassroom(SchedulingSolution solution)
        {
            if (_problem.Classrooms.Count <= 1)
                return;

            // Choose a random assignment
            int idx = _random.Next(solution.Assignments.Count);
            var assignment = solution.Assignments[idx];

            // Get the course section for this assignment
            var section = _problem.CourseSections.FirstOrDefault(s => s.Id == assignment.SectionId);
            if (section == null)
                return;

            // Find suitable classrooms for this section
            var suitableClassrooms = _problem.Classrooms
                .Where(c => c.Capacity >= section.Enrollment)
                .ToList();

            if (suitableClassrooms.Count <= 1)
                return;

            // Choose a new random classroom different from the current one
            int currentClassroomIdx = suitableClassrooms.FindIndex(c => c.Id == assignment.ClassroomId);
            if (currentClassroomIdx < 0)
                return;

            int newClassroomIdx;
            do
            {
                newClassroomIdx = _random.Next(suitableClassrooms.Count);
            } while (newClassroomIdx == currentClassroomIdx);

            var newClassroom = suitableClassrooms[newClassroomIdx];

            // Create a new assignment with the updated classroom
            var newAssignment = assignment.Clone();
            newAssignment.ClassroomId = newClassroom.Id;
            newAssignment.ClassroomName = $"{newClassroom.Building}-{newClassroom.Name}";

            // Update the solution
            solution.RemoveAssignment(assignment.Id);

            // Try to add the new assignment, if it fails (due to conflicts), keep the original
            if (!solution.AddAssignment(newAssignment))
            {
                solution.AddAssignment(assignment);
            }
        }

        /// <summary>
        /// Mutate by changing the teacher of a random assignment
        /// </summary>
        private void MutateTeacher(SchedulingSolution solution)
        {
            if (_problem.Teachers.Count <= 1)
                return;

            // Choose a random assignment
            int idx = _random.Next(solution.Assignments.Count);
            var assignment = solution.Assignments[idx];

            // Get the course section for this assignment
            var section = _problem.CourseSections.FirstOrDefault(s => s.Id == assignment.SectionId);
            if (section == null)
                return;

            // Find suitable teachers for this section based on preferences
            var preferredTeachers = _problem.TeacherCoursePreferences
                .Where(p => p.CourseId == section.CourseId)
                .Join(_problem.Teachers,
                    p => p.TeacherId,
                    t => t.Id,
                    (p, t) => new { Teacher = t, Preference = p.PreferenceLevel })
                .OrderByDescending(t => t.Preference)
                .Select(t => t.Teacher)
                .ToList();

            // If no preferred teachers or too few, use any teacher from the same department
            if (preferredTeachers.Count <= 1)
            {
                preferredTeachers = _problem.Teachers
                    .Where(t => t.DepartmentId == section.DepartmentId)
                    .ToList();
            }

            // If still too few, use any teacher
            if (preferredTeachers.Count <= 1)
            {
                preferredTeachers = _problem.Teachers.ToList();

                if (preferredTeachers.Count <= 1)
                    return;
            }

            // Choose a new random teacher different from the current one
            int currentTeacherIdx = preferredTeachers.FindIndex(t => t.Id == assignment.TeacherId);
            if (currentTeacherIdx < 0)
                return;

            int newTeacherIdx;
            do
            {
                newTeacherIdx = _random.Next(preferredTeachers.Count);
            } while (newTeacherIdx == currentTeacherIdx);

            var newTeacher = preferredTeachers[newTeacherIdx];

            // Create a new assignment with the updated teacher
            var newAssignment = assignment.Clone();
            newAssignment.TeacherId = newTeacher.Id;
            newAssignment.TeacherName = newTeacher.Name;

            // Update the solution
            solution.RemoveAssignment(assignment.Id);

            // Try to add the new assignment, if it fails (due to conflicts), keep the original
            if (!solution.AddAssignment(newAssignment))
            {
                solution.AddAssignment(assignment);
            }
        }

        /// <summary>
        /// Generate a neighbor solution by making a small change to the given solution
        /// </summary>
        private SchedulingSolution GenerateNeighbor(SchedulingSolution solution)
        {
            var neighbor = solution.Clone();
            Mutate(neighbor);
            return neighbor;
        }

        /// <summary>
        /// Calculate the diversity of the population
        /// </summary>
        private double CalculatePopulationDiversity(List<SchedulingSolution> population)
        {
            if (population.Count <= 1)
                return 0;

            double totalDifference = 0;
            int totalComparisons = 0;

            // Sample at most 10 solutions to avoid O(n²) complexity
            int sampleSize = Math.Min(10, population.Count);
            var sampledPopulation = population
                .OrderBy(_ => _random.Next())
                .Take(sampleSize)
                .ToList();

            // Compare each pair of solutions
            for (int i = 0; i < sampledPopulation.Count; i++)
            {
                for (int j = i + 1; j < sampledPopulation.Count; j++)
                {
                    double difference = CalculateSolutionDifference(sampledPopulation[i], sampledPopulation[j]);
                    totalDifference += difference;
                    totalComparisons++;
                }
            }

            // Return average difference
            return totalComparisons > 0 ? totalDifference / totalComparisons : 0;
        }

        /// <summary>
        /// Calculate the difference between two solutions (0 = identical, 1 = completely different)
        /// </summary>
        private double CalculateSolutionDifference(SchedulingSolution solution1, SchedulingSolution solution2)
        {
            // Get all section IDs from both solutions
            var allSectionIds = solution1.Assignments
                .Select(a => a.SectionId)
                .Union(solution2.Assignments.Select(a => a.SectionId))
                .Distinct()
                .ToList();

            if (allSectionIds.Count == 0)
                return 0;

            int sameAssignments = 0;

            foreach (int sectionId in allSectionIds)
            {
                var assignment1 = solution1.Assignments.FirstOrDefault(a => a.SectionId == sectionId);
                var assignment2 = solution2.Assignments.FirstOrDefault(a => a.SectionId == sectionId);

                // If both solutions have assigned this section
                if (assignment1 != null && assignment2 != null)
                {
                    // Check if they have the same teacher, classroom and time slot
                    if (assignment1.TeacherId == assignment2.TeacherId &&
                        assignment1.ClassroomId == assignment2.ClassroomId &&
                        assignment1.TimeSlotId == assignment2.TimeSlotId)
                    {
                        sameAssignments++;
                    }
                }
            }

            // Calculate difference percentage
            return 1.0 - ((double)sameAssignments / allSectionIds.Count);
        }

        /// <summary>
        /// Inject diversity into the population by replacing some solutions with new random ones
        /// </summary>
        private void InjectDiversity(List<SchedulingSolution> population, int count)
        {
            count = Math.Min(count, population.Count - 1); // Keep at least 1 original solution

            // Replace the worst solutions
            var evaluations = population
                .Select(s => (Score: _evaluator.Evaluate(s).Score, Solution: s))
                .OrderBy(e => e.Score)
                .ToList();

            for (int i = 0; i < count; i++)
            {
                // Generate a new diverse solution
                var newSolution = GenerateDiverseSolution(population);

                // Replace one of the worst solutions
                int idx = population.IndexOf(evaluations[i].Solution);
                if (idx >= 0)
                {
                    population[idx] = newSolution;
                }
            }
        }

        /// <summary>
        /// Generate a diverse solution that's different from existing population
        /// </summary>
        private SchedulingSolution GenerateDiverseSolution(List<SchedulingSolution> population)
        {
            // Start with a clone of a random solution
            int baseIdx = _random.Next(population.Count);
            var newSolution = population[baseIdx].Clone();

            // Apply multiple mutations to make it significantly different
            int mutations = 5 + _random.Next(10); // Between 5-15 mutations

            for (int i = 0; i < mutations; i++)
            {
                Mutate(newSolution);
            }

            return newSolution;
        }

        /// <summary>
        /// Shuffle a list in-place using Fisher-Yates algorithm
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Determine if one solution is better than another
        /// </summary>
        private bool IsBetterSolution(SchedulingEvaluation evaluation1, SchedulingEvaluation evaluation2)
        {
            // First prioritize feasibility
            if (evaluation1.IsFeasible && !evaluation2.IsFeasible)
                return true;
            if (!evaluation1.IsFeasible && evaluation2.IsFeasible)
                return false;

            // Then compare scores
            return evaluation1.Score > evaluation2.Score;
        }
    }
}