using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints
{
    /// <summary>
    /// 约束基类，提供IConstraint接口的通用实现
    /// </summary>
    public abstract class BaseConstraint : IConstraint
    {
        /// <summary>
        /// 约束唯一标识符
        /// </summary>
        public abstract int Id { get; }
        
        /// <summary>
        /// 约束名称
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// 约束描述
        /// </summary>
        public abstract string Description { get; }
        
        /// <summary>
        /// 是否是硬约束（必须满足）
        /// </summary>
        public abstract bool IsHard { get; }
        
        /// <summary>
        /// 约束层级
        /// </summary>
        public abstract ConstraintHierarchy Hierarchy { get; }
        
        /// <summary>
        /// 约束类别
        /// </summary>
        public abstract string Category { get; }
        
        /// <summary>
        /// 关联的约束定义ID
        /// </summary>
        public abstract string DefinitionId { get; }
        
        /// <summary>
        /// 关联的基本排课规则
        /// </summary>
        public abstract string BasicRule { get; }
        
        /// <summary>
        /// 是否激活此约束
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// 约束权重（0-1之间，仅对软约束有效）
        /// </summary>
        public double Weight { get; set; } = 1.0;
        
        /// <summary>
        /// 评估解决方案对此约束的满足程度
        /// </summary>
        public abstract (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution);
        
        /// <summary>
        /// 检查解决方案是否满足约束
        /// </summary>
        public virtual bool IsSatisfied(SchedulingSolution solution)
        {
            var (score, _) = Evaluate(solution);
            return IsHard ? score >= 1.0 : score > 0.0;
        }
        
        /// <summary>
        /// 创建冲突对象
        /// </summary>
        protected SchedulingConflict CreateConflict(
            SchedulingConflictType type, 
            string description, 
            ConflictSeverity severity, 
            Dictionary<string, List<int>> involvedEntities, 
            List<int> involvedTimeSlots = null)
        {
            return new SchedulingConflict
            {
                ConstraintId = Id,
                Type = type,
                Description = description,
                Severity = severity,
                InvolvedEntities = involvedEntities,
                InvolvedTimeSlots = involvedTimeSlots ?? new List<int>()
            };
        }
    }
} 