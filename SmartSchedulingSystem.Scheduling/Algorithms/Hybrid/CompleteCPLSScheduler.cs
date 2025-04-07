using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Engine.LS;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace SmartSchedulingSystem.Scheduling.Engine.Hybrid
{
    /// <summary>
    /// Complete implementation of the Constraint Programming + Local Search hybrid scheduler
    /// </summary>
    public class CompleteCPLSScheduler
    {
        private readonly ILogger<CPLSScheduler> _logger;
        private readonly CPScheduler _cpScheduler;
        private readonly LocalSearchOptimizer _localSearchOptimizer;
        private readonly SolutionEvaluator _evaluator;
        private readonly ParameterAdjuster _parameterAdjuster;
        private readonly SolutionDiversifier _solutionDiversifier;
        private readonly ConflictResolver _conflictResolver;
        private readonly SchedulingParameters _parameters;

        public CompleteCPLSScheduler(
            ILogger<CPLSScheduler> logger,
            CPScheduler cpScheduler,
            LocalSearchOptimizer localSearchOptimizer,
            SolutionEvaluator evaluator,
            ParameterAdjuster parameterAdjuster,
            SolutionDiversifier solutionDiversifier,
            ConflictResolver conflictResolver,
            SchedulingParameters parameters = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cpScheduler = cpScheduler ?? throw new ArgumentNullException(nameof(cpScheduler));
            _localSearchOptimizer = localSearchOptimizer ?? throw new ArgumentNullException(nameof(localSearchOptimizer));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _parameterAdjuster = parameterAdjuster ?? throw new ArgumentNullException(nameof(parameterAdjuster));
            _solutionDiversifier = solutionDiversifier ?? throw new ArgumentNullException(nameof(solutionDiversifier));
            _conflictResolver = conflictResolver ?? throw new ArgumentNullException(nameof(conflictResolver));
            _parameters = parameters ?? new SchedulingParameters();
        }

        /// <summary>
        /// Generate a schedule with multiple passes and conflict resolution
        /// </summary>
        public async Task<SchedulingResult> GenerateScheduleAsync(SchedulingProblem problem, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting multi-phase scheduling generation...");
                var sw = Stopwatch.StartNew();

                // 1. Adjust algorithm parameters based on problem characteristics
                AdjustParameters(problem);

                // 2. Check problem feasibility
                if (!await CheckFeasibilityAsync(problem, cancellationToken))
                {
                    _logger.LogWarning("Scheduling problem is infeasible, cannot generate a solution satisfying all hard constraints");
                    return new SchedulingResult
                    {
                        Status = SchedulingStatus.Failure,
                        Message = "Cannot find a schedule satisfying all hard constraints",
                        Solutions = new List<SchedulingSolution>(),
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }

                // 3. Phase 1: Generate initial solutions using CP
                _logger.LogInformation("Phase 1: Generating initial solutions using Constraint Programming...");
                List<SchedulingSolution> initialSolutions = await GenerateInitialSolutionsAsync(problem, cancellationToken);

                if (initialSolutions.Count == 0)
                {
                    _logger.LogWarning("Failed to generate initial solutions");
                    return new SchedulingResult
                    {
                        Status = SchedulingStatus.Failure,
                        Message = "Failed to generate initial solutions",
                        Solutions = new List<SchedulingSolution>(),
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }

                _logger.LogInformation($"Generated {initialSolutions.Count} initial solutions");

                // 4. Phase 2: Optimize solutions using Local Search
                _logger.LogInformation("Phase 2: Optimizing solutions using Local Search...");
                List<SchedulingSolution> optimizedSolutions = await OptimizeSolutionsAsync(initialSolutions, cancellationToken);

                _logger.LogInformation($"Optimized {optimizedSolutions.Count} solutions");

                // 5. Phase 3: Resolve remaining conflicts
                _logger.LogInformation("Phase 3: Resolving remaining conflicts...");
                List<SchedulingSolution> resolvedSolutions = await ResolveConflictsAsync(optimizedSolutions, cancellationToken);

                _logger.LogInformation($"Resolved conflicts in {resolvedSolutions.Count} solutions");

                // 6. Phase 4: Diversify final solution set
                _logger.LogInformation("Phase 4: Diversifying solution set...");
                List<SchedulingSolution> diverseSolutions = DiversifySolutions(resolvedSolutions, _parameters.InitialSolutionCount);

                _logger.LogInformation($"Selected {diverseSolutions.Count} diverse solutions");

                // 7. Evaluate and rank final solutions
                diverseSolutions = diverseSolutions.OrderByDescending(s => _evaluator.Evaluate(s)).ToList();

                sw.Stop();
                var result = new SchedulingResult
                {
                    Status = diverseSolutions.Any() ? SchedulingStatus.Success : SchedulingStatus.PartialSuccess,
                    Message = diverseSolutions.Any()
                        ? "Successfully generated schedule"
                        : "Generated partial schedule with some constraint violations",
                    Solutions = diverseSolutions,
                    ExecutionTimeMs = sw.ElapsedMilliseconds,
                    Statistics = ComputeStatistics(diverseSolutions, problem)
                };

                _logger.LogInformation($"Scheduling completed in {sw.ElapsedMilliseconds}ms, " +
                                     $"status: {result.Status}, solution count: {result.Solutions.Count}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during schedule generation");
                return new SchedulingResult
                {
                    Status = SchedulingStatus.Error,
                    Message = $"Error during schedule generation: {ex.Message}",
                    Solutions = new List<SchedulingSolution>(),
                    ExecutionTimeMs = -1
                };
            }
        }

        /// <summary>
        /// Check problem feasibility using CP
        /// </summary>
        private async Task<bool> CheckFeasibilityAsync(SchedulingProblem problem, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Checking problem feasibility...");

                // Use CP solver to verify if a feasible solution exists
                Google.OrTools.Sat.CpSolverStatus status;
                bool isFeasible = _cpScheduler.CheckFeasibility(problem, out status);

                _logger.LogInformation($"Feasibility check result: {isFeasible}, status: {status}");

                return isFeasible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during feasibility check");
                // Be conservative, assume problem is feasible
                return true;
            }
        }

        /// <summary>
        /// Generate initial solutions using CP
        /// </summary>
        private async Task<List<SchedulingSolution>> GenerateInitialSolutionsAsync(SchedulingProblem problem, CancellationToken cancellationToken)
        {
            try
            {
                return _cpScheduler.GenerateInitialSolutions(problem, _parameters.InitialSolutionCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating initial solutions");
                return new List<SchedulingSolution>();
            }
        }

        /// <summary>
        /// Optimize solutions using LS
        /// </summary>
        private async Task<List<SchedulingSolution>> OptimizeSolutionsAsync(List<SchedulingSolution> initialSolutions, CancellationToken cancellationToken)
        {
            try
            {
                return _localSearchOptimizer.OptimizeSolutions(initialSolutions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing solutions");
                return initialSolutions; // Return initial solutions if optimization fails
            }
        }

        /// <summary>
        /// Resolve remaining conflicts
        /// </summary>
        private async Task<List<SchedulingSolution>> ResolveConflictsAsync(List<SchedulingSolution> solutions, CancellationToken cancellationToken)
        {
            var resolvedSolutions = new List<SchedulingSolution>();

            foreach (var solution in solutions)
            {
                try
                {
                    // Detect conflicts
                    var conflicts = DetectConflicts(solution);

                    if (conflicts.Count == 0)
                    {
                        // No conflicts to resolve
                        resolvedSolutions.Add(solution);
                        continue;
                    }

                    _logger.LogInformation($"Resolving {conflicts.Count} conflicts in solution {solution.Id}");

                    // Resolve conflicts
                    var resolvedSolution = await _conflictResolver.ResolveConflictsAsync(
                        solution,
                        conflicts,
                        ConflictResolutionStrategy.Hybrid,
                        cancellationToken);

                    resolvedSolutions.Add(resolvedSolution);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error resolving conflicts in solution {solution.Id}");
                    resolvedSolutions.Add(solution); // Keep original solution if conflict resolution fails
                }
            }

            return resolvedSolutions;
        }

        /// <summary>
        /// Detect conflicts in a solution
        /// </summary>
        private List<SchedulingConflict> DetectConflicts(SchedulingSolution solution)
        {
            var conflicts = new List<SchedulingConflict>();

            // Evaluate all constraints to detect conflicts
            foreach (var constraint in _evaluator.GetAllActiveConstraints())
            {
                try
                {
                    var (score, constraintConflicts) = constraint.Evaluate(solution);

                    if (constraintConflicts != null && constraintConflicts.Count > 0)
                    {
                        conflicts.AddRange(constraintConflicts);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error evaluating constraint {constraint.Name}");
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Diversify solutions to ensure variety in the final set
        /// </summary>
        private List<SchedulingSolution> DiversifySolutions(List<SchedulingSolution> solutions, int targetCount)
        {
            if (solutions.Count <= targetCount)
            {
                return solutions;
            }

            return _solutionDiversifier.SelectDiverseSet(solutions, targetCount, _evaluator);
        }

        /// <summary>
        /// Adjust algorithm parameters based on problem characteristics
        /// </summary>
        private void AdjustParameters(SchedulingProblem problem)
        {
            try
            {
                _logger.LogInformation("Adjusting algorithm parameters...");
                _parameterAdjuster.AdjustParameters(problem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting parameters");
                // Continue with default parameters
            }
        }

        /// <summary>
        /// Compute statistics for the generated schedule
        /// </summary>
        private SchedulingStatistics ComputeStatistics(List<SchedulingSolution> solutions, SchedulingProblem problem)
        {
            if (solutions == null || !solutions.Any())
                return new SchedulingStatistics();

            try
            {
                _logger.LogDebug("Computing scheduling statistics...");

                // Use the best solution for statistics
                var bestSolution = solutions.First();

                var stats = new SchedulingStatistics
                {
                    TotalSections = problem.CourseSections.Count,
                    ScheduledSections = bestSolution.Assignments.Select(a => a.SectionId).Distinct().Count(),
                    TotalTeachers = problem.Teachers.Count,
                    AssignedTeachers = bestSolution.Assignments.Select(a => a.TeacherId).Distinct().Count(),
                    TotalClassrooms = problem.Classrooms.Count,
                    UsedClassrooms = bestSolution.Assignments.Select(a => a.ClassroomId).Distinct().Count()
                };

                stats.UnscheduledSections = stats.TotalSections - stats.ScheduledSections;

                // Compute detailed statistics (classroom utilization, teacher workloads, time slot utilization)
                ComputeDetailedStatistics(bestSolution, problem, stats);

                _logger.LogDebug("Statistics computation completed");

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing statistics");
                return new SchedulingStatistics();
            }
        }

        /// <summary>
        /// Compute detailed statistics for visualization and analysis
        /// </summary>
        private void ComputeDetailedStatistics(SchedulingSolution solution, SchedulingProblem problem, SchedulingStatistics stats)
        {
            // Compute classroom utilization
            foreach (var classroom in problem.Classrooms)
            {
                var assignments = solution.Assignments.Where(a => a.ClassroomId == classroom.Id).ToList();

                if (assignments.Any())
                {
                    double utilizationRate = CalculateClassroomUtilizationRate(classroom, assignments, problem);

                    stats.ClassroomUtilization[classroom.Id] = new ClassroomUtilizationInfo
                    {
                        ClassroomId = classroom.Id,
                        ClassroomName = classroom.Name,
                        Building = classroom.Building,
                        UtilizationRate = utilizationRate,
                        AssignmentCount = assignments.Count
                    };
                }
            }

            // Compute teacher workloads
            foreach (var teacher in problem.Teachers)
            {
                var assignments = solution.Assignments.Where(a => a.TeacherId == teacher.Id).ToList();

                if (assignments.Any())
                {
                    var workloadInfo = CalculateTeacherWorkload(teacher, assignments, problem);
                    stats.TeacherWorkloads[teacher.Id] = workloadInfo;
                }
            }

            // Compute time slot utilization
            foreach (var timeSlot in problem.TimeSlots)
            {
                var assignments = solution.Assignments.Where(a => a.TimeSlotId == timeSlot.Id).ToList();

                double utilizationRate = CalculateTimeSlotUtilizationRate(timeSlot, assignments, problem);

                stats.TimeSlotUtilization[timeSlot.Id] = new TimeSlotUtilizationInfo
                {
                    TimeSlotId = timeSlot.Id,
                    DayOfWeek = timeSlot.DayOfWeek,
                    StartTime = timeSlot.StartTime,
                    EndTime = timeSlot.EndTime,
                    UtilizationRate = utilizationRate,
                    AssignmentCount = assignments.Count
                };
            }

            // Calculate averages and find peak/lowest utilization
            if (stats.ClassroomUtilization.Count > 0)
            {
                stats.AverageClassroomUtilization = stats.ClassroomUtilization.Values.Average(info => info.UtilizationRate);
            }

            if (stats.TimeSlotUtilization.Count > 0)
            {
                stats.AverageTimeSlotUtilization = stats.TimeSlotUtilization.Values.Average(info => info.UtilizationRate);

                var peakSlot = stats.TimeSlotUtilization.Values.OrderByDescending(info => info.UtilizationRate).First();
                var lowestSlot = stats.TimeSlotUtilization.Values.OrderBy(info => info.UtilizationRate).First();

                stats.PeakTimeSlotId = peakSlot.TimeSlotId;
                stats.PeakTimeSlotUtilization = peakSlot.UtilizationRate;
                stats.LowestTimeSlotId = lowestSlot.TimeSlotId;
                stats.LowestTimeSlotUtilization = lowestSlot.UtilizationRate;
            }

            // Calculate workload balance
            if (stats.TeacherWorkloads.Count > 0)
            {
                var workloads = stats.TeacherWorkloads.Values.Select(info => info.TotalHours).ToList();
                double avgWorkload = workloads.Average();
                double sumSquaredDiff = workloads.Sum(x => Math.Pow(x - avgWorkload, 2));
                stats.TeacherWorkloadStdDev = Math.Sqrt(sumSquaredDiff / workloads.Count);
            }
        }

        /// <summary>
        /// Calculate classroom utilization rate
        /// </summary>
        private double CalculateClassroomUtilizationRate(ClassroomInfo classroom, List<SchedulingAssignment> assignments, SchedulingProblem problem)
        {
            // Assuming 5 days per week, 10 hours per day of availability
            double totalAvailableHours = 5 * 10.0;

            // Calculate used hours (assuming each assignment is 1.5 hours)
            double usedHours = assignments.Count * 1.5;

            return usedHours / totalAvailableHours;
        }

        /// <summary>
        /// Calculate teacher workload information
        /// </summary>
        private TeacherWorkloadInfo CalculateTeacherWorkload(TeacherInfo teacher, List<SchedulingAssignment> assignments, SchedulingProblem problem)
        {
            // Calculate total teaching hours (assuming each assignment is 1.5 hours)
            int totalHours = (int)(assignments.Count * 1.5);

            // Group assignments by day
            var dailyWorkload = assignments
                .GroupBy(a => a.DayOfWeek)
                .ToDictionary(g => g.Key, g => (int)(g.Count() * 1.5));

            // Find maximum daily hours
            int maxDailyHours = dailyWorkload.Count > 0 ? dailyWorkload.Values.Max() : 0;

            // Calculate maximum consecutive hours
            int maxConsecutiveHours = CalculateMaxConsecutiveHours(assignments);

            return new TeacherWorkloadInfo
            {
                TeacherId = teacher.Id,
                TeacherName = teacher.Name,
                TotalHours = totalHours,
                DailyWorkload = dailyWorkload,
                MaxDailyHours = maxDailyHours,
                MaxConsecutiveHours = maxConsecutiveHours,
                AssignmentCount = assignments.Count
            };
        }

        /// <summary>
        /// Calculate maximum consecutive teaching hours
        /// </summary>
        private int CalculateMaxConsecutiveHours(List<SchedulingAssignment> assignments)
        {
            // Group assignments by day
            var assignmentsByDay = assignments.GroupBy(a => a.DayOfWeek).ToDictionary(g => g.Key, g => g.ToList());

            int maxConsecutive = 0;

            foreach (var dayAssignments in assignmentsByDay.Values)
            {
                // Sort assignments by start time
                var sortedAssignments = dayAssignments.OrderBy(a => a.StartTime).ToList();

                int currentConsecutive = 1;

                for (int i = 1; i < sortedAssignments.Count; i++)
                {
                    var prev = sortedAssignments[i - 1];
                    var curr = sortedAssignments[i];

                    // If gap between classes is 30 minutes or less, consider them consecutive
                    if ((curr.StartTime - prev.EndTime).TotalMinutes <= 30)
                    {
                        currentConsecutive++;
                    }
                    else
                    {
                        // Reset counter
                        currentConsecutive = 1;
                    }

                    maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
                }
            }

            return maxConsecutive;
        }

        /// <summary>
        /// Calculate time slot utilization rate
        /// </summary>
        private double CalculateTimeSlotUtilizationRate(TimeSlotInfo timeSlot, List<SchedulingAssignment> assignments, SchedulingProblem problem)
        {
            // Calculate utilization based on classroom usage
            int totalClassrooms = problem.Classrooms.Count;
            return totalClassrooms > 0 ? (double)assignments.Count / totalClassrooms : 0;
        }
    }
}