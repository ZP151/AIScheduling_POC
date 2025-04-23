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
    /// 冲突解决策略枚举
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        /// <summary>
        /// 自动：让系统自动选择最佳解决方案
        /// </summary>
        Auto,
        
        /// <summary>
        /// 重分配教师：为发生冲突的课程分配不同的教师
        /// </summary>
        ReassignTeacher,
        
        /// <summary>
        /// 重分配教室：为发生冲突的课程分配不同的教室
        /// </summary>
        ReassignClassroom,
        
        /// <summary>
        /// 重分配时间：为发生冲突的课程分配不同的时间段
        /// </summary>
        ReassignTime,
        
        /// <summary>
        /// 忽略冲突：接受冲突，不做任何修改
        /// </summary>
        IgnoreConflict,
        
        /// <summary>
        /// 顺序处理：按照优先级顺序逐个处理冲突
        /// </summary>
        Sequential,
        
        /// <summary>
        /// 整体处理：考虑所有冲突的相互影响进行处理
        /// </summary>
        Holistic,
        
        /// <summary>
        /// 混合处理：结合顺序和整体处理的优点
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
    /// 冲突解决器 - 负责处理和解决排课中的冲突
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

            // 注册各种冲突处理器
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

            // 创建解决方案的副本
            var resolvedSolution = solution.Clone();

            // 按严重程度排序冲突
            var sortedConflicts = conflicts
                .OrderByDescending(c => c.Severity)
                .ToList();

            if (!sortedConflicts.Any())
                return resolvedSolution;

            // 根据策略处理冲突
            switch (strategy)
            {
                case ConflictResolutionStrategy.Sequential:
                    // 逐个解决冲突
                    foreach (var conflict in sortedConflicts)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (_conflictHandlers.TryGetValue(conflict.Type, out var handler))
                        {
                            var options = await handler.GetResolutionOptionsAsync(conflict, resolvedSolution, cancellationToken);

                            // 选择最佳选项
                            var bestOption = SelectBestOption(options, resolvedSolution);
                            if (bestOption != null)
                            {
                                // 应用解决方案
                                resolvedSolution = await handler.ApplyResolutionAsync(bestOption, resolvedSolution, cancellationToken);
                            }
                        }
                    }
                    break;

                case ConflictResolutionStrategy.Holistic:
                    // 全局优化冲突解决方案
                    // 这种方法考虑冲突之间的相互影响
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
                    // 先处理关键冲突，然后批量处理剩余冲突
                    var criticalConflicts = sortedConflicts.Where(c => c.Severity == ConflictSeverity.Critical).ToList();
                    var nonCriticalConflicts = sortedConflicts.Where(c => c.Severity != ConflictSeverity.Critical).ToList();

                    // 先单独处理关键冲突
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

                    // 然后批量处理剩余冲突
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

            // 评估每个选项，计算应用后的解决方案得分
            var scoredOptions = new List<(ConflictResolutionOption Option, double Score)>();

            foreach (var option in options)
            {
                // 应用选项到解决方案的副本
                var tempSolution = currentSolution.Clone();

                // 修改解决方案（根据选项描述）
                option.Apply(tempSolution);

                // 评估修改后的解决方案
                var evaluation = _evaluator.Evaluate(tempSolution);

                // 仅考虑可行的方案
                if (evaluation.IsFeasible)
                {
                    scoredOptions.Add((option, evaluation.Score));
                }
            }

            // 返回得分最高的选项
            return scoredOptions.OrderByDescending(o => o.Score).FirstOrDefault().Option;
        }
    }

    // 冲突处理器接口
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