using Microsoft.Extensions.DependencyInjection;
using SmartSchedulingSystem.Scheduling.Algorithms;
using SmartSchedulingSystem.Scheduling.Algorithms.AlgorithmsImple;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Constraints.Hard;
using SmartSchedulingSystem.Scheduling.Constraints.Soft;
using SmartSchedulingSystem.Scheduling.Engine;

namespace SmartSchedulingSystem.Scheduling
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSchedulingServices(this IServiceCollection services)
        {
            services.AddScoped<IConstraintManager, ConstraintManager>();
            services.AddScoped<IConflictResolver, ConflictResolver>();
            services.AddScoped<ISolutionEvaluator, SolutionEvaluator>();
            services.AddScoped<ISchedulingAlgorithmFactory, SchedulingAlgorithmFactory>();
            services.AddScoped<GreedyInitialSolutionGenerator>();
            services.AddScoped<SimulatedAnnealingAlgorithm>();
            services.AddScoped<GeneticAlgorithm>();
            services.AddScoped<SchedulingEngine>();
            return services;
        }
    }
}