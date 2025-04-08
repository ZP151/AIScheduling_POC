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
    /// 结合约束规划(CP)和局部搜索(LS)的混合排课引擎
    /// </summary>
    public class CPLSScheduler
    {
        private readonly ILogger<CPLSScheduler> _logger;
        private readonly CPScheduler _cpScheduler;
        private readonly LocalSearchOptimizer _localSearchOptimizer;
        private readonly SolutionEvaluator _evaluator;
        private readonly ParameterAdjuster _parameterAdjuster;
        private readonly SolutionDiversifier _solutionDiversifier;
        private readonly SchedulingParameters _parameters;

        public CPLSScheduler(
            ILogger<CPLSScheduler> logger,
            CPScheduler cpScheduler,
            LocalSearchOptimizer localSearchOptimizer,
            SolutionEvaluator evaluator,
            ParameterAdjuster parameterAdjuster,
            SolutionDiversifier solutionDiversifier,
            SchedulingParameters parameters = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cpScheduler = cpScheduler ?? throw new ArgumentNullException(nameof(cpScheduler));
            _localSearchOptimizer = localSearchOptimizer ?? throw new ArgumentNullException(nameof(localSearchOptimizer));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _parameterAdjuster = parameterAdjuster ?? throw new ArgumentNullException(nameof(parameterAdjuster));
            _solutionDiversifier = solutionDiversifier ?? throw new ArgumentNullException(nameof(solutionDiversifier));
            _parameters = parameters ?? new SchedulingParameters();
        }

        /// <summary>
        /// 生成排课方案
        /// </summary>
        public SchedulingResult GenerateSchedule(SchedulingProblem problem)
        {
            try
            {
                _logger.LogInformation("开始生成排课方案...");
                var sw = Stopwatch.StartNew();

                // 1. 调整算法参数
                AdjustParameters(problem);

                // 2. 检查问题可行性
                if (!CheckFeasibility(problem))
                {
                    _logger.LogWarning("排课问题不可行，无法生成满足所有硬约束的解");

                    return new SchedulingResult
                    {
                        Status = SchedulingStatus.Failure,
                        Message = "无法找到满足所有硬约束的排课方案",
                        Solutions = new List<SchedulingSolution>(),
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }

                // 3. CP阶段：生成初始解
                _logger.LogInformation("CP阶段：生成初始解...");
                List<SchedulingSolution> initialSolutions = _cpScheduler.GenerateInitialSolutions(
                    problem, _parameters.InitialSolutionCount);

                if (initialSolutions.Count == 0)
                {
                    _logger.LogWarning("CP阶段未能生成任何初始解");
                    return new SchedulingResult
                    {
                        Status = SchedulingStatus.Failure,
                        Message = "未能生成满足硬约束的初始解",
                        Solutions = new List<SchedulingSolution>(),
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }

                _logger.LogInformation($"CP阶段完成，生成了 {initialSolutions.Count} 个初始解");

                // 4. 初步评估初始解
                foreach (var solution in initialSolutions)
                {
                    double score = _evaluator.Evaluate(solution).Score;
                    _logger.LogDebug($"初始解评分: {score:F4}");
                }

                // 5. 使用局部搜索优化每个初始解
                _logger.LogInformation("LS阶段：优化解...");

                List<SchedulingSolution> optimizedSolutions = _localSearchOptimizer.OptimizeSolutions(initialSolutions);

                _logger.LogInformation($"LS阶段完成，优化了 {optimizedSolutions.Count} 个解");

                // 6. 评估和排序优化后的解
                optimizedSolutions = optimizedSolutions
                    .OrderByDescending(s => _evaluator.Evaluate(s))
                    .ToList();

                // 记录最终解的评分
                if (optimizedSolutions.Any())
                {
                    double bestScore = _evaluator.Evaluate(optimizedSolutions.First()).Score;
                    _logger.LogInformation($"最优解评分: {bestScore:F4}");
                }

                // 7. 准备返回结果
                sw.Stop();
                var result = new SchedulingResult
                {
                    Status = optimizedSolutions.Any() ? SchedulingStatus.Success : SchedulingStatus.PartialSuccess,
                    Message = optimizedSolutions.Any()
                        ? "成功生成排课方案"
                        : "生成了部分满足约束的排课方案",
                    Solutions = optimizedSolutions,
                    ExecutionTimeMs = sw.ElapsedMilliseconds,
                    Statistics = ComputeStatistics(optimizedSolutions, problem)
                };

                _logger.LogInformation($"排课完成，耗时: {sw.ElapsedMilliseconds}ms，" +
                                     $"状态: {result.Status}，解数量: {result.Solutions.Count}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成排课方案时发生异常");

                return new SchedulingResult
                {
                    Status = SchedulingStatus.Error,
                    Message = $"排课过程中发生异常: {ex.Message}",
                    Solutions = new List<SchedulingSolution>(),
                    ExecutionTimeMs = -1
                };
            }
        }

        /// <summary>
        /// 计算排课统计信息
        /// </summary>
        private SchedulingStatistics ComputeStatistics(List<SchedulingSolution> solutions, SchedulingProblem problem)
        {
            if (solutions == null || !solutions.Any())
                return new SchedulingStatistics();

            // 使用最优解计算统计信息
            var bestSolution = solutions.First();

            try
            {
                _logger.LogDebug("计算排课统计信息...");

                var stats = new SchedulingStatistics
                {
                    TotalSections = problem.CourseSections.Count,
                    ScheduledSections = bestSolution.Assignments.Select(a => a.SectionId).Distinct().Count(),
                    TotalTeachers = problem.Teachers.Count,
                    AssignedTeachers = bestSolution.Assignments.Select(a => a.TeacherId).Distinct().Count(),
                    TotalClassrooms = problem.Classrooms.Count,
                    UsedClassrooms = bestSolution.Assignments.Select(a => a.ClassroomId).Distinct().Count()
                };

                // 计算未排课的班级数
                stats.UnscheduledSections = stats.TotalSections - stats.ScheduledSections;

                // 计算教室利用率信息
                ComputeClassroomUtilization(bestSolution, problem, stats);

                // 计算教师工作量信息
                ComputeTeacherWorkloads(bestSolution, problem, stats);

                // 计算时间槽利用率信息
                ComputeTimeSlotUtilization(bestSolution, problem, stats);

                _logger.LogDebug("统计信息计算完成");

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算统计信息时出错");
                return new SchedulingStatistics();
            }
        }

        /// <summary>
        /// 计算教室利用率信息
        /// </summary>
        private void ComputeClassroomUtilization(
            SchedulingSolution solution,
            SchedulingProblem problem,
            SchedulingStatistics stats)
        {
            // 计算每个教室的利用情况
            foreach (var classroom in problem.Classrooms)
            {
                var assignments = solution.Assignments.Where(a => a.ClassroomId == classroom.Id).ToList();

                if (assignments.Any())
                {
                    // 假设每周有5天，每天10小时可用时间
                    double totalAvailableHours = 5 * 10.0;

                    // 计算已用课时（假设每个时间槽是1.5小时）
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

            // 计算平均教室利用率
            if (stats.ClassroomUtilization.Count > 0)
            {
                stats.AverageClassroomUtilization = stats.ClassroomUtilization.Values
                    .Average(info => info.UtilizationRate);
            }
        }

        /// <summary>
        /// 计算教师工作量信息
        /// </summary>
        private void ComputeTeacherWorkloads(
            SchedulingSolution solution,
            SchedulingProblem problem,
            SchedulingStatistics stats)
        {
            // 计算每个教师的工作量
            foreach (var teacher in problem.Teachers)
            {
                var assignments = solution.Assignments.Where(a => a.TeacherId == teacher.Id).ToList();

                if (assignments.Any())
                {
                    // 计算总课时（假设每个时间槽是1.5小时）
                    int totalHours = (int)(assignments.Count * 1.5);

                    // 按日统计课时
                    var dailyWorkload = assignments
                        .GroupBy(a => a.DayOfWeek)
                        .ToDictionary(
                            g => g.Key,
                            g => (int)(g.Count() * 1.5));

                    // 计算最大日课时
                    int maxDailyHours = dailyWorkload.Any()
                        ? dailyWorkload.Values.Max()
                        : 0;

                    // 计算最大连续课时（这需要详细的时间槽信息）
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

            // 计算教师工作量标准差
            if (stats.TeacherWorkloads.Count > 0)
            {
                var workloads = stats.TeacherWorkloads.Values.Select(info => info.TotalHours).ToList();
                double avg = workloads.Average();
                double sumOfSquares = workloads.Sum(x => Math.Pow(x - avg, 2));
                stats.TeacherWorkloadStdDev = Math.Sqrt(sumOfSquares / workloads.Count);
            }
        }

        /// <summary>
        /// 计算最大连续课时
        /// </summary>
        private int CalculateMaxConsecutiveHours(List<SchedulingAssignment> assignments)
        {
            // 按天分组
            var assignmentsByDay = assignments.GroupBy(a => a.DayOfWeek).ToDictionary(g => g.Key, g => g.ToList());

            int maxConsecutive = 0;

            foreach (var dayAssignments in assignmentsByDay.Values)
            {
                // 按开始时间排序
                var sortedAssignments = dayAssignments.OrderBy(a => a.StartTime).ToList();

                int currentConsecutive = 1;

                for (int i = 1; i < sortedAssignments.Count; i++)
                {
                    var prev = sortedAssignments[i - 1];
                    var curr = sortedAssignments[i];

                    // 如果两节课间隔小于或等于30分钟，视为连续
                    // 假设结束时间和开始时间格式为 TimeSpan
                    if ((curr.StartTime - prev.EndTime).TotalMinutes <= 30)
                    {
                        currentConsecutive++;
                    }
                    else
                    {
                        // 重置计数器
                        currentConsecutive = 1;
                    }

                    maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
                }
            }

            return maxConsecutive;
        }

        /// <summary>
        /// 计算时间槽利用率信息
        /// </summary>
        private void ComputeTimeSlotUtilization(
            SchedulingSolution solution,
            SchedulingProblem problem,
            SchedulingStatistics stats)
        {
            // 计算每个时间槽的利用情况
            foreach (var timeSlot in problem.TimeSlots)
            {
                var assignments = solution.Assignments.Where(a => a.TimeSlotId == timeSlot.Id).ToList();

                // 假设总教室数作为基准
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

            // 计算平均时间槽利用率
            if (stats.TimeSlotUtilization.Count > 0)
            {
                stats.AverageTimeSlotUtilization = stats.TimeSlotUtilization.Values
                    .Average(info => info.UtilizationRate);

                // 找出峰值和谷值时段
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
        /// 检查问题可行性
        /// </summary>
        private bool CheckFeasibility(SchedulingProblem problem)
        {
            try
            {
                _logger.LogInformation("检查排课问题可行性...");

                // 使用CP求解器检查是否存在可行解
                CpSolverStatus status;
                bool isFeasible = _cpScheduler.CheckFeasibility(problem, out status);

                if (isFeasible)
                {
                    _logger.LogInformation("排课问题有可行解");
                }
                else
                {
                    switch (status)
                    {
                        case CpSolverStatus.Infeasible:
                            _logger.LogWarning("排课问题无可行解，约束冲突");
                            break;
                        case CpSolverStatus.Unknown:
                            _logger.LogWarning("排课问题不确定是否有解，求解时间不足");
                            // 不确定时，尝试继续，可能仍能找到解
                            return true;
                        default:
                            _logger.LogWarning($"检查可行性返回未预期状态: {status}");
                            break;
                    }
                }

                return isFeasible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查问题可行性时出错");
                // 出错时保守处理，假设问题有解
                return true;
            }
        }

        /// <summary>
        /// 调整算法参数
        /// </summary>
        private void AdjustParameters(SchedulingProblem problem)
        {
            try
            {
                _logger.LogInformation("调整算法参数...");

                _parameterAdjuster.AdjustParameters(problem);

                // 记录调整后的关键参数
                _logger.LogInformation($"初始解数量: {_parameters.InitialSolutionCount}");
                _logger.LogInformation($"CP求解时间限制: {_parameters.CpTimeLimit}秒");
                _logger.LogInformation($"LS最大迭代次数: {_parameters.MaxLsIterations}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调整算法参数时出错");
                // 出错时使用默认参数
            }
        }
    }
}