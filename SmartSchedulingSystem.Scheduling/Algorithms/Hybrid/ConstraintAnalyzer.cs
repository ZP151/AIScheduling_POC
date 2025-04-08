using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Engine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.Hybrid
{
    /// <summary>
    /// 分析解的约束满足情况，用于指导局部搜索
    /// </summary>
    public class ConstraintAnalyzer
    {
        private readonly ILogger<ConstraintAnalyzer> _logger;
        private readonly ConstraintManager _constraintManager;
        private readonly Random _random = new Random();

        public ConstraintAnalyzer(
            ILogger<ConstraintAnalyzer> logger,
            ConstraintManager constraintManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
        }

        /// <summary>
        /// 分析解的约束满足情况
        /// </summary>
        public ConstraintAnalysisResult AnalyzeSolution(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            try
            {
                _logger.LogDebug("开始分析解的约束满足情况");

                var result = new ConstraintAnalysisResult();

                // 获取所有软约束
                var softConstraints = _constraintManager.GetSoftConstraints();

                // 分析各约束的满足情况
                foreach (var constraint in softConstraints)
                {
                    try
                    {
                        var (score, conflicts) = constraint.Evaluate(solution);

                        // 记录满足度和冲突
                        result.ConstraintSatisfaction[constraint] = score;
                        result.ConstraintConflicts[constraint] = conflicts;

                        _logger.LogDebug($"约束 '{constraint.Name}' 满足度: {score:F4}, 冲突数: {conflicts?.Count ?? 0}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"评估约束 '{constraint.Name}' 时出错");
                        // 出错的约束记为低满足度，优先优化
                        result.ConstraintSatisfaction[constraint] = 0.0;
                        result.ConstraintConflicts[constraint] = new List<SchedulingConflict>();
                    }
                }

                _logger.LogDebug($"约束分析完成，共 {softConstraints.Count} 个软约束");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析约束满足情况时出错");
                throw;
            }
        }
    }

    /// <summary>
    /// 表示约束分析结果
    /// </summary>
    public class ConstraintAnalysisResult
    {
        /// <summary>
        /// 各约束的满足度(0-1)
        /// </summary>
        public Dictionary<IConstraint, double> ConstraintSatisfaction { get; } = new Dictionary<IConstraint, double>();

        /// <summary>
        /// 各约束的冲突
        /// </summary>
        public Dictionary<IConstraint, List<SchedulingConflict>> ConstraintConflicts { get; } =
            new Dictionary<IConstraint, List<SchedulingConflict>>();

        /// <summary>
        /// 获取满足度最低的约束
        /// </summary>
        public IConstraint GetWeakestConstraint()
        {
            if (ConstraintSatisfaction.Count == 0)
            {
                return null;
            }

            // 根据满足度和权重计算优化优先级
            var prioritizedConstraints = ConstraintSatisfaction
                .Select(kv => new
                {
                    Constraint = kv.Key,
                    // 计算加权优先级: (1-满足度) * 权重
                    // 满足度越低，权重越高，优先级越高
                    Priority = (1.0 - kv.Value) * kv.Key.Weight
                })
                .Where(item => item.Priority > 0) // 只考虑未完全满足的约束
                .OrderByDescending(item => item.Priority)
                .ToList();

            if (prioritizedConstraints.Count == 0)
            {
                return null; // 所有约束都完全满足
            }

            // 从前3个优先级最高的约束中随机选择一个
            // 这样可以避免总是优化同一个约束，增加搜索多样性
            int selectCount = Math.Min(3, prioritizedConstraints.Count);

            if (selectCount == 1)
            {
                return prioritizedConstraints[0].Constraint;
            }
            else
            {
                int randomIndex = new Random().Next(selectCount);
                return prioritizedConstraints[randomIndex].Constraint;
            }
        }

        /// <summary>
        /// 获取与指定约束相关的课程分配
        /// </summary>
        public List<SchedulingAssignment> GetAssignmentsAffectedByConstraint(
            SchedulingSolution solution,
            IConstraint constraint)
        {
            if (constraint == null || solution == null || solution.Assignments.Count == 0)
            {
                return new List<SchedulingAssignment>();
            }

            // 如果约束没有冲突，随机选择分配
            if (!ConstraintConflicts.TryGetValue(constraint, out var conflicts) ||
                conflicts == null ||
                conflicts.Count == 0)
            {
                // 如果没有明确的冲突信息，随机选择2-3个分配
                int count = Math.Min(3, solution.Assignments.Count);

                return solution.Assignments
                    .OrderBy(a => Guid.NewGuid())
                    .Take(count)
                    .ToList();
            }

            // 获取所有冲突中涉及的分配ID
            var affectedSectionIds = new HashSet<int>();
            var affectedTeacherIds = new HashSet<int>();
            var affectedClassroomIds = new HashSet<int>();
            var affectedTimeSlotIds = new HashSet<int>();

            foreach (var conflict in conflicts)
            {
                // 收集冲突中涉及的所有实体ID
                if (conflict.InvolvedEntities != null)
                {
                    if (conflict.InvolvedEntities.TryGetValue("Sections", out var sections))
                        foreach (var id in sections)
                            affectedSectionIds.Add(id);

                    if (conflict.InvolvedEntities.TryGetValue("Teachers", out var teachers))
                        foreach (var id in teachers)
                            affectedTeacherIds.Add(id);

                    if (conflict.InvolvedEntities.TryGetValue("Classrooms", out var classrooms))
                        foreach (var id in classrooms)
                            affectedClassroomIds.Add(id);
                }

                // 收集涉及的时间槽
                if (conflict.InvolvedTimeSlots != null)
                    foreach (var id in conflict.InvolvedTimeSlots)
                        affectedTimeSlotIds.Add(id);
            }

            // 找出与这些ID相关的所有分配
            var affectedAssignments = solution.Assignments
                .Where(a =>
                    affectedSectionIds.Contains(a.SectionId) ||
                    affectedTeacherIds.Contains(a.TeacherId) ||
                    affectedClassroomIds.Contains(a.ClassroomId) ||
                    affectedTimeSlotIds.Contains(a.TimeSlotId))
                .ToList();

            // 如果没有找到相关分配，随机选择
            if (affectedAssignments.Count == 0)
            {
                int count = Math.Min(3, solution.Assignments.Count);

                return solution.Assignments
                    .OrderBy(a => Guid.NewGuid())
                    .Take(count)
                    .ToList();
            }

            return affectedAssignments;
        }
    }
}