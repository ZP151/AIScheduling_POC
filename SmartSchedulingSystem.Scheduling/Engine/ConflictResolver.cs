using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Engine;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    /// <summary>
    /// Enumeration of conflict resolution strategies
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        /// <summary>
        /// Auto: Let the system automatically select the best solution
        /// </summary>
        Auto,
        
        /// <summary>
        /// ReassignTeacher: Reassign different teachers for courses involved in conflicts
        /// </summary>
        ReassignTeacher,
        
        /// <summary>
        /// ReassignClassroom: Reassign different classrooms for courses involved in conflicts
        /// </summary>
        ReassignClassroom,
        
        /// <summary>
        /// ReassignTime: Reassign different time slots for courses involved in conflicts
        /// </summary>
        ReassignTime,
        
        /// <summary>
        /// IgnoreConflict: Accept conflicts and make no modifications
        /// </summary>
        IgnoreConflict,
        
        /// <summary>
        /// Sequential: Process conflicts in order of priority
        /// </summary>
        Sequential,
        
        /// <summary>
        /// Holistic: Consider the mutual impact of all conflicts
        /// </summary>
        Holistic,
        
        /// <summary>
        /// Hybrid: Combine the advantages of sequential and holistic processing
        /// </summary>
        Hybrid
    }

    public interface IConflictResolver
    {
        Task<SchedulingSolution> ResolveConflictsAsync(
            SchedulingSolution solution,
            IEnumerable<SchedulingConflict> conflicts,
            ConflictResolutionStrategy strategy,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<ConflictResolutionOption>> GetResolutionOptionsAsync(
            SchedulingConflict conflict,
            SchedulingSolution solution,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Conflict resolver - responsible for handling and resolving conflicts in scheduling
    /// </summary>
    public class ConflictResolver : IConflictResolver
    {
        private readonly ISolutionEvaluator _evaluator;
        private readonly Dictionary<SchedulingConflictType, IConflictHandler> _conflictHandlers;

        public ConflictResolver(
            ISolutionEvaluator evaluator,
            IEnumerable<IConflictHandler> conflictHandlers)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

            // Register various conflict handlers
            _conflictHandlers = conflictHandlers?.ToDictionary(h => h.ConflictType)
                              ?? throw new ArgumentNullException(nameof(conflictHandlers));
        }

        public async Task<SchedulingSolution> ResolveConflictsAsync(
            SchedulingSolution solution,
            IEnumerable<SchedulingConflict> conflicts,
            ConflictResolutionStrategy strategy,
            CancellationToken cancellationToken = default)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            if (conflicts == null) throw new ArgumentNullException(nameof(conflicts));

            // Create a copy of the solution
            var resolvedSolution = solution.Clone();

            // Sort conflicts by severity
            var sortedConflicts = conflicts
                .OrderByDescending(c => c.Severity)
                .ToList();

            if (!sortedConflicts.Any())
                return resolvedSolution;

            // Process conflicts based on strategy
            switch (strategy)
            {
                case ConflictResolutionStrategy.Sequential:
                    // Solve conflicts one by one
                    foreach (var conflict in sortedConflicts)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (_conflictHandlers.TryGetValue(conflict.Type, out var handler))
                        {
                            var options = await handler.GetResolutionOptionsAsync(conflict, resolvedSolution, cancellationToken);

                            // Select the best option
                            var bestOption = SelectBestOption(options, resolvedSolution);
                            if (bestOption != null)
                            {
                                // Apply the solution
                                resolvedSolution = await handler.ApplyResolutionAsync(bestOption, resolvedSolution, cancellationToken);
                            }
                        }
                    }
                    break;

                case ConflictResolutionStrategy.Holistic:
                    // Global optimization of conflict solutions
                    // This method considers the mutual impact of conflicts
                    var groupedConflicts = sortedConflicts.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var group in groupedConflicts)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (_conflictHandlers.TryGetValue(group.Key, out var handler))
                        {
                            resolvedSolution = await handler.ResolveBatchAsync(group.Value, resolvedSolution, cancellationToken);
                        }
                    }
                    break;

                case ConflictResolutionStrategy.Hybrid:
                    // First handle critical conflicts, then batch process remaining conflicts
                    var criticalConflicts = sortedConflicts.Where(c => c.Severity == ConflictSeverity.Critical).ToList();
                    var nonCriticalConflicts = sortedConflicts.Where(c => c.Severity != ConflictSeverity.Critical).ToList();

                    // First handle critical conflicts, then batch process remaining conflicts
                    foreach (var conflict in criticalConflicts)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (_conflictHandlers.TryGetValue(conflict.Type, out var handler))
                        {
                            var options = await handler.GetResolutionOptionsAsync(conflict, resolvedSolution, cancellationToken);

                            var bestOption = SelectBestOption(options, resolvedSolution);
                            if (bestOption != null)
                            {
                                resolvedSolution = await handler.ApplyResolutionAsync(bestOption, resolvedSolution, cancellationToken);
                            }
                        }
                    }

                    // Then batch process remaining conflicts
                    var remainingGroups = nonCriticalConflicts.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var group in remainingGroups)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (_conflictHandlers.TryGetValue(group.Key, out var handler))
                        {
                            resolvedSolution = await handler.ResolveBatchAsync(group.Value, resolvedSolution, cancellationToken);
                        }
                    }
                    break;

                default:
                    throw new ArgumentException($"Unsupported conflict resolution strategy: {strategy}");
            }

            return resolvedSolution;
        }
        private async Task<SchedulingSolution> ApplyResolutionOptionsAsync(
            IEnumerable<ConflictResolutionOption> options,
            SchedulingSolution solution,
            CancellationToken cancellationToken)
        {
            var tempSolution = solution.Clone();
            foreach (var option in options.Where(o => o != null))
            {
                option.Apply(tempSolution);
            }
            return tempSolution;
        }
        public async Task<IEnumerable<ConflictResolutionOption>> GetResolutionOptionsAsync(
            SchedulingConflict conflict,
            SchedulingSolution solution,
            CancellationToken cancellationToken = default)
        {
            if (_conflictHandlers.TryGetValue(conflict.Type, out var handler))
            {
                return await handler.GetResolutionOptionsAsync(conflict, solution, cancellationToken);
            }

            return Enumerable.Empty<ConflictResolutionOption>();
        }

        private ConflictResolutionOption SelectBestOption(
            IEnumerable<ConflictResolutionOption> options,
            SchedulingSolution currentSolution)
        {
            if (options == null || !options.Any())
                return null;

            // Evaluate each option, calculate the score of the solution after application
            var scoredOptions = new List<(ConflictResolutionOption Option, double Score)>();

            foreach (var option in options)
            {
                // Apply the option to a copy of the solution
                var tempSolution = currentSolution.Clone();

                // Modify the solution (based on the option description)
                option.Apply(tempSolution);

                // Evaluate the modified solution
                var evaluation = _evaluator.Evaluate(tempSolution);

                // Only consider feasible solutions
                if (evaluation.IsFeasible)
                {
                    scoredOptions.Add((option, evaluation.Score));
                }
            }

            // Return the option with the highest score
            return scoredOptions.OrderByDescending(o => o.Score).FirstOrDefault().Option;
        }
    }

    // Conflict handler interface
    public interface IConflictHandler
    {
        SchedulingConflictType ConflictType { get; }

        Task<IEnumerable<ConflictResolutionOption>> GetResolutionOptionsAsync(
            SchedulingConflict conflict,
            SchedulingSolution solution,
            CancellationToken cancellationToken = default);

        Task<SchedulingSolution> ApplyResolutionAsync(
            ConflictResolutionOption option,
            SchedulingSolution solution,
            CancellationToken cancellationToken = default);

        Task<SchedulingSolution> ResolveBatchAsync(
            IEnumerable<SchedulingConflict> conflicts,
            SchedulingSolution solution,
            CancellationToken cancellationToken = default);
    }
}