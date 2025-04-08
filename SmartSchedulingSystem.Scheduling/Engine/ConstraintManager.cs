using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    public interface IConstraintManager
    {
        void RegisterConstraint(IConstraint constraint);
        void RegisterConstraints(IEnumerable<IConstraint> constraints);
        IReadOnlyList<IConstraint> GetAllConstraints();
        IReadOnlyList<IConstraint> GetHardConstraints();
        IReadOnlyList<IConstraint> GetSoftConstraints();
        void DeactivateConstraint(int constraintId);
        void ActivateConstraint(int constraintId);
        void UpdateConstraintWeight(int constraintId, double weight);
    }

    public class ConstraintManager : IConstraintManager
    {
        private readonly List<IConstraint> _constraints = new List<IConstraint>();
        private readonly Dictionary<int, IConstraint> _constraintsById = new Dictionary<int, IConstraint>();
        public ConstraintManager()
        {
        }
        // 添加新构造函数，自动注册所有约束
        public ConstraintManager(IEnumerable<IConstraint> constraints)
        {
            if (constraints != null)
            {
                RegisterConstraints(constraints);
            }
        }
        public void RegisterConstraint(IConstraint constraint)
        {
            if (constraint == null)
                throw new ArgumentNullException(nameof(constraint));

            if (_constraintsById.ContainsKey(constraint.Id))
            {
                // 如果约束已存在，则更新它
                int index = _constraints.FindIndex(c => c.Id == constraint.Id);
                if (index >= 0)
                {
                    _constraints[index] = constraint;
                    _constraintsById[constraint.Id] = constraint;
                }
            }
            else
            {
                // 添加新约束
                _constraints.Add(constraint);
                _constraintsById[constraint.Id] = constraint;
            }
        }

        public void RegisterConstraints(IEnumerable<IConstraint> constraints)
        {
            foreach (var constraint in constraints)
            {
                RegisterConstraint(constraint);
            }
        }

        public IReadOnlyList<IConstraint> GetAllConstraints()
        {
            return _constraints.AsReadOnly();
        }

        public IReadOnlyList<IConstraint> GetHardConstraints()
        {
            return _constraints.Where(c => c.IsHard && c.IsActive).ToList().AsReadOnly();
        }

        public IReadOnlyList<IConstraint> GetSoftConstraints()
        {
            return _constraints.Where(c => !c.IsHard && c.IsActive).ToList().AsReadOnly();
        }

        public void DeactivateConstraint(int constraintId)
        {
            if (_constraintsById.TryGetValue(constraintId, out var constraint))
            {
                constraint.IsActive = false;
            }
        }

        public void ActivateConstraint(int constraintId)
        {
            if (_constraintsById.TryGetValue(constraintId, out var constraint))
            {
                constraint.IsActive = true;
            }
        }

        public void UpdateConstraintWeight(int constraintId, double weight)
        {
            if (_constraintsById.TryGetValue(constraintId, out var constraint))
            {
                if (!constraint.IsHard) // 只能更新软约束的权重
                {
                    constraint.Weight = Math.Clamp(weight, 0.0, 1.0);
                }
            }
        }

    }
}