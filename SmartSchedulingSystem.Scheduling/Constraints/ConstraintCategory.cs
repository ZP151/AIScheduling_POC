using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Constraints
{
    /// <summary>
    /// 约束类别常量，用于对约束进行分类
    /// </summary>
    public static class ConstraintCategory
    {
        /// <summary>
        /// 资源分配类别 - 涉及教师、教室等物理资源的分配
        /// </summary>
        public const string ResourceAllocation = "ResourceAllocation";

        /// <summary>
        /// 时间分配类别 - 涉及时间段分配和时间相关约束
        /// </summary>
        public const string TimeAllocation = "TimeAllocation";

        /// <summary>
        /// 教学质量类别 - 涉及教学质量和效果的约束
        /// </summary>
        public const string TeachingQuality = "TeachingQuality";

        /// <summary>
        /// 学生体验类别 - 涉及学生体验和课表质量的约束
        /// </summary>
        public const string StudentExperience = "StudentExperience";

        /// <summary>
        /// 核心规则类别 - 基本的排课规则和限制
        /// </summary>
        public const string CoreRules = "CoreRules";

        /// <summary>
        /// 管理类别 - 涉及管理和行政方面的约束
        /// </summary>
        public const string Administrative = "Administrative";
    }

    /// <summary>
    /// 约束类型枚举
    /// </summary>
    public enum ConstraintType
    {
        /// <summary>
        /// 硬约束 - 必须满足
        /// </summary>
        Hard,

        /// <summary>
        /// 软约束 - 尽量满足
        /// </summary>
        Soft
    }

    /// <summary>
    /// 约束层次结构
    /// </summary>
    public enum ConstraintHierarchy
    {
        /// <summary>
        /// 一级：核心硬约束，基本排课规则，必须满足
        /// </summary>
        Level1_CoreHard = 1,

        /// <summary>
        /// 二级：可配置硬约束，必须满足但可以配置
        /// </summary>
        Level2_ConfigurableHard = 2,

        /// <summary>
        /// 三级：物理软约束，涉及物理资源限制，尽量满足
        /// </summary>
        Level3_PhysicalSoft = 3,

        /// <summary>
        /// 四级：质量软约束，涉及质量和体验，尽量满足
        /// </summary>
        Level4_QualitySoft = 4
    }
} 