using SmartSchedulingSystem.Data.Entities;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Constraints
{
    /// <summary>
    /// 约束接口，所有具体约束实现类必须实现此接口
    /// </summary>
    public interface IConstraint
    {
        /// <summary>
        /// 约束唯一标识符
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// 约束名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 约束描述
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 是否是硬约束（必须满足）
        /// </summary>
        bool IsHard { get; }
        
        /// <summary>
        /// 是否激活此约束
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// 约束权重（0-1之间，仅对软约束有效）
        /// </summary>
        double Weight { get; set; }
        
        /// <summary>
        /// 约束层级（基本排课规则硬约束、可变硬约束、物理限制软约束、质量软约束）
        /// </summary>
        ConstraintHierarchy Hierarchy { get; }
        
        /// <summary>
        /// 约束类别（用于分组和筛选）
        /// </summary>
        string Category { get; }
        
        /// <summary>
        /// 关联的约束定义ID (ConstraintDefinitions中的常量)
        /// </summary>
        string DefinitionId { get; }
        
        /// <summary>
        /// 关联的基本排课规则 (BasicSchedulingRules中的常量)
        /// </summary>
        string BasicRule { get; }
        
        /// <summary>
        /// 评估解决方案对此约束的满足程度
        /// </summary>
        /// <param name="solution">要评估的解决方案</param>
        /// <returns>评分（0-1）和冲突列表</returns>
        (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution);
        
        /// <summary>
        /// 检查解决方案是否满足约束
        /// </summary>
        /// <param name="solution">要检查的解决方案</param>
        /// <returns>是否满足约束</returns>
        bool IsSatisfied(SchedulingSolution solution);
    }
}
