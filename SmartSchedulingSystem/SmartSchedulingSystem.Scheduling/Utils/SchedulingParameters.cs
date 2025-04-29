using System;
using SmartSchedulingSystem.Scheduling.Engine;

namespace SmartSchedulingSystem.Scheduling.Utils
{
    /// <summary>
    /// 排课系统参数设置
    /// </summary>
    public class SchedulingParameters
    {
        /// <summary>
        /// CP求解器最大时间限制（秒）
        /// </summary>
        public int CpTimeLimit { get; set; } = 60;

        /// <summary>
        /// 初始解数量
        /// </summary>
        public int InitialSolutionCount { get; set; } = 3;

        /// <summary>
        /// 是否启用并行优化
        /// </summary>
        public bool EnableParallelOptimization { get; set; } = true;

        /// <summary>
        /// 是否使用最小约束集
        /// </summary>
        public bool UseBasicConstraints { get; set; } = false;

        /// <summary>
        /// 是否使用标准约束集
        /// </summary>
        public bool UseStandardConstraints { get; set; } = true;

        /// <summary>
        /// 是否使用增强约束集
        /// </summary>
        public bool UseEnhancedConstraints { get; set; } = false;

        /// <summary>
        /// 资源约束应用级别
        /// </summary>
        public ConstraintApplicationLevel ResourceConstraintLevel { get; set; } = ConstraintApplicationLevel.Standard;

        /// <summary>
        /// 最大并行度，默认使用CPU逻辑核心数
        /// </summary>
        public int MaxParallelism { get; set; } = 0;

        /// <summary>
        /// 最大迭代次数
        /// </summary>
        public int MaxIterations { get; set; } = 1000;

        /// <summary>
        /// 局部搜索最大无改进迭代次数
        /// </summary>
        public int MaxNoImprovementIterations { get; set; } = 100;

        /// <summary>
        /// 局部搜索最大迭代次数
        /// </summary>
        public int MaxLsIterations { get; set; } = 1000;

        /// <summary>
        /// 是否启用局部搜索
        /// </summary>
        public bool EnableLocalSearch { get; set; } = true;

        /// <summary>
        /// 模拟退火初始温度
        /// </summary>
        public double InitialTemperature { get; set; } = 1.0;

        /// <summary>
        /// 模拟退火冷却率（每次迭代温度乘以的系数）
        /// </summary>
        public double CoolingRate { get; set; } = 0.995;

        /// <summary>
        /// 最小温度（当温度低于此值时停止）
        /// </summary>
        public double MinTemperature { get; set; } = 0.01;

        /// <summary>
        /// 解的多样性阈值（0-1之间），值越大要求解之间越不同
        /// </summary>
        public double DiversityThreshold { get; set; } = 0.3;

        // 强制规则权重
        public double HardConstraintWeight { get; set; } = 1000.0;

        // 软约束权重
        public double SoftConstraintWeight { get; set; } = 1.0;

        // 物理软约束权重
        public double PhysicalSoftConstraintWeight { get; set; } = 0.8;

        // 质量软约束权重
        public double QualitySoftConstraintWeight { get; set; } = 0.6;

        // 移除对晚上时间段的偏好设置
        // 之前的TimeSlotPreferenceWeight已移除

        // 各约束类型权重，这些不再区分时间段类型
        public double TeacherPreferenceWeight { get; set; } = 1.0;
        public double ClassroomCapacityWeight { get; set; } = 0.8;
        public double BuildingProximityWeight { get; set; } = 0.6;
        public double TimeSlotDistributionWeight { get; set; } = 0.7;
        
        /// <summary>
        /// 复制参数实例
        /// </summary>
        public SchedulingParameters Clone()
        {
            return new SchedulingParameters
            {
                CpTimeLimit = this.CpTimeLimit,
                InitialSolutionCount = this.InitialSolutionCount,
                EnableParallelOptimization = this.EnableParallelOptimization,
                UseBasicConstraints = this.UseBasicConstraints,
                UseStandardConstraints = this.UseStandardConstraints,
                UseEnhancedConstraints = this.UseEnhancedConstraints,
                ResourceConstraintLevel = this.ResourceConstraintLevel,
                MaxParallelism = this.MaxParallelism,
                MaxIterations = this.MaxIterations,
                MaxNoImprovementIterations = this.MaxNoImprovementIterations,
                MaxLsIterations = this.MaxLsIterations,
                EnableLocalSearch = this.EnableLocalSearch,
                InitialTemperature = this.InitialTemperature,
                CoolingRate = this.CoolingRate,
                MinTemperature = this.MinTemperature,
                DiversityThreshold = this.DiversityThreshold,
                HardConstraintWeight = this.HardConstraintWeight,
                SoftConstraintWeight = this.SoftConstraintWeight,
                PhysicalSoftConstraintWeight = this.PhysicalSoftConstraintWeight,
                QualitySoftConstraintWeight = this.QualitySoftConstraintWeight,
                TeacherPreferenceWeight = this.TeacherPreferenceWeight,
                ClassroomCapacityWeight = this.ClassroomCapacityWeight,
                BuildingProximityWeight = this.BuildingProximityWeight,
                TimeSlotDistributionWeight = this.TimeSlotDistributionWeight
            };
        }
    }
} 