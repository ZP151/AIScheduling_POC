using Microsoft.Extensions.DependencyInjection;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Constraints.Hard;
using SmartSchedulingSystem.Scheduling.Constraints.PhysicalSoft;
using SmartSchedulingSystem.Scheduling.Constraints.QualitySoft;
using SmartSchedulingSystem.Scheduling.Constraints.Soft;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Engine.Hybrid;
using SmartSchedulingSystem.Scheduling.Engine.LS;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System;

namespace SmartSchedulingSystem.Scheduling
{
    /// <summary>
    /// 排课系统依赖注入配置
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// 注册排课服务
        /// </summary>
        public static IServiceCollection AddSchedulingServices(this IServiceCollection services, SchedulingParameters parameters = null)
        {
            // 使用默认参数或提供的参数
            parameters ??= SchedulingParameters.CreateDefault();

            // 注册参数
            services.AddSingleton(parameters);

            // 注册核心组件
            services.AddSingleton<ConstraintManager>();
            services.AddSingleton<SolutionEvaluator>();
            services.AddSingleton<SolutionConverter>();
            services.AddSingleton<SolutionDiversifier>();
            services.AddSingleton<ProblemAnalyzer>();

            // 注册约束转换器
            services.AddTransient<ICPConstraintConverter, TeacherConflictConstraintConverter>();
            services.AddTransient<ICPConstraintConverter, TeacherAvailabilityConstraintConverter>();
            services.AddTransient<ICPConstraintConverter, ClassroomAvailabilityConstraintConverter>();
            services.AddTransient<ICPConstraintConverter, ClassroomCapacityConstraintConverter>();
            services.AddTransient<ICPConstraintConverter, ClassroomConflictConstraintConverter>();
            services.AddTransient<ICPConstraintConverter, PrerequisiteConstraintConverter>();

            // 注册CP模型构建器
            services.AddTransient<CPModelBuilder>();

            // 注册CP引擎
            services.AddTransient<CPScheduler>();

            // 注册局部搜索组件
            services.AddTransient<MoveGenerator>();
            services.AddTransient<IntelligentMoveSelector>();
            services.AddSingleton<SimulatedAnnealingController>();
            services.AddTransient<LocalSearchOptimizer>();
            services.AddTransient<ConstraintAnalyzer>();
            services.AddTransient<ParameterAdjuster>();

            // 注册混合引擎
            services.AddTransient<CPLSScheduler>();
            services.AddTransient<EngineSelector>();

            // 注册主引擎
            services.AddTransient<SchedulingEngine>();

            // 注册冲突解析器
            services.AddTransient<ConflictResolver>();

            // 注册硬约束
            services.AddTransient<IConstraint, TeacherConflictConstraint>();
            services.AddTransient<IConstraint, ClassroomConflictConstraint>();
            services.AddTransient<IConstraint, TeacherAvailabilityConstraint>();
            services.AddTransient<IConstraint, PrerequisiteConstraint>();
            services.AddTransient<IConstraint, ClassroomCapacityConstraint>();
            services.AddTransient<IConstraint, ClassroomAvailabilityConstraint>();

            // 注册物理软约束
            services.AddTransient<IConstraint, TimeAvailabilityConstraint>();
            services.AddTransient<IConstraint, EquipmentRequirementConstraint>();
            services.AddTransient<IConstraint, LocationProximityConstraint>();

            // 注册质量软约束
            services.AddTransient<IConstraint, TeacherPreferenceConstraint>();
            services.AddTransient<IConstraint, TeacherWorkloadConstraint>();
            services.AddTransient<IConstraint, TeacherScheduleCompactnessConstraint>();

            return services;
        }
    }
}