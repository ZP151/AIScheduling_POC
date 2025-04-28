using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Algorithms.LS;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Google.OrTools.Sat;

namespace SmartSchedulingSystem.Scheduling.Algorithms.Hybrid
{
    /// <summary>
    /// Hybrid scheduling engine combining Constraint Programming (CP) and Local Search (LS)
    /// </summary>
    public class CPLSScheduler
    {
        private readonly ILogger<CPLSScheduler> _logger;
        private readonly CPScheduler _cpScheduler;
        private readonly LocalSearchOptimizer _localSearchOptimizer;
        private readonly SolutionEvaluator _evaluator;
        private readonly ParameterAdjuster _parameterAdjuster;
        private readonly SolutionDiversifier _solutionDiversifier;
        private readonly Utils.SchedulingParameters _parameters;
        private readonly Random _random;

        public CPLSScheduler(
            ILogger<CPLSScheduler> logger,
            CPScheduler cpScheduler,
            LocalSearchOptimizer localSearchOptimizer,
            SolutionEvaluator evaluator,
            ParameterAdjuster parameterAdjuster,
            SolutionDiversifier solutionDiversifier,
            Utils.SchedulingParameters parameters = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cpScheduler = cpScheduler ?? throw new ArgumentNullException(nameof(cpScheduler));
            _localSearchOptimizer = localSearchOptimizer ?? throw new ArgumentNullException(nameof(localSearchOptimizer));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _parameterAdjuster = parameterAdjuster ?? throw new ArgumentNullException(nameof(parameterAdjuster));
            _solutionDiversifier = solutionDiversifier ?? throw new ArgumentNullException(nameof(solutionDiversifier));
            _parameters = parameters ?? new Utils.SchedulingParameters();
            _random = new Random();
        }

        /// <summary>
        /// Generate scheduling solution
        /// </summary>
        public SchedulingResult GenerateSchedule(SchedulingProblem problem)
        {
            try
            {
                _logger.LogInformation("Starting to generate scheduling solution...");
                var sw = Stopwatch.StartNew();

                // 1. Adjust algorithm parameters
                AdjustParameters(problem);

                // 2. Check problem feasibility
                if (!CheckFeasibility(problem))
                {
                    _logger.LogWarning("Scheduling problem is infeasible, cannot generate solution satisfying all hard constraints");

                    return new SchedulingResult
                    {
                        Status = SchedulingStatus.Failure,
                        Message = "Cannot find scheduling solution satisfying all hard constraints",
                        Solutions = new List<SchedulingSolution>(),
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }

                // 3. CP phase: Use progressive constraint application, generate initial solution with minimum level constraints
                _logger.LogInformation("CP phase: Generating initial solution using basic level constraints (Basic)...");
                
                // First set constraint manager to minimum level
                var originalLevel = GlobalConstraintManager.Current?.GetCurrentApplicationLevel() ?? ConstraintApplicationLevel.Basic;
                try
                {
                    // Set constraint manager to basic level
                    GlobalConstraintManager.Current?.SetConstraintApplicationLevel(ConstraintApplicationLevel.Basic);
                    
                    List<SchedulingSolution> initialSolutions = _cpScheduler.GenerateInitialSolutions(
                        problem, _parameters.InitialSolutionCount);

                    if (initialSolutions.Count == 0)
                    {
                        _logger.LogWarning("CP phase failed to generate any initial solutions");
                        return new SchedulingResult
                        {
                            Status = SchedulingStatus.Failure,
                            Message = "Failed to generate initial solutions satisfying basic hard constraints",
                            Solutions = new List<SchedulingSolution>(),
                            ExecutionTimeMs = sw.ElapsedMilliseconds
                        };
                    }

                    _logger.LogInformation($"CP phase completed, generated {initialSolutions.Count} initial solutions using Basic level constraints");

                    // 4. Preliminary evaluation of initial solutions
                    foreach (var solution in initialSolutions)
                    {
                        double score = _evaluator.Evaluate(solution).Score;
                        _logger.LogDebug($"Initial solution score: {score:F4}");
                    }

                    // 5. Use local search to optimize each initial solution, gradually applying higher level constraints
                    _logger.LogInformation("LS phase: Gradually applying higher level constraints to optimize solutions...");
                    
                    List<SchedulingSolution> optimizedSolutions = new List<SchedulingSolution>();
                    
                    // Save current constraint parameters for analysis
                    bool useBasic = _parameters.UseBasicConstraints;
                    bool useStandard = _parameters.UseStandardConstraints;
                    bool useEnhanced = _parameters.UseEnhancedConstraints;
                    
                    // Create optimization phase queue based on parameters
                    var optimizationPhases = new List<(string phaseName, ConstraintApplicationLevel level)>();
                    
                    // Always include Basic level
                    optimizationPhases.Add(("Optimize using Basic level constraints (Level1)", ConstraintApplicationLevel.Basic));
                    
                    // Add Standard level (if not using Basic level only)
                    if (!useBasic || useStandard)
                    {
                        optimizationPhases.Add(("Optimize using Standard level constraints (Level1+Level2)", ConstraintApplicationLevel.Standard));
                    }
                    
                    // Add Enhanced level (if enabled)
                    if (useEnhanced)
                    {
                        optimizationPhases.Add(("Optimize using Enhanced level constraints (Level1+Level2+Level3)", ConstraintApplicationLevel.Enhanced));
                    }
                    
                    _logger.LogInformation($"Optimization will proceed in {optimizationPhases.Count} phases, gradually increasing constraint application level");
                    
                    // Start from initial solutions
                    var currentSolutions = initialSolutions;
                    
                    // Optimize phase by phase, using best solutions from previous phase as input
                    for (int phase = 0; phase < optimizationPhases.Count; phase++)
                    {
                        var (phaseName, level) = optimizationPhases[phase];
                        _logger.LogInformation($"Phase {phase+1}: {phaseName}...");
                        
                        // Set current constraint level
                        GlobalConstraintManager.Current?.SetConstraintApplicationLevel(level);
                        
                        // Optimize solutions for current phase
                        var phaseSolutions = _localSearchOptimizer.OptimizeSolutions(currentSolutions);
                        
                        // If solutions found, use them for next phase optimization
                        if (phaseSolutions.Any())
                        {
                            _logger.LogInformation($"Phase {phase+1} completed, successfully optimized {phaseSolutions.Count} solutions");
                            currentSolutions = phaseSolutions;
                        }
                        else
                        {
                            _logger.LogWarning($"Phase {phase+1} failed to generate valid solutions, keeping solutions from previous phase");
                            // Keep using current solutions
                        }
                    }
                    
                    // Final solutions are from last phase
                    optimizedSolutions = currentSolutions;

                    _logger.LogInformation($"LS phase completed, optimized {optimizedSolutions.Count} solutions");

                    // 6. Evaluate and sort optimized solutions
                    optimizedSolutions = optimizedSolutions
                        .OrderByDescending(s => _evaluator.Evaluate(s))
                        .ToList();

                    // Record final solution scores
                    if (optimizedSolutions.Any())
                    {
                        double bestScore = _evaluator.Evaluate(optimizedSolutions.First()).Score;
                        _logger.LogInformation($"Best solution score: {bestScore:F4}");
                    }

                    // 7. Prepare return result
                    sw.Stop();
                    var result = new SchedulingResult
                    {
                        Status = optimizedSolutions.Any() ? SchedulingStatus.Success : SchedulingStatus.PartialSuccess,
                        Message = optimizedSolutions.Any()
                            ? "Successfully generated scheduling solution"
                            : "Generated partially constraint-satisfying scheduling solution",
                        Solutions = optimizedSolutions,
                        ExecutionTimeMs = sw.ElapsedMilliseconds,
                        Statistics = ComputeStatistics(optimizedSolutions, problem)
                    };

                    _logger.LogInformation($"Scheduling completed, time taken: {sw.ElapsedMilliseconds}ms, " +
                                         $"status: {result.Status}, solution count: {result.Solutions.Count}");

                    return result;
                }
                finally
                {
                    // Restore original constraint manager level
                    if (GlobalConstraintManager.Current != null)
                    {
                        GlobalConstraintManager.Current.SetConstraintApplicationLevel(originalLevel);
                        _logger.LogInformation($"Restored constraint application level to: {originalLevel}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while generating scheduling solution");

                return new SchedulingResult
                {
                    Status = SchedulingStatus.Error,
                    Message = $"Exception occurred during scheduling: {ex.Message}",
                    Solutions = new List<SchedulingSolution>(),
                    ExecutionTimeMs = -1
                };
            }
        }

        /// <summary>
        /// Calculate scheduling statistics
        /// </summary>
        private SchedulingStatistics ComputeStatistics(List<SchedulingSolution> solutions, SchedulingProblem problem)
        {
            if (solutions == null || !solutions.Any())
                return new SchedulingStatistics();

            // Use best solution to calculate statistics
            var bestSolution = solutions.First();

            try
            {
                _logger.LogDebug("Calculating scheduling statistics...");

                var stats = new SchedulingStatistics
                {
                    TotalSections = problem.CourseSections.Count,
                    ScheduledSections = bestSolution.Assignments.Select(a => a.SectionId).Distinct().Count(),
                    TotalTeachers = problem.Teachers.Count,
                    AssignedTeachers = bestSolution.Assignments.Select(a => a.TeacherId).Distinct().Count(),
                    TotalClassrooms = problem.Classrooms.Count,
                    UsedClassrooms = bestSolution.Assignments.Select(a => a.ClassroomId).Distinct().Count()
                };

                // Calculate unscheduled sections
                stats.UnscheduledSections = stats.TotalSections - stats.ScheduledSections;

                // Calculate classroom utilization information
                ComputeClassroomUtilization(bestSolution, problem, stats);

                // Calculate teacher workload information
                ComputeTeacherWorkloads(bestSolution, problem, stats);

                // Calculate time slot utilization information
                ComputeTimeSlotUtilization(bestSolution, problem, stats);

                _logger.LogDebug("Statistics calculation completed");

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating scheduling statistics");
                return new SchedulingStatistics();
            }
        }

        /// <summary>
        /// Calculate classroom utilization information
        /// </summary>
        private void ComputeClassroomUtilization(
            SchedulingSolution solution,
            SchedulingProblem problem,
            SchedulingStatistics stats)
        {
            // Calculate utilization for each classroom
            foreach (var classroom in problem.Classrooms)
            {
                var assignments = solution.Assignments.Where(a => a.ClassroomId == classroom.Id).ToList();

                if (assignments.Any())
                {
                    // Assume 5 days a week, 10 hours available per day
                    double totalAvailableHours = 5 * 10.0;

                    // Calculate used hours (assuming each time slot is 1.5 hours)
                    double usedHours = assignments.Count * 1.5;

                    double utilizationRate = usedHours / totalAvailableHours;

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

            // Calculate average classroom utilization
            if (stats.ClassroomUtilization.Count > 0)
            {
                stats.AverageClassroomUtilization = stats.ClassroomUtilization.Values
                    .Average(info => info.UtilizationRate);
            }
        }

        /// <summary>
        /// Calculate teacher workload information
        /// </summary>
        private void ComputeTeacherWorkloads(
            SchedulingSolution solution,
            SchedulingProblem problem,
            SchedulingStatistics stats)
        {
            // Calculate workload for each teacher
            foreach (var teacher in problem.Teachers)
            {
                var assignments = solution.Assignments.Where(a => a.TeacherId == teacher.Id).ToList();

                if (assignments.Any())
                {
                    // Calculate total hours (assuming each time slot is 1.5 hours)
                    int totalHours = (int)(assignments.Count * 1.5);

                    // Daily workload by day
                    var dailyWorkload = assignments
                        .GroupBy(a => a.DayOfWeek)
                        .ToDictionary(
                            g => g.Key,
                            g => (int)(g.Count() * 1.5));

                    // Calculate maximum daily hours
                    int maxDailyHours = dailyWorkload.Any()
                        ? dailyWorkload.Values.Max()
                        : 0;

                    // Calculate maximum consecutive hours (this requires detailed time slot information)
                    int maxConsecutiveHours = CalculateMaxConsecutiveHours(assignments);

                    stats.TeacherWorkloads[teacher.Id] = new TeacherWorkloadInfo
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
            }

            // Calculate teacher workload standard deviation
            if (stats.TeacherWorkloads.Count > 0)
            {
                var workloads = stats.TeacherWorkloads.Values.Select(info => info.TotalHours).ToList();
                double avg = workloads.Average();
                double sumOfSquares = workloads.Sum(x => Math.Pow(x - avg, 2));
                stats.TeacherWorkloadStdDev = Math.Sqrt(sumOfSquares / workloads.Count);
            }
        }

        /// <summary>
        /// Calculate maximum consecutive hours
        /// </summary>
        private int CalculateMaxConsecutiveHours(List<SchedulingAssignment> assignments)
        {
            // Group by day
            var assignmentsByDay = assignments.GroupBy(a => a.DayOfWeek).ToDictionary(g => g.Key, g => g.ToList());

            int maxConsecutive = 0;

            foreach (var dayAssignments in assignmentsByDay.Values)
            {
                // Sort by start time
                var sortedAssignments = dayAssignments.OrderBy(a => a.StartTime).ToList();

                int currentConsecutive = 1;

                for (int i = 1; i < sortedAssignments.Count; i++)
                {
                    var prev = sortedAssignments[i - 1];
                    var curr = sortedAssignments[i];

                    // If two classes are within 30 minutes of each other, consider them consecutive
                    // Assuming end time and start time format is TimeSpan
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
        /// Calculate time slot utilization information
        /// </summary>
        private void ComputeTimeSlotUtilization(
            SchedulingSolution solution,
            SchedulingProblem problem,
            SchedulingStatistics stats)
        {
            // Calculate utilization for each time slot
            foreach (var timeSlot in problem.TimeSlots)
            {
                var assignments = solution.Assignments.Where(a => a.TimeSlotId == timeSlot.Id).ToList();

                // Assume total classroom count as baseline
                int totalRooms = problem.Classrooms.Count;
                double utilizationRate = totalRooms > 0 ? (double)assignments.Count / totalRooms : 0;

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

            // Calculate average time slot utilization
            if (stats.TimeSlotUtilization.Count > 0)
            {
                stats.AverageTimeSlotUtilization = stats.TimeSlotUtilization.Values
                    .Average(info => info.UtilizationRate);

                // Find peak and valley periods
                var peakSlot = stats.TimeSlotUtilization.Values
                    .OrderByDescending(info => info.UtilizationRate)
                    .First();

                var lowestSlot = stats.TimeSlotUtilization.Values
                    .OrderBy(info => info.UtilizationRate)
                    .First();

                stats.PeakTimeSlotId = peakSlot.TimeSlotId;
                stats.PeakTimeSlotUtilization = peakSlot.UtilizationRate;

                stats.LowestTimeSlotId = lowestSlot.TimeSlotId;
                stats.LowestTimeSlotUtilization = lowestSlot.UtilizationRate;
            }
        }

        /// <summary>
        /// Check problem feasibility
        /// </summary>
        private bool CheckFeasibility(SchedulingProblem problem)
        {
            CpSolverStatus status = CpSolverStatus.Unknown;

            try
            {
                _logger.LogInformation("Checking scheduling problem feasibility...");

                // Increase solving time to improve probability of finding feasible solution
                var tempParams = new Utils.SchedulingParameters
                {
                    CpTimeLimit = 120, // Give more time
                    InitialSolutionCount = 1 // Only need one solution to prove feasibility
                };
                
                // Modify method call to match CPScheduler class's CheckFeasibility method signature
                bool isFeasible = _cpScheduler.CheckFeasibility(null, problem);

                if (isFeasible)
                {
                    _logger.LogInformation("Scheduling problem has feasible solution");
                }
                else
                {
                    switch (status)
                    {
                        case CpSolverStatus.Infeasible:
                            _logger.LogWarning("Scheduling problem has no feasible solution, constraint conflicts");
                            DiagnoseConstraintConflicts(problem);
                            break;
                        case CpSolverStatus.Unknown:
                            _logger.LogWarning("Scheduling problem uncertain whether feasible, solving time insufficient");
                            // Uncertain, try continuing, may still find solution
                            return true;
                        default:
                            _logger.LogWarning($"Check feasibility returned unexpected status: {status}");
                            break;
                    }
                }

                return isFeasible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking problem feasibility");
                // Conservative handling on error, assume problem feasible
                return true;
            }
        }
        // Diagnose constraint conflicts
        private void DiagnoseConstraintConflicts(SchedulingProblem problem)
        {
            _logger.LogInformation("Diagnosing potential constraint conflicts that may make problem infeasible...");

            // Check classroom capacity
            foreach (var section in problem.CourseSections)
            {
                var suitableRooms = problem.Classrooms
                    .Where(r => r.Capacity >= section.Enrollment)
                    .ToList();

                if (suitableRooms.Count == 0)
                {
                    _logger.LogError($"Course {section.CourseCode} (required capacity: {section.Enrollment}) cannot find suitable classroom!");
                }
            }

            // Check teacher qualification
            foreach (var section in problem.CourseSections)
            {
                var qualifiedTeachers = problem.TeacherCoursePreferences
                    .Where(tcp => tcp.CourseId == section.CourseId && tcp.ProficiencyLevel >= 3)
                    .Select(tcp => tcp.TeacherId)
                    .ToList();

                if (qualifiedTeachers.Count == 0)
                {
                    _logger.LogError($"Course {section.CourseCode} cannot find qualified teacher!");
                }
                else
                {
                    // Check if these teachers have enough available time
                    foreach (var teacherId in qualifiedTeachers)
                    {
                        var unavailableTimes = problem.TeacherAvailabilities
                            .Where(ta => ta.TeacherId == teacherId && !ta.IsAvailable)
                            .Select(ta => ta.TimeSlotId)
                            .ToList();

                        if (unavailableTimes.Count >= problem.TimeSlots.Count)
                        {
                            _logger.LogError($"Teacher ID:{teacherId} has no available time slots!");
                        }
                    }
                }
            }

            // Check classroom availability
            foreach (var classroom in problem.Classrooms)
            {
                var unavailableTimeSlots = problem.ClassroomAvailabilities
                    .Where(ca => ca.ClassroomId == classroom.Id && !ca.IsAvailable)
                    .Select(ca => ca.TimeSlotId)
                    .ToList();

                if (unavailableTimeSlots.Count >= problem.TimeSlots.Count)
                {
                    _logger.LogError($"Classroom {classroom.Name} has no available time slots!");
                }
            }

            // Check teacher availability
            foreach (var teacher in problem.Teachers)
            {
                var unavailableTimeSlots = problem.TeacherAvailabilities
                    .Where(ta => ta.TeacherId == teacher.Id && !ta.IsAvailable)
                    .Select(ta => ta.TimeSlotId)
                    .ToList();

                if (unavailableTimeSlots.Count >= problem.TimeSlots.Count)
                {
                    _logger.LogError($"Teacher {teacher.Name} has no available time slots!");
                }
            }
            // Check if resource total is sufficient
            if (problem.TimeSlots.Count < problem.CourseSections.Count)
            {
                _logger.LogError($"Total time slot count ({problem.TimeSlots.Count}) less than course count ({problem.CourseSections.Count}), cannot complete scheduling!");
            }
        }
        /// <summary>
        /// Adjust algorithm parameters
        /// </summary>
        private void AdjustParameters(SchedulingProblem problem)
        {
            try
            {
                _logger.LogDebug("Adjusting algorithm parameters based on problem characteristics...");

                // Adjust parameters based on problem size
                if (problem.CourseSections.Count > 200)
                {
                    _parameters.InitialSolutionCount = 3;
                    _parameters.CpTimeLimit = 300; // Large problem give CP more time
                }
                else if (problem.CourseSections.Count > 100)
                {
                    _parameters.InitialSolutionCount = 5;
                    _parameters.CpTimeLimit = 180;
                }
                else
                {
                    _parameters.InitialSolutionCount = 8;
                    _parameters.CpTimeLimit = 120;
                }

                // Adjust constraint levels based on problem constraint count
                if (problem.Constraints != null)
                {
                    int hardConstraintCount = problem.Constraints.Count(c => c.IsHard);
                    
                    // Decide initial constraint application level based on hard constraint count
                    if (hardConstraintCount > 10)
                    {
                        // Complex problem start from basic constraints
                        GlobalConstraintManager.Current?.SetConstraintApplicationLevel(ConstraintApplicationLevel.Basic);
                        _logger.LogInformation("Many constraints, set initial constraint level to Basic");
                    }
                    else if (hardConstraintCount > 5)
                    {
                        // Medium complexity start from Basic
                        GlobalConstraintManager.Current?.SetConstraintApplicationLevel(ConstraintApplicationLevel.Basic);
                        _logger.LogInformation("Medium constraints, set initial constraint level to Basic");
                    }
                    else
                    {
                        // Simple problem start from Standard
                        GlobalConstraintManager.Current?.SetConstraintApplicationLevel(ConstraintApplicationLevel.Standard);
                        _logger.LogInformation("Few constraints, set initial constraint level to Standard");
                    }
                }
                else
                {
                    // Default start from basic constraints, ensure initial solution can be found
                    GlobalConstraintManager.Current?.SetConstraintApplicationLevel(ConstraintApplicationLevel.Basic);
                    _logger.LogInformation("No constraint information provided, default set initial constraint level to Basic");
                }

                _logger.LogDebug($"Parameter adjustment completed: initial solution count={_parameters.InitialSolutionCount}, CP time limit={_parameters.CpTimeLimit} seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting parameters");
            }
        }

       
    }
}