using SmartSchedulingSystem.Data.Entities;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Constraints
{
    public interface IConstraint
    {
        int Id { get; }
        string Name { get; }
        string Description { get; }
        bool IsHard { get; }
        bool IsActive { get; set; }
        double Weight { get; set; }
        ConstraintHierarchy Hierarchy { get; } // 添加约束层级
        string Category { get; } // 添加约束类别
        (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution);
        /// <summary>
        /// 检查解决方案是否满足约束
        /// </summary>
        /// <param name="solution">要检查的解决方案</param>
        /// <returns>是否满足约束</returns>
        bool IsSatisfied(SchedulingSolution solution);
    }

}
