using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System.Collections.Generic;

namespace SmartSchedulingSystem.Scheduling.Interfaces
{
    /// <summary>
    /// Constraint manager interface
    /// </summary>
    public interface IConstraintManager
    {
        #region Constraint management methods

        /// <summary>
        /// Register constraint
        /// </summary>
        /// <param name="constraint">Constraint to register</param>
        void RegisterConstraint(IConstraint constraint);

        /// <summary>
        /// Batch register constraints
        /// </summary>
        /// <param name="constraints">Constraints to register</param>
        void RegisterConstraints(IEnumerable<IConstraint> constraints);

        /// <summary>
        /// Add constraint
        /// </summary>
        /// <param name="constraint">Constraint to add</param>
        void AddConstraint(IConstraint constraint);

        /// <summary>
        /// Remove constraint
        /// </summary>
        /// <param name="id">Constraint ID</param>
        void RemoveConstraint(string id);

        /// <summary>
        /// Deactivate constraint
        /// </summary>
        /// <param name="constraintId">Constraint ID</param>
        void DeactivateConstraint(int constraintId);

        /// <summary>
        /// Activate constraint
        /// </summary>
        /// <param name="constraintId">Constraint ID</param>
        void ActivateConstraint(int constraintId);

        /// <summary>
        /// Update constraint weight
        /// </summary>
        /// <param name="constraintId">Constraint ID</param>
        /// <param name="weight">New weight</param>
        void UpdateConstraintWeight(int constraintId, double weight);

        /// <summary>
        /// Enable or disable simplified constraint set
        /// </summary>
        /// <param name="useSimplified">Whether to use simplified constraint set</param>
        void UseSimplifiedConstraints(bool useSimplified = true);

        /// <summary>
        /// Set constraint application level
        /// </summary>
        /// <param name="level">Constraint application level</param>
        void SetConstraintApplicationLevel(ConstraintApplicationLevel level);

        /// <summary>
        /// Get current constraint application level
        /// </summary>
        /// <returns>Constraint application level</returns>
        ConstraintApplicationLevel GetCurrentApplicationLevel();

        /// <summary>
        /// Load constraint configuration
        /// </summary>
        /// <param name="constraintIds">List of constraint IDs to load</param>
        /// <param name="parameters">Scheduling parameters</param>
        void LoadConstraintConfiguration(List<string> constraintIds, SchedulingParameters parameters);

        #endregion

        #region Constraint query methods

        /// <summary>
        /// Get all constraints
        /// </summary>
        /// <returns>List of constraints</returns>
        List<IConstraint> GetAllConstraints();

        /// <summary>
        /// Get all hard constraints
        /// </summary>
        /// <returns>List of hard constraints</returns>
        List<IConstraint> GetHardConstraints();

        /// <summary>
        /// Get all soft constraints
        /// </summary>
        /// <returns>List of soft constraints</returns>
        List<IConstraint> GetSoftConstraints();

        /// <summary>
        /// Find constraint by numeric ID
        /// </summary>
        /// <param name="id">Constraint ID</param>
        /// <returns>Constraint</returns>
        IConstraint FindConstraint(int id);

        /// <summary>
        /// Get constraint by string ID
        /// </summary>
        /// <param name="id">Constraint ID</param>
        /// <returns>Constraint</returns>
        IConstraint GetConstraintById(string id);

        /// <summary>
        /// Find constraint by definition ID
        /// </summary>
        /// <param name="definitionId">Definition ID</param>
        /// <returns>Constraint</returns>
        IConstraint FindConstraintByDefinitionId(string definitionId);

        /// <summary>
        /// Get constraints by basic rule
        /// </summary>
        /// <param name="basicRule">Basic rule</param>
        /// <returns>List of constraints satisfying the basic rule</returns>
        List<IConstraint> GetConstraintsByBasicRule(string basicRule);

        /// <summary>
        /// Get constraints by hierarchy
        /// </summary>
        /// <param name="hierarchy">Constraint hierarchy</param>
        /// <returns>List of constraints satisfying the hierarchy</returns>
        List<IConstraint> GetConstraintsByHierarchy(ConstraintHierarchy hierarchy);

        /// <summary>
        /// Get active constraints
        /// </summary>
        /// <param name="level">Constraint application level</param>
        /// <returns>List of active constraints</returns>
        List<IConstraint> GetActiveConstraints(ConstraintApplicationLevel level);

        #endregion

        #region Constraint evaluation methods

        /// <summary>
        /// Evaluate all constraints
        /// </summary>
        /// <param name="solution">Scheduling solution</param>
        /// <returns>Evaluation result</returns>
        SchedulingEvaluation EvaluateConstraints(SchedulingSolution solution);

        /// <summary>
        /// Evaluate hard constraints
        /// </summary>
        /// <param name="solution">Scheduling solution</param>
        /// <returns>Evaluation result</returns>
        List<ConstraintEvaluation> EvaluateHardConstraints(SchedulingSolution solution);

        /// <summary>
        /// Evaluate soft constraints
        /// </summary>
        /// <param name="solution">Scheduling solution</param>
        /// <returns>Evaluation result</returns>
        List<ConstraintEvaluation> EvaluateSoftConstraints(SchedulingSolution solution);

        /// <summary>
        /// Calculate conflicts
        /// </summary>
        /// <param name="solution">Scheduling solution</param>
        /// <returns>List of conflicts</returns>
        List<SchedulingConflict> CalculateConflicts(SchedulingSolution solution);

        #endregion
    }
} 