using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using SmartSchedulingSystem.Scheduling.Algorithms.AlgorithmsImple;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms
{
    public class SchedulingAlgorithmFactory : ISchedulingAlgorithmFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SchedulingAlgorithmFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ISchedulingAlgorithm CreateAlgorithm(SchedulingAlgorithmType algorithmType)
        {
            return algorithmType switch
            {
                SchedulingAlgorithmType.Greedy => _serviceProvider.GetRequiredService<GreedyInitialSolutionGenerator>(),
                SchedulingAlgorithmType.SimulatedAnnealing => _serviceProvider.GetRequiredService<SimulatedAnnealingAlgorithm>(),
                SchedulingAlgorithmType.GeneticAlgorithm => _serviceProvider.GetRequiredService<GeneticAlgorithm>(),
                _ => throw new ArgumentException($"Unsupported algorithm type: {algorithmType}")
            };
        }
    }
}