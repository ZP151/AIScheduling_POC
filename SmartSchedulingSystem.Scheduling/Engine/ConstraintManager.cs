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
    /// Constraint application level, controlling the degree of constraint application in the algorithm
    /// </summary>
    public enum ConstraintApplicationLevel
    {
        /// <summary>
        /// Basic level - includes all immutable hard constraints (corresponding to Level1_CoreHard)
        /// </summary>
        Basic = 0,
        
        /// <summary>
        /// Standard level - contains core hard constraints and configurable hard constraints (corresponding to Level1_CoreHard and Level2_ConfigurableHard)
        /// </summary>
        Standard = 1,
        
        /// <summary>
        /// Enhanced level - includes hard constraints and physical soft constraints (corresponding to Level1~3)
        /// </summary>
        Enhanced = 2,
        
        /// <summary>
        /// Complete level - includes all constraints, including quality soft constraints (corresponding to Level1~4)
        /// </summary>
        Complete = 3
    }

    /// <summary>
    /// Constraint manager, responsible for managing all scheduling constraints
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
        /// Constructor
        /// </summary>
        public ConstraintManager(IEnumerable<IConstraint> constraints, ILogger<ConstraintManager> logger)
        {
            _constraints = constraints?.ToList() ?? new List<IConstraint>();
            _logger = logger;
            
            // Initialize dictionaries
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
            
            // Use the Simplified Constraints collection by default
            UseSimplifiedConstraints(true);
        }

        /// <summary>
        /// Set the constraint application level
        /// </summary>
        public void SetConstraintApplicationLevel(ConstraintApplicationLevel level)
        {
            _constraintLevel = level;
            _logger.LogInformation($"Constraint application level set to: {level}");
            
            // Automatically adjust the activation status of constraints based on the new application level
            ApplyConstraintLevel();
        }
        
        /// <summary>
        /// Apply the corresponding constraints based on the current constraint level
        /// </summary>
        private void ApplyConstraintLevel()
        {
            // Disable all constraints first
            foreach (var constraint in _constraints)
            {
                constraint.IsActive = false;
            }
            
            switch (_constraintLevel)
            {
                case ConstraintApplicationLevel.Basic:
                    // Basic level: Enable all Level1_CoreHard constraints
                    foreach (var constraint in _constraints.Where(c => c.Hierarchy == ConstraintHierarchy.Level1_CoreHard))
                    {
                        constraint.IsActive = true;
                    }
                    _logger.LogInformation("Applying base level constraints - Enable all core hard constraints");;
                    break;
                    
                case ConstraintApplicationLevel.Standard:
                    // Standard level: Enable Level1_CoreHard and Level2_ConfigurableHard constraints
                    foreach (var constraint in _constraints.Where(c => 
                        c.Hierarchy == ConstraintHierarchy.Level1_CoreHard || 
                        c.Hierarchy == ConstraintHierarchy.Level2_ConfigurableHard))
                    {
                        constraint.IsActive = true;
                    }
                    _logger.LogInformation("Applying standard level constraints - Enable all core and configurable hard constraints");;
                    break;
                    
                case ConstraintApplicationLevel.Enhanced:
                    // Enhanced level: Enable all Level1~3 constraints
                    foreach (var constraint in _constraints.Where(c => 
                        c.Hierarchy == ConstraintHierarchy.Level1_CoreHard || 
                        c.Hierarchy == ConstraintHierarchy.Level2_ConfigurableHard ||
                        c.Hierarchy == ConstraintHierarchy.Level3_PhysicalSoft))
                    {
                        constraint.IsActive = true;
                    }
                    _logger.LogInformation("Applying enhanced level constraints - Enable all hard constraints and physical soft constraints");
                    break;
                    
                case ConstraintApplicationLevel.Complete:
                    // Complete level: Enable all constraints
                    foreach (var constraint in _constraints)
                    {
                        constraint.IsActive = true;
                    }
                    _logger.LogInformation("Applying complete level constraints - Enable all constraints");
                    break;
            }
            
            _logger.LogInformation($"Current number of enabled constraints: {_constraints.Count(c => c.IsActive)}/{_constraints.Count}");
        }

        /// <summary>
        /// Get the current constraint application level
        /// </summary>
        public ConstraintApplicationLevel GetCurrentApplicationLevel()
        {
            return _constraintLevel;
        }

        /// <summary>
        /// Get all constraints
        /// </summary>
        public List<IConstraint> GetAllConstraints()
        {
            return _constraints.ToList();
        }

        /// <summary>
        /// Get all hard constraints
        /// </summary>
        public List<IConstraint> GetHardConstraints()
        {
            return _constraints.Where(c => c.IsHard).ToList();
        }

        /// <summary>
        /// Get all soft constraints
        /// </summary>
        public List<IConstraint> GetSoftConstraints()
        {
            return _constraints.Where(c => !c.IsHard).ToList();
        }

        /// <summary>
        /// Get constraint by constraint definition ID
        /// </summary>
        public IConstraint GetConstraintById(string id)
        {
            return _constraints.FirstOrDefault(c => c.DefinitionId == id);
        }

        /// <summary>
        /// Add constraint
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
                
                _logger.LogInformation($"Constraint added: {constraint.Name}");
            }
        }

        /// <summary>
        /// Remove constraint
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
                
                _logger.LogInformation($"Constraint removed: {constraint.Name}");
            }
        }

        /// <summary>
        /// Evaluate all constraints
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

            // Only evaluate active constraints
            var activeConstraints = _constraints.Where(c => c.IsActive).ToList();
            
            // Evaluate hard constraints
            var hardConstraintEvaluations = EvaluateHardConstraints(solution);
            evaluation.HardConstraintEvaluations.AddRange(hardConstraintEvaluations);
            
            // Evaluate soft constraints
            var softConstraintEvaluations = EvaluateSoftConstraints(solution);
            evaluation.SoftConstraintEvaluations.AddRange(softConstraintEvaluations);
            
            // Collect all conflicts
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

            // Calculate total score
            evaluation.HardConstraintsSatisfied = hardConstraintEvaluations.All(e => e.Satisfied);
            evaluation.IsFeasible = evaluation.HardConstraintsSatisfied;
            
            evaluation.HardConstraintsSatisfactionLevel = hardConstraintEvaluations.Count > 0 
                ? hardConstraintEvaluations.Average(e => e.Score) 
                : 1.0;

            evaluation.SoftConstraintsSatisfactionLevel = softConstraintEvaluations.Count > 0 
                ? softConstraintEvaluations.Average(e => e.Score) 
                : 1.0;

            // If there are hard constraints not satisfied, the total score is 0
            evaluation.Score = evaluation.IsFeasible ? 
                (evaluation.HardConstraintsSatisfactionLevel * 0.7 + evaluation.SoftConstraintsSatisfactionLevel * 0.3) : 0.0;

            return evaluation;
        }

        /// <summary>
        /// Evaluate hard constraints
        /// </summary>
        public List<ConstraintEvaluation> EvaluateHardConstraints(SchedulingSolution solution)
        {
            var result = new List<ConstraintEvaluation>();
            
            // Only evaluate active hard constraints
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
                    _logger.LogError(ex, $"Error evaluating hard constraint {constraint.Name}");
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
                                Description = $"Error evaluating constraint {constraint.Name}: {ex.Message}",
                                Severity = ConflictSeverity.Critical
                            }
                        }
                    });
                }
            }
            
            return result;
        }

        /// <summary>
        /// Evaluate soft constraints
        /// </summary>
        public List<ConstraintEvaluation> EvaluateSoftConstraints(SchedulingSolution solution)
        {
            var result = new List<ConstraintEvaluation>();
            
            // Only evaluate active soft constraints
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
                    _logger.LogError(ex, $"Error evaluating soft constraint {constraint.Name}");
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
                                Description = $"Error evaluating constraint {constraint.Name}: {ex.Message}",
                                Severity = ConflictSeverity.Moderate
                            }
                        }
                    });
                }
            }
            
            return result;
        }

        /// <summary>
        /// Calculate conflicts
        /// </summary>
        public List<SchedulingConflict> CalculateConflicts(SchedulingSolution solution)
        {
            var conflicts = new List<SchedulingConflict>();
            
            // Evaluate all constraints and collect conflicts
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
                    _logger.LogError(ex, $"Error calculating conflicts for constraint {constraint.Name}");
                    conflicts.Add(new SchedulingConflict
                    {
                        ConstraintId = constraint.Id,
                        Type = SchedulingConflictType.ConstraintEvaluationError,
                        Description = $"Error calculating conflicts for constraint {constraint.Name}: {ex.Message}",
                        Severity = constraint.IsHard ? ConflictSeverity.Critical : ConflictSeverity.Moderate
                    });
                }
            }
            
            return conflicts;
        }

        /// <summary>
        /// Get constraints by hierarchy
        /// </summary>
        public List<IConstraint> GetConstraintsByHierarchy(ConstraintHierarchy hierarchy)
        {
            return _constraints.Where(c => c.Hierarchy == hierarchy).ToList();
        }

        /// <summary>
        /// Get constraints by basic rule
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
        /// Get constraint by ID
        /// </summary>
        public IConstraint FindConstraint(int id)
        {
            return _constraintsById.TryGetValue(id, out var constraint) ? constraint : null;
        }

        /// <summary>
        /// Get constraint by constraint definition ID
        /// </summary>
        public IConstraint FindConstraintByDefinitionId(string definitionId)
        {
            return _constraintsByDefinitionId.TryGetValue(definitionId, out var constraint) ? constraint : null;
        }

        /// <summary>
        /// Enable or disable simplified constraint set
        /// </summary>
        public void UseSimplifiedConstraints(bool useSimplified = true)
        {
            _useSimplifiedConstraints = useSimplified;
            
            if (useSimplified)
            {
                _logger.LogInformation("Enable simplified constraint set, only keep Level1_CoreHard level core hard constraints");
                
                // Set constraint level to Basic
                _constraintLevel = ConstraintApplicationLevel.Basic;
                
                // Apply constraint level
                ApplyConstraintLevel();
            }
            else
            {
                _logger.LogInformation("Restore full constraint set");
                
                // Set constraint level to Complete
                _constraintLevel = ConstraintApplicationLevel.Complete;
                
                // Apply constraint level
                ApplyConstraintLevel();
            }
        }

        /// <summary>
        /// Register constraint
        /// </summary>
        public void RegisterConstraint(IConstraint constraint)
        {
            AddConstraint(constraint);
        }

        /// <summary>
        /// Register multiple constraints
        /// </summary>
        public void RegisterConstraints(IEnumerable<IConstraint> constraints)
        {
            foreach (var constraint in constraints)
            {
                AddConstraint(constraint);
            }
        }

        /// <summary>
        /// Disable constraint
        /// </summary>
        public void DeactivateConstraint(int constraintId)
        {
            var constraint = FindConstraint(constraintId);
            if (constraint != null)
            {
                constraint.IsActive = false;
                _logger.LogInformation($"Constraint {constraint.Name} disabled");
            }
        }

        /// <summary>
        /// Activate constraint
        /// </summary>
        public void ActivateConstraint(int constraintId)
        {
            var constraint = FindConstraint(constraintId);
            if (constraint != null)
            {
                constraint.IsActive = true;
                _logger.LogInformation($"Constraint {constraint.Name} activated");
            }
        }

        /// <summary>
        /// Update constraint weight
        /// </summary>
        public void UpdateConstraintWeight(int constraintId, double weight)
        {
            var constraint = FindConstraint(constraintId);
            if (constraint != null && !constraint.IsHard)
            {
                constraint.Weight = Math.Clamp(weight, 0.0, 1.0);
                _logger.LogInformation($"Constraint {constraint.Name} weight updated to {weight}");
            }
        }

        /// <summary>
        /// Get current active constraints
        /// </summary>
        public List<IConstraint> GetActiveConstraints(ConstraintApplicationLevel level)
        {
            // Return different constraint collections based on the requested level
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
        /// Load constraint configuration
        /// </summary>
        public void LoadConstraintConfiguration(List<string> constraintIds, SchedulingParameters parameters)
        {
            if (constraintIds == null || !constraintIds.Any())
            {
                _logger.LogWarning("No constraint ID list provided, using default configuration");
                return;
            }

            _logger.LogInformation($"Load constraint configuration, {constraintIds.Count} constraints");
            
            // Disable all constraints first
            foreach (var constraint in _constraints)
            {
                constraint.IsActive = false;
            }
            
            // Activate constraints by specified IDs
            foreach (var id in constraintIds)
            {
                var constraint = FindConstraintByDefinitionId(id);
                if (constraint != null)
                {
                    constraint.IsActive = true;
                    _logger.LogInformation($"Constraint {constraint.Name} activated");
                }
                else
                {
                    _logger.LogWarning($"Constraint with ID {id} not found");
                }
            }
            
            // If scheduling parameters are provided, can be used to further configure constraints
            if (parameters != null)
            {
                _logger.LogInformation("Use scheduling parameters to configure constraints");
                
                // Add specific parameter configuration logic as needed
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
                
                // Add specific parameter configuration logic as needed
            }
        }
    }
}