using Microsoft.Extensions.DependencyInjection;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Constraints.Hard;
using SmartSchedulingSystem.Scheduling.Constraints.PhysicalSoft;
using SmartSchedulingSystem.Scheduling.Constraints.QualitySoft;
using SmartSchedulingSystem.Scheduling.Constraints.Soft;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Algorithms.Hybrid;
using SmartSchedulingSystem.Scheduling.Algorithms.LS;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System;
using SchedulSmartSchedulingSystemingSystem.Scheduling.Constraints.Soft;

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

            services.AddSingleton<IClassroomCapacityProvider, TestClassroomCapacityProvider>();
            services.AddSingleton<Dictionary<int, int>>(provider =>
            {
                return new Dictionary<int, int>
                {
                    [1] = 50,
                    [2] = 40,
                    [3] = 60,
                    [4] = 35,
                    [5] = 45,
                    [6] = 55,
                    [7] = 70,
                    [8] = 30,
                    [9] = 65,
                    [10] = 50,
                    [11] = 40,
                    [12] = 60,
                    [13] = 45,
                    [14] = 50,
                    [15] = 55
                };
            });
            services.AddSingleton<Dictionary<int, List<int>>>(provider => new Dictionary<int, List<int>>
            {
                [2] = new List<int> { 1 },
                [3] = new List<int> { 1, 2 },
                [5] = new List<int> { 4 }
            });
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
            services.AddSingleton<ConflictResolver>();
            // 注册冲突处理器
            services.AddSingleton<IConflictHandler, TeacherConflictHandler>();
            services.AddSingleton<IConflictHandler, ClassroomConflictHandler>();

            // 注册硬约束 - 修改为Singleton以避免生命周期冲突
            services.AddSingleton<IConstraint, TeacherConflictConstraint>();
            services.AddSingleton<IConstraint, ClassroomConflictConstraint>();
            services.AddSingleton<IConstraint, TeacherAvailabilityConstraint>();
            services.AddSingleton<IConstraint, PrerequisiteConstraint>();
            services.AddSingleton<IConstraint, ClassroomCapacityConstraint>();
            services.AddSingleton<IConstraint, ClassroomAvailabilityConstraint>();

            // 注册物理软约束 - 修改为Singleton以避免生命周期冲突
            services.AddSingleton<IConstraint, TimeAvailabilityConstraint>();
            services.AddSingleton<IConstraint, EquipmentRequirementConstraint>();
            services.AddSingleton<IConstraint, LocationProximityConstraint>();
            services.AddSingleton<IConstraint, ClassroomTypeMatchConstraint>();

            // 注册质量软约束 - 修改为Singleton以避免生命周期冲突
            services.AddSingleton<IConstraint, TeacherPreferenceConstraint>();
            services.AddSingleton<IConstraint, TeacherWorkloadConstraint>();
            services.AddSingleton<IConstraint, TeacherScheduleCompactnessConstraint>();

            return services;
        }
    }
}