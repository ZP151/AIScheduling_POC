using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Engine.LS;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Test
{
    /// <summary>
    /// 简化版CP-LS混合调度器，专注于算法的关键流程
    /// </summary>
    public class SimplifiedCPLSScheduler
    {
        private readonly ILogger<SimplifiedCPLSScheduler> _logger;
        private readonly SolutionEvaluator _evaluator;
        private readonly Random _random = new Random();

        public SimplifiedCPLSScheduler(
            ILogger<SimplifiedCPLSScheduler> logger,
            SolutionEvaluator evaluator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
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

                // 1. 生成初始解
                _logger.LogInformation("阶段1: 生成初始解...");
                var initialSolution = GenerateInitialSolution(problem);

                if (initialSolution == null || initialSolution.Assignments.Count == 0)
                {
                    _logger.LogWarning("无法生成有效的初始解");
                    return new SchedulingResult
                    {
                        Status = SchedulingStatus.Failure,
                        Message = "无法生成有效的初始解",
                        Solutions = new List<SchedulingSolution>(),
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }

                _logger.LogInformation($"成功生成初始解，包含 {initialSolution.Assignments.Count} 个分配");

                // 2. 优化解
                _logger.LogInformation("阶段2: 优化解...");
                var optimizedSolution = OptimizeSolution(initialSolution);

                _logger.LogInformation("优化完成");

                // 3. 准备返回结果
                var solutions = new List<SchedulingSolution> { optimizedSolution };

                sw.Stop();
                var result = new SchedulingResult
                {
                    Status = SchedulingStatus.Success,
                    Message = "成功生成排课方案",
                    Solutions = solutions,
                    ExecutionTimeMs = sw.ElapsedMilliseconds,
                    Statistics = ComputeStatistics(optimizedSolution, problem)
                };

                _logger.LogInformation($"排课完成，耗时: {sw.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成排课方案时发生异常");
                return new SchedulingResult
                {
                    Status = SchedulingStatus.Error,
                    Message = $"生成排课方案时发生异常: {ex.Message}",
                    Solutions = new List<SchedulingSolution>(),
                    ExecutionTimeMs = -1
                };
            }
        }

        /// <summary>
        /// 生成初始解 - 实现简化版启发式算法
        /// </summary>
        private SchedulingSolution GenerateInitialSolution(SchedulingProblem problem)
        {
            var solution = new SchedulingSolution
            {
                Id = 1,
                ProblemId = problem.Id,
                Problem = problem,
                Name = "初始解",
                CreatedAt = DateTime.Now,
                Algorithm = "Simplified-Heuristic",
                Assignments = new List<SchedulingAssignment>()
            };

            // 克隆各种资源列表，以便在分配过程中移除已使用的资源
            var availableSections = new List<CourseSectionInfo>(problem.CourseSections);
            var availableTimeSlots = problem.TimeSlots
                .GroupBy(ts => ts.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 教师和教室的可用时间
            var teacherAvailableTimeSlots = new Dictionary<int, List<int>>();
            var classroomAvailableTimeSlots = new Dictionary<int, List<int>>();

            // 初始化教师和教室的可用时间
            foreach (var teacher in problem.Teachers)
            {
                teacherAvailableTimeSlots[teacher.Id] = problem.TimeSlots
                    .Where(ts => !problem.TeacherAvailabilities.Any(ta =>
                        ta.TeacherId == teacher.Id &&
                        ta.TimeSlotId == ts.Id &&
                        !ta.IsAvailable))
                    .Select(ts => ts.Id)
                    .ToList();
            }

            foreach (var classroom in problem.Classrooms)
            {
                classroomAvailableTimeSlots[classroom.Id] = problem.TimeSlots
                    .Where(ts => !problem.ClassroomAvailabilities.Any(ca =>
                        ca.ClassroomId == classroom.Id &&
                        ca.TimeSlotId == ts.Id &&
                        !ca.IsAvailable))
                    .Select(ts => ts.Id)
                    .ToList();
            }

            // 按照学生人数降序排列课程班级，优先安排人数多的班级
            availableSections.Sort((a, b) => b.Enrollment.CompareTo(a.Enrollment));

            int assignmentId = 1;

            // 为每个课程班级分配资源
            foreach (var section in availableSections)
            {
                // 1. 找到合适的教师（能教这门课程的）
                var suitableTeachers = problem.TeacherCoursePreferences
                    .Where(tcp => tcp.CourseId == section.CourseId && tcp.ProficiencyLevel >= 3)
                    .Select(tcp => tcp.TeacherId)
                    .ToList();

                if (suitableTeachers.Count == 0)
                {
                    // 如果没有找到合适的教师，跳过这个班级
                    _logger.LogWarning($"找不到能教授课程 {section.CourseName} 的教师，跳过此班级");
                    continue;
                }

                // 2. 找到合适的教室（容量足够的）
                var suitableClassrooms = problem.Classrooms
                    .Where(c => c.Capacity >= section.Enrollment)
                    .OrderBy(c => c.Capacity - section.Enrollment) // 最小足够容量优先
                    .Select(c => c.Id)
                    .ToList();

                if (suitableClassrooms.Count == 0)
                {
                    // 如果没有找到合适的教室，跳过这个班级
                    _logger.LogWarning($"找不到容量足够的教室给课程 {section.CourseName}，跳过此班级");
                    continue;
                }

                // 3. 尝试为这个班级分配教师、教室和时间槽
                bool assigned = false;

                foreach (var teacherId in suitableTeachers)
                {
                    if (assigned) break;

                    var teacherAvailableTimes = teacherAvailableTimeSlots[teacherId];

                    foreach (var classroomId in suitableClassrooms)
                    {
                        if (assigned) break;

                        var classroomAvailableTimes = classroomAvailableTimeSlots[classroomId];

                        // 查找教师和教室都可用的时间槽
                        var commonAvailableTimes = teacherAvailableTimes
                            .Intersect(classroomAvailableTimes)
                            .ToList();

                        if (commonAvailableTimes.Count > 0)
                        {
                            // 随机选择一个可用时间槽
                            int timeSlotId = commonAvailableTimes[_random.Next(commonAvailableTimes.Count)];
                            var timeSlot = problem.TimeSlots.First(ts => ts.Id == timeSlotId);

                            // 创建排课分配
                            var assignment = new SchedulingAssignment
                            {
                                Id = assignmentId++,
                                SectionId = section.Id,
                                SectionCode = section.SectionCode,
                                TeacherId = teacherId,
                                TeacherName = problem.Teachers.First(t => t.Id == teacherId).Name,
                                ClassroomId = classroomId,
                                ClassroomName = problem.Classrooms.First(c => c.Id == classroomId).Name,
                                TimeSlotId = timeSlotId,
                                DayOfWeek = timeSlot.DayOfWeek,
                                StartTime = timeSlot.StartTime,
                                EndTime = timeSlot.EndTime,
                                WeekPattern = Enumerable.Range(1, 15).ToList() // 假设15周都上课
                            };

                            solution.Assignments.Add(assignment);

                            // 从可用时间列表中移除已分配的时间槽
                            teacherAvailableTimeSlots[teacherId].Remove(timeSlotId);
                            classroomAvailableTimeSlots[classroomId].Remove(timeSlotId);

                            assigned = true;
                        }
                    }
                }

                if (!assigned)
                {
                    _logger.LogWarning($"无法为课程 {section.CourseName} 分配资源");
                }
            }

            return solution;
        }

        /// <summary>
        /// 优化解 - 实现简化版模拟退火算法
        /// </summary>
        private SchedulingSolution OptimizeSolution(SchedulingSolution initialSolution)
        {
            var currentSolution = initialSolution.Clone();
            var bestSolution = initialSolution.Clone();
            double bestScore = _evaluator.Evaluate(bestSolution).Score;

            _logger.LogInformation($"初始解评分: {bestScore:F4}");

            // 模拟退火参数
            double temperature = 1.0;
            double coolingRate = 0.995;
            double finalTemperature = 0.01;
            int maxIterations = 1000;

            int iteration = 0;
            int noImprovementCount = 0;
            const int maxNoImprovement = 100;

            while (temperature > finalTemperature && iteration < maxIterations)
            {
                iteration++;

                // 生成邻域解
                var neighborSolution = GenerateNeighborSolution(currentSolution);
                double neighborScore = _evaluator.Evaluate(neighborSolution).Score;

                // 决定是否接受新解
                bool accept = false;

                if (neighborScore > bestScore)
                {
                    // 如果新解更好，总是接受
                    accept = true;
                    bestSolution = neighborSolution.Clone();
                    bestScore = neighborScore;
                    noImprovementCount = 0;

                    _logger.LogDebug($"迭代 {iteration}: 发现更好的解，评分: {bestScore:F4}");
                }
                else
                {
                    // 根据温度和评分差异计算接受概率
                    double scoreDifference = neighborScore - bestScore;
                    double acceptanceProbability = Math.Exp(scoreDifference / temperature);

                    // 随机决定是否接受
                    if (_random.NextDouble() < acceptanceProbability)
                    {
                        accept = true;
                        noImprovementCount++;
                    }
                }

                if (accept)
                {
                    currentSolution = neighborSolution;
                }
                else
                {
                    noImprovementCount++;
                }

                // 降低温度
                temperature *= coolingRate;

                // 每50次迭代输出一次进度
                if (iteration % 50 == 0)
                {
                    _logger.LogInformation($"已完成 {iteration} 次迭代，当前最佳评分: {bestScore:F4}, 温度: {temperature:F6}");
                }

                // 如果长时间无改进，提前终止
                if (noImprovementCount >= maxNoImprovement)
                {
                    _logger.LogInformation($"连续 {maxNoImprovement} 次无改进，提前终止搜索");
                    break;
                }
            }

            _logger.LogInformation($"优化完成，共 {iteration} 次迭代，最终评分: {bestScore:F4}");

            return bestSolution;
        }

        /// <summary>
        /// 生成邻域解 - 随机选择一种移动操作
        /// </summary>
        private SchedulingSolution GenerateNeighborSolution(SchedulingSolution solution)
        {
            var newSolution = solution.Clone();

            if (newSolution.Assignments.Count == 0)
            {
                return newSolution;
            }

            // 随机选择一个分配
            int randomIndex = _random.Next(newSolution.Assignments.Count);
            var randomAssignment = newSolution.Assignments[randomIndex];

            // 随机选择一种移动操作
            int moveType = _random.Next(3);

            switch (moveType)
            {
                case 0:
                    // 时间移动 - 更换时间槽
                    MoveTime(newSolution, randomAssignment);
                    break;
                case 1:
                    // 教室移动 - 更换教室
                    MoveRoom(newSolution, randomAssignment);
                    break;
                case 2:
                    // 交换移动 - 与另一个分配交换时间或教室
                    SwapAssignments(newSolution, randomAssignment);
                    break;
            }

            return newSolution;
        }

        /// <summary>
        /// 时间移动 - 将一个分配移动到另一个时间槽
        /// </summary>
        private void MoveTime(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            if (solution.Problem == null || solution.Problem.TimeSlots == null || solution.Problem.TimeSlots.Count == 0)
            {
                return;
            }

            // 随机选择一个新的时间槽
            var availableTimeSlots = solution.Problem.TimeSlots
                .Where(ts => ts.Id != assignment.TimeSlotId)
                .ToList();

            if (availableTimeSlots.Count == 0)
            {
                return;
            }

            var newTimeSlot = availableTimeSlots[_random.Next(availableTimeSlots.Count)];

            // 检查教师和教室在新时间槽是否可用
            bool teacherAvailable = !solution.Assignments.Any(a =>
                a.Id != assignment.Id &&
                a.TeacherId == assignment.TeacherId &&
                a.TimeSlotId == newTimeSlot.Id);

            bool roomAvailable = !solution.Assignments.Any(a =>
                a.Id != assignment.Id &&
                a.ClassroomId == assignment.ClassroomId &&
                a.TimeSlotId == newTimeSlot.Id);

            if (teacherAvailable && roomAvailable)
            {
                // 更新时间槽
                assignment.TimeSlotId = newTimeSlot.Id;
                assignment.DayOfWeek = newTimeSlot.DayOfWeek;
                assignment.StartTime = newTimeSlot.StartTime;
                assignment.EndTime = newTimeSlot.EndTime;
            }
        }

        /// <summary>
        /// 教室移动 - 将一个分配移动到另一个教室
        /// </summary>
        private void MoveRoom(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            if (solution.Problem == null || solution.Problem.Classrooms == null || solution.Problem.Classrooms.Count == 0)
            {
                return;
            }

            // 随机选择一个新的教室
            var availableRooms = solution.Problem.Classrooms
                .Where(r => r.Id != assignment.ClassroomId)
                .ToList();

            if (availableRooms.Count == 0)
            {
                return;
            }

            var newRoom = availableRooms[_random.Next(availableRooms.Count)];

            // 检查新教室在当前时间槽是否可用
            bool roomAvailable = !solution.Assignments.Any(a =>
                a.Id != assignment.Id &&
                a.ClassroomId == newRoom.Id &&
                a.TimeSlotId == assignment.TimeSlotId);

            if (roomAvailable)
            {
                // 更新教室
                assignment.ClassroomId = newRoom.Id;
                assignment.ClassroomName = newRoom.Name;
            }
        }

        /// <summary>
        /// 交换移动 - 与另一个分配交换时间或教室
        /// </summary>
        private void SwapAssignments(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            if (solution.Assignments.Count < 2)
            {
                return;
            }

            // 随机选择另一个不同的分配
            var otherAssignments = solution.Assignments
                .Where(a => a.Id != assignment.Id)
                .ToList();

            if (otherAssignments.Count == 0)
            {
                return;
            }

            var otherAssignment = otherAssignments[_random.Next(otherAssignments.Count)];

            // 随机决定交换内容 (0: 时间, 1: 教室, 2: 两者都交换)
            int swapType = _random.Next(3);

            // 交换时间
            if (swapType == 0 || swapType == 2)
            {
                // 交换时间槽信息
                (assignment.TimeSlotId, otherAssignment.TimeSlotId) = (otherAssignment.TimeSlotId, assignment.TimeSlotId);
                (assignment.DayOfWeek, otherAssignment.DayOfWeek) = (otherAssignment.DayOfWeek, assignment.DayOfWeek);
                (assignment.StartTime, otherAssignment.StartTime) = (otherAssignment.StartTime, assignment.StartTime);
                (assignment.EndTime, otherAssignment.EndTime) = (otherAssignment.EndTime, assignment.EndTime);
            }

            // 交换教室
            if (swapType == 1 || swapType == 2)
            {
                // 交换教室信息
                (assignment.ClassroomId, otherAssignment.ClassroomId) = (otherAssignment.ClassroomId, assignment.ClassroomId);
                (assignment.ClassroomName, otherAssignment.ClassroomName) = (otherAssignment.ClassroomName, assignment.ClassroomName);
            }
        }

        /// <summary>
        /// 计算统计信息
        /// </summary>
        private SchedulingStatistics ComputeStatistics(SchedulingSolution solution, SchedulingProblem problem)
        {
            var stats = new SchedulingStatistics
            {
                TotalSections = problem.CourseSections.Count,
                ScheduledSections = solution.Assignments.Select(a => a.SectionId).Distinct().Count(),
                TotalTeachers = problem.Teachers.Count,
                AssignedTeachers = solution.Assignments.Select(a => a.TeacherId).Distinct().Count(),
                TotalClassrooms = problem.Classrooms.Count,
                UsedClassrooms = solution.Assignments.Select(a => a.ClassroomId).Distinct().Count()
            };

            stats.UnscheduledSections = stats.TotalSections - stats.ScheduledSections;

            // 计算教室利用率
            var usedClassroomIds = solution.Assignments.Select(a => a.ClassroomId).Distinct().ToList();
            foreach (var classroomId in usedClassroomIds)
            {
                var classroom = problem.Classrooms.FirstOrDefault(c => c.Id == classroomId);
                if (classroom == null) continue;

                var assignments = solution.Assignments.Where(a => a.ClassroomId == classroomId).ToList();

                // 计算利用率（假设每周5天，每天10小时）
                double totalAvailableHours = 5 * 10.0;
                double usedHours = assignments.Count * 1.5; // 假设每个分配是1.5小时
                double utilizationRate = usedHours / totalAvailableHours;

                stats.ClassroomUtilization[classroomId] = new ClassroomUtilizationInfo
                {
                    ClassroomId = classroomId,
                    ClassroomName = classroom.Name,
                    Building = classroom.Building,
                    UtilizationRate = utilizationRate,
                    AssignmentCount = assignments.Count
                };
            }

            // 计算平均教室利用率
            if (stats.ClassroomUtilization.Count > 0)
            {
                stats.AverageClassroomUtilization = stats.ClassroomUtilization.Values.Average(info => info.UtilizationRate);
            }

            // 计算教师工作量
            var usedTeacherIds = solution.Assignments.Select(a => a.TeacherId).Distinct().ToList();
            foreach (var teacherId in usedTeacherIds)
            {
                var teacher = problem.Teachers.FirstOrDefault(t => t.Id == teacherId);
                if (teacher == null) continue;

                var assignments = solution.Assignments.Where(a => a.TeacherId == teacherId).ToList();

                // 计算总学时和每日工作量
                int totalHours = assignments.Count * 2; // 假设每节课2学时
                var dailyWorkload = assignments.GroupBy(a => a.DayOfWeek)
                    .ToDictionary(g => g.Key, g => g.Count() * 2);

                int maxDailyHours = dailyWorkload.Count > 0 ? dailyWorkload.Values.Max() : 0;

                stats.TeacherWorkloads[teacherId] = new TeacherWorkloadInfo
                {
                    TeacherId = teacherId,
                    TeacherName = teacher.Name,
                    TotalHours = totalHours,
                    DailyWorkload = dailyWorkload,
                    MaxDailyHours = maxDailyHours,
                    AssignmentCount = assignments.Count
                };
            }

            // 计算教师工作量标准差
            if (stats.TeacherWorkloads.Count > 0)
            {
                var workloads = stats.TeacherWorkloads.Values.Select(info => info.TotalHours).ToList();
                double avg = workloads.Average();
                double sumOfSquares = workloads.Sum(x => Math.Pow(x - avg, 2));
                stats.TeacherWorkloadStdDev = Math.Sqrt(sumOfSquares / workloads.Count);
            }

            // 计算时间槽利用率
            foreach (var timeSlot in problem.TimeSlots)
            {
                var assignments = solution.Assignments.Where(a => a.TimeSlotId == timeSlot.Id).ToList();

                // 利用率 = 已使用教室数 / 总教室数
                double utilizationRate = problem.Classrooms.Count > 0
                    ? (double)assignments.Count / problem.Classrooms.Count
                    : 0;

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
                stats.AverageTimeSlotUtilization = stats.TimeSlotUtilization.Values.Average(info => info.UtilizationRate);

                // 找出峰值和谷值时段
                var peakSlot = stats.TimeSlotUtilization.Values.OrderByDescending(info => info.UtilizationRate).First();
                var lowestSlot = stats.TimeSlotUtilization.Values.OrderBy(info => info.UtilizationRate).First();

                stats.PeakTimeSlotId = peakSlot.TimeSlotId;
                stats.PeakTimeSlotUtilization = peakSlot.UtilizationRate;
                stats.LowestTimeSlotId = lowestSlot.TimeSlotId;
                stats.LowestTimeSlotUtilization = lowestSlot.UtilizationRate;
            }

            return stats;
        }
    }
}