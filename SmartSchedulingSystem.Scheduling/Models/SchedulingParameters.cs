using System;
using System.Collections.Generic;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Algorithms;

namespace SmartSchedulingSystem.Scheduling.Models
{
    public class SchedulingParameters
    {
        #region CP引擎参数

        /// <summary>
        /// 初始解数量
        /// </summary>
        public int InitialSolutionCount { get; set; } = 5;

        /// <summary>
        /// CP求解时间限制(秒)
        /// </summary>
        public int CpTimeLimit { get; set; } = 60;

        #endregion

        #region LS引擎参数

        /// <summary>
        /// 局部搜索最大迭代次数
        /// </summary>
        public int MaxLsIterations { get; set; } = 1000;

        /// <summary>
        /// 模拟退火初始温度
        /// </summary>
        public double InitialTemperature { get; set; } = 1.0;

        /// <summary>
        /// 模拟退火冷却率
        /// </summary>
        public double CoolingRate { get; set; } = 0.995;

        /// <summary>
        /// 模拟退火最终温度
        /// </summary>
        public double FinalTemperature { get; set; } = 0.01;

        #endregion

        #region 并行化参数

        /// <summary>
        /// 是否启用并行优化
        /// </summary>
        public bool EnableParallelOptimization { get; set; } = true;

        /// <summary>
        /// 最大并行度(0表示使用所有可用处理器)
        /// </summary>
        public int MaxParallelism { get; set; } = 0;

        #endregion

        #region 约束权重

        /// <summary>
        /// 物理软约束权重
        /// </summary>
        public double PhysicalSoftConstraintWeight { get; set; } = 0.6;

        /// <summary>
        /// 质量软约束权重
        /// </summary>
        public double QualitySoftConstraintWeight { get; set; } = 0.4;

        #endregion

        /// <summary>
        /// 创建参数默认配置
        /// </summary>
        public static SchedulingParameters CreateDefault()
        {
            return new SchedulingParameters
            {
                // 使用所有可用处理器并行化
                MaxParallelism = Environment.ProcessorCount - 1
            };
        }

        /// <summary>
        /// 创建小型问题配置
        /// </summary>
        public static SchedulingParameters CreateSmallProblemConfig()
        {
            return new SchedulingParameters
            {
                InitialSolutionCount = 10,
                CpTimeLimit = 30,
                MaxLsIterations = 500,
                EnableParallelOptimization = false
            };
        }

        /// <summary>
        /// 创建大型问题配置
        /// </summary>
        public static SchedulingParameters CreateLargeProblemConfig()
        {
            return new SchedulingParameters
            {
                InitialSolutionCount = 3,
                CpTimeLimit = 300,
                MaxLsIterations = 5000,
                EnableParallelOptimization = true,
                MaxParallelism = Environment.ProcessorCount - 1
            };
        }
    }

    /// <summary>
    /// 约束和策略设置
    /// </summary>
    public class ConstraintSettings
    {
        public bool EnableGenderSegregation { get; set; } = true;
        public int MinimumTravelTime { get; set; } = 30; // 分钟
        public int MaximumConsecutiveClasses { get; set; } = 3;
        public bool EnableRamadanSchedule { get; set; } = false;
        public bool AllowCrossListedCourses { get; set; } = true;
        public bool EnableMultiCampusConstraints { get; set; } = true;
        public bool HolidayExclusions { get; set; } = true;
        public bool AllowCrossSchoolEnrollment { get; set; } = true;
        public bool AllowCrossDepartmentTeaching { get; set; } = true;
        public bool PrioritizeHomeBuildings { get; set; } = true;

        public ConstraintSettings Clone()
        {
            return new ConstraintSettings
            {
                EnableGenderSegregation = this.EnableGenderSegregation,
                MinimumTravelTime = this.MinimumTravelTime,
                MaximumConsecutiveClasses = this.MaximumConsecutiveClasses,
                EnableRamadanSchedule = this.EnableRamadanSchedule,
                AllowCrossListedCourses = this.AllowCrossListedCourses,
                EnableMultiCampusConstraints = this.EnableMultiCampusConstraints,
                HolidayExclusions = this.HolidayExclusions,
                AllowCrossSchoolEnrollment = this.AllowCrossSchoolEnrollment,
                AllowCrossDepartmentTeaching = this.AllowCrossDepartmentTeaching,
                PrioritizeHomeBuildings = this.PrioritizeHomeBuildings
            };
        }
    }

    /// <summary>
    /// 算法配置
    /// </summary>
    public class AlgorithmSettings
    {
        public double SimulatedAnnealingInitialTemperature { get; set; } = 100.0;
        public double SimulatedAnnealingCoolingRate { get; set; } = 0.97;
        public int SimulatedAnnealingIterationsPerTemperature { get; set; } = 100;
        public double SimulatedAnnealingMinTemperature { get; set; } = 0.01;

        public int GeneticAlgorithmPopulationSize { get; set; } = 50;
        public double GeneticAlgorithmCrossoverRate { get; set; } = 0.8;
        public double GeneticAlgorithmMutationRate { get; set; } = 0.2;
        public int GeneticAlgorithmMaxGenerations { get; set; } = 100;
        public int GeneticAlgorithmElitismCount { get; set; } = 5;

        public int TabuSearchListSize { get; set; } = 20;

        public int MaxIterations { get; set; } = 1000;

        public AlgorithmSettings Clone()
        {
            return new AlgorithmSettings
            {
                SimulatedAnnealingInitialTemperature = this.SimulatedAnnealingInitialTemperature,
                SimulatedAnnealingCoolingRate = this.SimulatedAnnealingCoolingRate,
                SimulatedAnnealingIterationsPerTemperature = this.SimulatedAnnealingIterationsPerTemperature,
                SimulatedAnnealingMinTemperature = this.SimulatedAnnealingMinTemperature,
                GeneticAlgorithmPopulationSize = this.GeneticAlgorithmPopulationSize,
                GeneticAlgorithmCrossoverRate = this.GeneticAlgorithmCrossoverRate,
                GeneticAlgorithmMutationRate = this.GeneticAlgorithmMutationRate,
                GeneticAlgorithmMaxGenerations = this.GeneticAlgorithmMaxGenerations,
                GeneticAlgorithmElitismCount = this.GeneticAlgorithmElitismCount,
                TabuSearchListSize = this.TabuSearchListSize,
                MaxIterations = this.MaxIterations
            };
        }
    }

    // 保持原有枚举
    public enum SchedulingAlgorithmType
    {
        SimulatedAnnealing,
        GeneticAlgorithm,
        TabuSearch,
        VariableNeighborhoodSearch,
        Greedy
    }

    public enum ConflictResolutionStrategy
    {
        Sequential,
        Holistic,
        Hybrid
    }

    public enum AlternativeGenerationStrategy
    {
        RandomPerturbation,
        DiverseOptimization,
        ConstraintRelaxation
    }
}