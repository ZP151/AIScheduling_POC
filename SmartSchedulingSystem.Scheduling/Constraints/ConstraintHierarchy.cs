using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Constraints
{
    /// <summary>
    /// 约束层级枚举
    /// </summary>
    public enum ConstraintHierarchy
    {
        /// <summary>
        /// 第一层：硬约束（必须满足）
        /// </summary>
        Level1_Hard = 1,

        /// <summary>
        /// 第二层：物理限制软约束
        /// </summary>
        Level2_PhysicalSoft = 2,

        /// <summary>
        /// 第三层：质量软约束
        /// </summary>
        Level3_QualitySoft = 3
    }
}