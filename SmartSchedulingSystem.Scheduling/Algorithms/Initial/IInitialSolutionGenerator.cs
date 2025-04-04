using System.Threading;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms.Initial
{
    public interface IInitialSolutionGenerator
    {
        Task<SchedulingSolution> GenerateAsync(
            SchedulingProblem problem,
            SchedulingParameters parameters,
            CancellationToken cancellationToken = default);
    }
}