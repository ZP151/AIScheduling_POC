using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Algorithms;

namespace SmartSchedulingSystem.Scheduling.Algorithms
{
    public interface ISchedulingAlgorithmFactory
    {
        ISchedulingAlgorithm CreateAlgorithm(SchedulingAlgorithmType algorithmType);
    }
}