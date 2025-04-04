using System.Threading;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms
{
    public interface ISchedulingAlgorithm
    {
        void Initialize(SchedulingProblem problem, SchedulingParameters parameters, IConstraintManager constraintManager, IConflictResolver conflictResolver, ISolutionEvaluator evaluator); 
        Task<SchedulingSolution> GenerateInitialSolutionAsync(CancellationToken cancellationToken = default);
        Task<SchedulingSolution> OptimizeSolutionAsync(SchedulingSolution initialSolution, CancellationToken cancellationToken = default);
    }
}