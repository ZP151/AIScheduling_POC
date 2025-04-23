using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Interfaces;
using SmartSchedulingSystem.Scheduling.Utils;
using Microsoft.Extensions.Logging;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    /// <summary>
    /// 约束应用级别，控制在算法中应用约束的程度
    /// </summary>
    public enum ConstraintApplicationLevel
    {
        /// <summary>
        /// 基本级别 - 包含所有不可变硬约束（对应Level1_CoreHard）
        /// </summary>
        Basic = 0,
        
        /// <summary>
        /// 标准级别 - 包含核心硬约束和可配置硬约束（对应Level1_CoreHard和Level2_ConfigurableHard）
        /// </summary>
        Standard = 1,
        
        /// <summary>
        /// 增强级别 - 包含硬约束和物理软约束（对应Level1~3）
        /// </summary>
        Enhanced = 2,
        
        /// <summary>
        /// 完整级别 - 包含所有约束，包括质量软约束（对应Level1~4）
        /// </summary>
        Complete = 3
    }

    /// <summary>
    /// 约束管理器，负责管理所有排课约束
    /// </summary>
    public class ConstraintManager : IConstraintManager
    {
        private readonly ILogger<ConstraintManager> _logger;
        private readonly List<IConstraint> _constraints = new();
        private ConstraintApplicationLevel _constraintLevel = ConstraintApplicationLevel.Basic;
        private bool _useSimplifiedConstraints = false;
        private readonly Dictionary<int, IConstraint> _constraintsById = new Dictionary<int, IConstraint>();
        private readonly Dictionary<string, IConstraint> _constraintsByDefinitionId = new Dictionary<string, IConstraint>();
        private readonly Dictionary<string, List<IConstraint>> _constraintsByBasicRule = new Dictionary<string, List<IConstraint>>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public ConstraintManager(IEnumerable<IConstraint> constraints, ILogger<ConstraintManager> logger)
        {
            _constraints = constraints?.ToList() ?? new List<IConstraint>();
            _logger = logger;
            
            // 初始化字典
            foreach (var constraint in _constraints)
            {
                if (constraint.Id > 0)
                {
                    _constraintsById[constraint.Id] = constraint;
                }
                
                if (!string.IsNullOrEmpty(constraint.DefinitionId))
                {
                    _constraintsByDefinitionId[constraint.DefinitionId] = constraint;
                }
                
                if (!string.IsNullOrEmpty(constraint.BasicRule))
                {
                    if (!_constraintsByBasicRule.ContainsKey(constraint.BasicRule))
                    {
                        _constraintsByBasicRule[constraint.BasicRule] = new List<IConstraint>();
                    }
                    
                    _constraintsByBasicRule[constraint.BasicRule].Add(constraint);
                }
            }
            
            // 默认使用简化约束集合
            UseSimplifiedConstraints(true);
        }

        /// <summary>
        /// 设置约束应用级别
        /// </summary>
        public void SetConstraintApplicationLevel(ConstraintApplicationLevel level)
        {
            _constraintLevel = level;
            _logger.LogInformation($"约束应用级别设置为: {level}");
            
            // 根据新的应用级别自动调整约束的启用状态
            ApplyConstraintLevel();
        }
        
        /// <summary>
        /// 根据当前约束级别应用对应的约束
        /// </summary>
        private void ApplyConstraintLevel()
        {
            // 先禁用所有约束
            foreach (var constraint in _constraints)
            {
                constraint.IsActive = false;
            }
            
            switch (_constraintLevel)
            {
                case ConstraintApplicationLevel.Basic:
                    // 基本级别：启用所有Level1_CoreHard约束
                    foreach (var constraint in _constraints.Where(c => c.Hierarchy == ConstraintHierarchy.Level1_CoreHard))
                    {
                        constraint.IsActive = true;
                    }
                    _logger.LogInformation("应用基本级别约束 - 启用所有核心硬约束");
                    break;
                    
                case ConstraintApplicationLevel.Standard:
                    // 标准级别：启用Level1_CoreHard和Level2_ConfigurableHard约束
                    foreach (var constraint in _constraints.Where(c => 
                        c.Hierarchy == ConstraintHierarchy.Level1_CoreHard || 
                        c.Hierarchy == ConstraintHierarchy.Level2_ConfigurableHard))
                    {
                        constraint.IsActive = true;
                    }
                    _logger.LogInformation("应用标准级别约束 - 启用所有核心和可配置硬约束");
                    break;
                    
                case ConstraintApplicationLevel.Enhanced:
                    // 增强级别：启用所有Level1~3约束
                    foreach (var constraint in _constraints.Where(c => 
                        c.Hierarchy == ConstraintHierarchy.Level1_CoreHard || 
                        c.Hierarchy == ConstraintHierarchy.Level2_ConfigurableHard ||
                        c.Hierarchy == ConstraintHierarchy.Level3_PhysicalSoft))
                    {
                        constraint.IsActive = true;
                    }
                    _logger.LogInformation("应用增强级别约束 - 启用所有硬约束和物理软约束");
                    break;
                    
                case ConstraintApplicationLevel.Complete:
                    // 完整级别：启用所有约束
                    foreach (var constraint in _constraints)
                    {
                        constraint.IsActive = true;
                    }
                    _logger.LogInformation("应用完整级别约束 - 启用所有约束");
                    break;
            }
            
            _logger.LogInformation($"当前启用的约束数量: {_constraints.Count(c => c.IsActive)}/{_constraints.Count}");
        }

        /// <summary>
        /// 获取当前约束应用级别
        /// </summary>
        public ConstraintApplicationLevel GetCurrentApplicationLevel()
        {
            return _constraintLevel;
        }

        /// <summary>
        /// 获取所有约束
        /// </summary>
        public List<IConstraint> GetAllConstraints()
        {
            return _constraints.ToList();
        }

        /// <summary>
        /// 获取所有硬约束
        /// </summary>
        public List<IConstraint> GetHardConstraints()
        {
            return _constraints.Where(c => c.IsHard).ToList();
        }

        /// <summary>
        /// 获取所有软约束
        /// </summary>
        public List<IConstraint> GetSoftConstraints()
        {
            return _constraints.Where(c => !c.IsHard).ToList();
        }

        /// <summary>
        /// 根据约束定义ID查找约束
        /// </summary>
        public IConstraint GetConstraintById(string id)
        {
            return _constraints.FirstOrDefault(c => c.DefinitionId == id);
        }

        /// <summary>
        /// 添加约束
        /// </summary>
        public void AddConstraint(IConstraint constraint)
        {
            if (constraint == null)
                throw new ArgumentNullException(nameof(constraint));

            if (!_constraints.Any(c => c.DefinitionId == constraint.DefinitionId))
            {
                _constraints.Add(constraint);
                
                if (constraint.Id > 0)
                {
                    _constraintsById[constraint.Id] = constraint;
                }
                
                if (!string.IsNullOrEmpty(constraint.DefinitionId))
                {
                    _constraintsByDefinitionId[constraint.DefinitionId] = constraint;
                }
                
                if (!string.IsNullOrEmpty(constraint.BasicRule))
                {
                    if (!_constraintsByBasicRule.ContainsKey(constraint.BasicRule))
                    {
                        _constraintsByBasicRule[constraint.BasicRule] = new List<IConstraint>();
                    }
                    
                    _constraintsByBasicRule[constraint.BasicRule].Add(constraint);
                }
                
                _logger.LogInformation($"已添加约束: {constraint.Name}");
            }
        }

        /// <summary>
        /// 移除约束
        /// </summary>
        public void RemoveConstraint(string id)
        {
            var constraint = _constraints.FirstOrDefault(c => c.DefinitionId == id);
            if (constraint != null)
            {
                _constraints.Remove(constraint);
                
                if (constraint.Id > 0)
                {
                    _constraintsById.Remove(constraint.Id);
                }
                
                if (!string.IsNullOrEmpty(constraint.DefinitionId))
                {
                    _constraintsByDefinitionId.Remove(constraint.DefinitionId);
                }
                
                if (!string.IsNullOrEmpty(constraint.BasicRule) && 
                    _constraintsByBasicRule.TryGetValue(constraint.BasicRule, out var constraints))
                {
                    constraints.Remove(constraint);
                }
                
                _logger.LogInformation($"已移除约束: {constraint.Name}");
            }
        }

        /// <summary>
        /// 评估所有约束
        /// </summary>
        public SchedulingEvaluation EvaluateConstraints(SchedulingSolution solution)
        {
            var evaluation = new SchedulingEvaluation
            {
                SolutionId = solution.Id,
                HardConstraintEvaluations = new List<ConstraintEvaluation>(),
                SoftConstraintEvaluations = new List<ConstraintEvaluation>(),
                Conflicts = new List<SchedulingConflict>()
            };

            // 只评估激活的约束
            var activeConstraints = _constraints.Where(c => c.IsActive).ToList();
            
            // 评估硬约束
            var hardConstraintEvaluations = EvaluateHardConstraints(solution);
            evaluation.HardConstraintEvaluations.AddRange(hardConstraintEvaluations);
            
            // 评估软约束
            var softConstraintEvaluations = EvaluateSoftConstraints(solution);
            evaluation.SoftConstraintEvaluations.AddRange(softConstraintEvaluations);
            
            // 收集所有冲突
            foreach (var hardEval in hardConstraintEvaluations)
            {
                if (hardEval.Conflicts != null && hardEval.Conflicts.Any())
                {
                    evaluation.Conflicts.AddRange(hardEval.Conflicts);
                }
            }
            
            foreach (var softEval in softConstraintEvaluations)
            {
                if (softEval.Conflicts != null && softEval.Conflicts.Any())
                {
                    evaluation.Conflicts.AddRange(softEval.Conflicts);
                }
            }

            // 计算总得分
            evaluation.HardConstraintsSatisfied = hardConstraintEvaluations.All(e => e.Satisfied);
            evaluation.IsFeasible = evaluation.HardConstraintsSatisfied;
            
            evaluation.HardConstraintsSatisfactionLevel = hardConstraintEvaluations.Count > 0 
                ? hardConstraintEvaluations.Average(e => e.Score) 
                : 1.0;

            evaluation.SoftConstraintsSatisfactionLevel = softConstraintEvaluations.Count > 0 
                ? softConstraintEvaluations.Average(e => e.Score) 
                : 1.0;

            // 如果有硬约束不满足，总分为0
            evaluation.Score = evaluation.IsFeasible ? 
                (evaluation.HardConstraintsSatisfactionLevel * 0.7 + evaluation.SoftConstraintsSatisfactionLevel * 0.3) : 0.0;

            return evaluation;
        }

        /// <summary>
        /// 评估硬约束
        /// </summary>
        public List<ConstraintEvaluation> EvaluateHardConstraints(SchedulingSolution solution)
        {
            var result = new List<ConstraintEvaluation>();
            
            // 只评估激活的硬约束
            var hardConstraints = _constraints.Where(c => c.IsActive && c.IsHard).ToList();
            
            foreach (var constraint in hardConstraints)
            {
                try
                {
                    var (score, conflicts) = constraint.Evaluate(solution);
                    var evaluation = new ConstraintEvaluation
                    {
                        Constraint = constraint,
                        Score = score,
                        Conflicts = conflicts ?? new List<SchedulingConflict>()
                    };
                    result.Add(evaluation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"评估硬约束 {constraint.Name} 时出错");
                    result.Add(new ConstraintEvaluation
                    {
                        Constraint = constraint,
                        Score = 0.0,
                        Conflicts = new List<SchedulingConflict>
                        {
                            new SchedulingConflict
                            {
                                ConstraintId = constraint.Id,
                                Type = SchedulingConflictType.ConstraintEvaluationError,
                                Description = $"评估约束 {constraint.Name} 时发生错误: {ex.Message}",
                                Severity = ConflictSeverity.Critical
                            }
                        }
                    });
                }
            }
            
            return result;
        }

        /// <summary>
        /// 评估软约束
        /// </summary>
        public List<ConstraintEvaluation> EvaluateSoftConstraints(SchedulingSolution solution)
        {
            var result = new List<ConstraintEvaluation>();
            
            // 只评估激活的软约束
            var softConstraints = _constraints.Where(c => c.IsActive && !c.IsHard).ToList();
            
            foreach (var constraint in softConstraints)
            {
                try
                {
                    var (score, conflicts) = constraint.Evaluate(solution);
                    var evaluation = new ConstraintEvaluation
                    {
                        Constraint = constraint,
                        Score = score,
                        Conflicts = conflicts ?? new List<SchedulingConflict>()
                    };
                    result.Add(evaluation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"评估软约束 {constraint.Name} 时出错");
                    result.Add(new ConstraintEvaluation
                    {
                        Constraint = constraint,
                        Score = 0.0,
                        Conflicts = new List<SchedulingConflict>
                        {
                            new SchedulingConflict
                            {
                                ConstraintId = constraint.Id,
                                Type = SchedulingConflictType.ConstraintEvaluationError,
                                Description = $"评估约束 {constraint.Name} 时发生错误: {ex.Message}",
                                Severity = ConflictSeverity.Moderate
                            }
                        }
                    });
                }
            }
            
            return result;
        }

        /// <summary>
        /// 计算冲突
        /// </summary>
        public List<SchedulingConflict> CalculateConflicts(SchedulingSolution solution)
        {
            var conflicts = new List<SchedulingConflict>();
            
            // 评估所有约束并收集冲突
            var activeConstraints = _constraints.Where(c => c.IsActive).ToList();
            
            foreach (var constraint in activeConstraints)
            {
                try
                {
                    var (_, constraintConflicts) = constraint.Evaluate(solution);
                    if (constraintConflicts != null && constraintConflicts.Any())
                    {
                        conflicts.AddRange(constraintConflicts);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"计算约束 {constraint.Name} 的冲突时出错");
                    conflicts.Add(new SchedulingConflict
                    {
                        ConstraintId = constraint.Id,
                        Type = SchedulingConflictType.ConstraintEvaluationError,
                        Description = $"计算约束 {constraint.Name} 的冲突时发生错误: {ex.Message}",
                        Severity = constraint.IsHard ? ConflictSeverity.Critical : ConflictSeverity.Moderate
                    });
                }
            }
            
            return conflicts;
        }

        /// <summary>
        /// 获取指定等级的约束
        /// </summary>
        public List<IConstraint> GetConstraintsByHierarchy(ConstraintHierarchy hierarchy)
        {
            return _constraints.Where(c => c.Hierarchy == hierarchy).ToList();
        }

        /// <summary>
        /// 根据基本规则获取约束
        /// </summary>
        public List<IConstraint> GetConstraintsByBasicRule(string basicRule)
        {
            if (_constraintsByBasicRule.TryGetValue(basicRule, out var constraints))
            {
                return constraints.Where(c => c.IsActive).ToList();
            }
            
            return new List<IConstraint>();
        }

        /// <summary>
        /// 根据ID查找约束
        /// </summary>
        public IConstraint FindConstraint(int id)
        {
            return _constraintsById.TryGetValue(id, out var constraint) ? constraint : null;
        }

        /// <summary>
        /// 根据约束定义ID查找约束
        /// </summary>
        public IConstraint FindConstraintByDefinitionId(string definitionId)
        {
            return _constraintsByDefinitionId.TryGetValue(definitionId, out var constraint) ? constraint : null;
        }

        /// <summary>
        /// 启用或禁用简化约束集合
        /// </summary>
        public void UseSimplifiedConstraints(bool useSimplified = true)
        {
            _useSimplifiedConstraints = useSimplified;
            
            if (useSimplified)
            {
                _logger.LogInformation("启用简化约束集合，只保留Level1_CoreHard级别的核心硬约束");
                
                // 设置约束级别为Basic
                _constraintLevel = ConstraintApplicationLevel.Basic;
                
                // 应用约束级别
                ApplyConstraintLevel();
            }
            else
            {
                _logger.LogInformation("恢复完整约束集合");
                
                // 设置约束级别为Complete
                _constraintLevel = ConstraintApplicationLevel.Complete;
                
                // 应用约束级别
                ApplyConstraintLevel();
            }
        }

        /// <summary>
        /// 注册约束
        /// </summary>
        public void RegisterConstraint(IConstraint constraint)
        {
            AddConstraint(constraint);
        }

        /// <summary>
        /// 注册多个约束
        /// </summary>
        public void RegisterConstraints(IEnumerable<IConstraint> constraints)
        {
            foreach (var constraint in constraints)
            {
                AddConstraint(constraint);
            }
        }

        /// <summary>
        /// 禁用约束
        /// </summary>
        public void DeactivateConstraint(int constraintId)
        {
            var constraint = FindConstraint(constraintId);
            if (constraint != null)
            {
                constraint.IsActive = false;
                _logger.LogInformation($"已禁用约束: {constraint.Name}");
            }
        }

        /// <summary>
        /// 启用约束
        /// </summary>
        public void ActivateConstraint(int constraintId)
        {
            var constraint = FindConstraint(constraintId);
            if (constraint != null)
            {
                constraint.IsActive = true;
                _logger.LogInformation($"已启用约束: {constraint.Name}");
            }
        }

        /// <summary>
        /// 更新约束权重
        /// </summary>
        public void UpdateConstraintWeight(int constraintId, double weight)
        {
            var constraint = FindConstraint(constraintId);
            if (constraint != null && !constraint.IsHard)
            {
                constraint.Weight = Math.Clamp(weight, 0.0, 1.0);
                _logger.LogInformation($"已更新约束 {constraint.Name} 的权重为 {weight}");
            }
        }

        /// <summary>
        /// 获取当前活动的约束
        /// </summary>
        public List<IConstraint> GetActiveConstraints(ConstraintApplicationLevel level)
        {
            // 根据请求的级别返回不同的约束集合
            switch (level)
            {
                case ConstraintApplicationLevel.Basic:
                    return _constraints
                        .Where(c => c.IsActive && c.Hierarchy == ConstraintHierarchy.Level1_CoreHard)
                        .ToList();
                
                case ConstraintApplicationLevel.Standard:
                    return _constraints
                        .Where(c => c.IsActive && 
                               (c.Hierarchy == ConstraintHierarchy.Level1_CoreHard || 
                                c.Hierarchy == ConstraintHierarchy.Level2_ConfigurableHard))
                        .ToList();
                
                case ConstraintApplicationLevel.Enhanced:
                    return _constraints
                        .Where(c => c.IsActive && 
                               (c.Hierarchy == ConstraintHierarchy.Level1_CoreHard || 
                                c.Hierarchy == ConstraintHierarchy.Level2_ConfigurableHard ||
                                c.Hierarchy == ConstraintHierarchy.Level3_PhysicalSoft))
                        .ToList();
                
                case ConstraintApplicationLevel.Complete:
                default:
                    return _constraints.Where(c => c.IsActive).ToList();
            }
        }

        /// <summary>
        /// 加载约束配置
        /// </summary>
        public void LoadConstraintConfiguration(List<string> constraintIds, SchedulingParameters parameters)
        {
            if (constraintIds == null || !constraintIds.Any())
            {
                _logger.LogWarning("没有提供约束ID列表，将使用默认配置");
                return;
            }

            _logger.LogInformation($"加载约束配置，共 {constraintIds.Count} 个约束");
            
            // 先禁用所有约束
            foreach (var constraint in _constraints)
            {
                constraint.IsActive = false;
            }
            
            // 启用指定ID的约束
            foreach (var id in constraintIds)
            {
                var constraint = FindConstraintByDefinitionId(id);
                if (constraint != null)
                {
                    constraint.IsActive = true;
                    _logger.LogInformation($"已启用约束: {constraint.Name}");
                }
                else
                {
                    _logger.LogWarning($"未找到ID为 {id} 的约束");
                }
            }
            
            // 如果提供了排课参数，可以用于进一步配置约束
            if (parameters != null)
            {
                _logger.LogInformation("使用排课参数配置约束");
                
                // 这里根据需要添加特定参数的配置逻辑
                if (parameters.UseBasicConstraints)
                {
                    _constraintLevel = ConstraintApplicationLevel.Basic;
                    ApplyConstraintLevel();
                }
                
                if (parameters.UseStandardConstraints)
                {
                    _constraintLevel = ConstraintApplicationLevel.Standard;
                    ApplyConstraintLevel();
                }
                
                // 根据需要处理其他参数
            }
        }
    }
}