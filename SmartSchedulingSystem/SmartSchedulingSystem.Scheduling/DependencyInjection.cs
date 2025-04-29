using Microsoft.Extensions.DependencyInjection;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Constraints.Level1_CoreHard;
using SmartSchedulingSystem.Scheduling.Constraints.Level2_ConfigurableHard;
using SmartSchedulingSystem.Scheduling.Constraints.Level3_PhysicalSoft;
using SmartSchedulingSystem.Scheduling.Constraints.Level4_QualitySoft;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Algorithms.Hybrid;
using SmartSchedulingSystem.Scheduling.Algorithms.LS;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using SmartSchedulingSystem.Scheduling.Interfaces;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace SmartSchedulingSystem.Scheduling
{
    /// <summary>
    /// Scheduling system dependency injection configuration system dependency injection configuration
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Register scheduling services
        /// </summary>
        public static IServiceCollection AddSchedulingServices(this IServiceCollection services, Utils.SchedulingParameters parameters = null)
        {
            // Use default parameters or provided parameters
            parameters ??= new Utils.SchedulingParameters();

            // Register parameters
            services.AddSingleton(parameters);

            // Register core components
            services.AddSingleton<ConstraintManager>();
            services.AddSingleton<SolutionEvaluator>();
            services.AddSingleton<Algorithms.CP.SolutionConverter>();
            services.AddSingleton<Algorithms.Hybrid.SolutionDiversifier>();
            services.AddSingleton<ProblemAnalyzer>();

            // Register constraint converters
            services.AddTransient<ICPConstraintConverter, TeacherConflictConstraintConverter>();
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
            // Register CP model builder
            services.AddTransient<CPModelBuilder>();

            // Register CP engine
            services.AddTransient<CPScheduler>();

            // Register local search components
            services.AddTransient<MoveGenerator>();
            services.AddTransient<IntelligentMoveSelector>();
            services.AddSingleton<SimulatedAnnealingController>();
            services.AddTransient<LocalSearchOptimizer>();
            services.AddTransient<ConstraintAnalyzer>();
            services.AddTransient<ParameterAdjuster>();

            // Register hybrid engine
            services.AddTransient<CPLSScheduler>();
            services.AddTransient<EngineSelector>();

            // Register main engine
            services.AddTransient<SchedulingEngine>();

            // Register conflict resolver
            services.AddSingleton<ConflictResolver>();
            // Register conflict handler
            services.AddSingleton<IConflictHandler, TeacherConflictHandler>();
            services.AddSingleton<IConflictHandler, ClassroomConflictHandler>();

            // Register hard constraints - modified to Singleton to avoid lifecycle conflicts
            services.AddSingleton<IConstraint, TeacherConflictConstraint>();
            services.AddSingleton<IConstraint, ClassroomConflictConstraint>();
            services.AddSingleton<IConstraint, TeacherAvailabilityConstraint>();
            services.AddSingleton<IConstraint, ClassroomCapacityConstraint>();
            services.AddSingleton<IConstraint, ClassroomAvailabilityConstraint>();

            // Register physical soft constraints - modified to Singleton to avoid lifecycle conflicts
            services.AddSingleton<IConstraint, EquipmentRequirementConstraint>();
            services.AddSingleton<IConstraint, ClassroomTypeMatchConstraint>();
            services.AddSingleton<IConstraint, ResourceComplianceConstraint>();

            // Register quality soft constraints - modified to Singleton to avoid lifecycle conflicts
            services.AddSingleton<IConstraint, TeacherPreferenceConstraint>();
            services.AddSingleton<IConstraint, TeacherWorkloadConstraint>();
            services.AddSingleton<IConstraint, TeacherScheduleCompactnessConstraint>();
            services.AddSingleton<IConstraint, TeacherMobilityConstraint>();

            return services;
        }
    }
}