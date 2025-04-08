// SmartSchedulingSystem.Test/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SmartSchedulingSystem.Test
{
    class Program
    {
        private static SolutionEvaluator _evaluator;

        static void Main(string[] args)
        {
            Console.WriteLine("智能排课系统算法测试");
            Console.WriteLine("===================");

            // 配置依赖注入
            var services = new ServiceCollection();
            services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information));

            // 注册排课服务
            services.AddSchedulingServices();

            // 添加测试数据生成器
            services.AddSingleton<TestDataGenerator>();

            var serviceProvider = services.BuildServiceProvider();

            // 获取所需服务
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var schedulingEngine = serviceProvider.GetRequiredService<SchedulingEngine>();
            var testDataGenerator = serviceProvider.GetRequiredService<TestDataGenerator>();
            _evaluator = serviceProvider.GetRequiredService<SolutionEvaluator>();

            try
            {
                RunSmallTest(schedulingEngine, testDataGenerator);
                RunMediumTest(schedulingEngine, testDataGenerator);
                RunConflictTest(schedulingEngine, testDataGenerator);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "测试过程中发生错误");
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n测试完成。按任意键退出...");
            Console.ReadKey();
        }

        static void RunSmallTest(SchedulingEngine schedulingEngine, TestDataGenerator testDataGenerator)
        {
            Console.WriteLine("\n=== 小规模测试 (10门课) ===");

            // 生成小规模测试问题(10门课)
            var problem = testDataGenerator.GenerateTestProblem(10, 5, 8, 20);

            Console.WriteLine($"生成了 {problem.CourseSections.Count} 门课程, {problem.Teachers.Count} 位教师, " +
                            $"{problem.Classrooms.Count} 间教室, {problem.TimeSlots.Count} 个时间槽");

            // 执行排课
            Console.WriteLine("开始排课...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var result = schedulingEngine.GenerateSchedule(problem);

            sw.Stop();
            Console.WriteLine($"排课完成！耗时: {sw.ElapsedMilliseconds}ms");

            // 分析结果
            AnalyzeResult(result);
        }

        static void RunMediumTest(SchedulingEngine schedulingEngine, TestDataGenerator testDataGenerator)
        {
            Console.WriteLine("\n=== 中等规模测试 (30门课) ===");

            // 生成中等规模测试问题(30门课)
            var problem = testDataGenerator.GenerateTestProblem(30, 10, 15, 30);

            Console.WriteLine($"生成了 {problem.CourseSections.Count} 门课程, {problem.Teachers.Count} 位教师, " +
                            $"{problem.Classrooms.Count} 间教室, {problem.TimeSlots.Count} 个时间槽");

            // 执行排课
            Console.WriteLine("开始排课...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var result = schedulingEngine.GenerateSchedule(problem);

            sw.Stop();
            Console.WriteLine($"排课完成！耗时: {sw.ElapsedMilliseconds}ms");

            // 分析结果
            AnalyzeResult(result);
        }

        static void RunConflictTest(SchedulingEngine schedulingEngine, TestDataGenerator testDataGenerator)
        {
            Console.WriteLine("\n=== 冲突处理测试 ===");

            // 生成包含冲突的测试问题
            var problem = testDataGenerator.GenerateTestProblem(15, 3, 10, 20);

            // 创建人为冲突：让某个教师在某个时间段不可用
            var teacher = problem.Teachers.First();
            var timeSlot = problem.TimeSlots.First();

            problem.TeacherAvailabilities.Add(new TeacherAvailability
            {
                TeacherId = teacher.Id,
                TimeSlotId = timeSlot.Id,
                IsAvailable = false
            });

            Console.WriteLine($"人为创建教师冲突: 教师{teacher.Id}在时间槽{timeSlot.Id}不可用");

            // 执行排课
            Console.WriteLine("开始排课...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var result = schedulingEngine.GenerateSchedule(problem);

            sw.Stop();
            Console.WriteLine($"排课完成！耗时: {sw.ElapsedMilliseconds}ms");

            // 分析结果
            AnalyzeResult(result);

            // 检查冲突是否被解决
            if (result.Solutions.Count > 0)
            {
                var bestSolution = result.Solutions.First();
                bool hasConflict = bestSolution.Assignments.Any(a =>
                    a.TeacherId == teacher.Id && a.TimeSlotId == timeSlot.Id);

                Console.WriteLine($"检查人为创建的冲突是否被解决: {(hasConflict ? "未解决" : "已解决")}");
            }
        }

        static void AnalyzeResult(SchedulingResult result)
        {
            if (result.Status == SchedulingStatus.Success)
            {
                Console.WriteLine("排课成功！");
                Console.WriteLine($"生成了 {result.Solutions.Count} 个排课方案");

                if (result.Solutions.Count > 0)
                {
                    var bestSolution = result.Solutions.First();
                    Console.WriteLine($"最优方案评分: {_evaluator.Evaluate(bestSolution).Score:F4}");
                    Console.WriteLine($"课程分配数量: {bestSolution.Assignments.Count}");

                    // 计算有效分配率
                    int totalSections = bestSolution.Problem?.CourseSections?.Count ?? 0;
                    int assignedSections = bestSolution.Assignments.Select(a => a.SectionId).Distinct().Count();

                    if (totalSections > 0)
                    {
                        double assignmentRate = (double)assignedSections / totalSections;
                        Console.WriteLine($"课程班级分配率: {assignmentRate:P2} ({assignedSections}/{totalSections})");
                    }

                    // 输出统计信息
                    if (result.Statistics != null)
                    {
                        PrintStatistics(result.Statistics);
                    }
                }
            }
            else if (result.Status == SchedulingStatus.PartialSuccess)
            {
                Console.WriteLine("排课部分成功。");
                Console.WriteLine($"生成了 {result.Solutions.Count} 个排课方案，但未能满足全部约束。");

                if (result.Solutions.Count > 0)
                {
                    var bestSolution = result.Solutions.First();
                    Console.WriteLine($"最优方案评分: {_evaluator.Evaluate(bestSolution).Score:F4}");
                    Console.WriteLine($"课程分配数量: {bestSolution.Assignments.Count}");

                    // 计算有效分配率
                    int totalSections = bestSolution.Problem?.CourseSections?.Count ?? 0;
                    int assignedSections = bestSolution.Assignments.Select(a => a.SectionId).Distinct().Count();

                    if (totalSections > 0)
                    {
                        double assignmentRate = (double)assignedSections / totalSections;
                        Console.WriteLine($"课程班级分配率: {assignmentRate:P2} ({assignedSections}/{totalSections})");
                    }

                    // 输出统计信息
                    if (result.Statistics != null)
                    {
                        PrintStatistics(result.Statistics);
                    }
                }
            }
            else
            {
                Console.WriteLine($"排课失败。状态: {result.Status}");
                Console.WriteLine($"错误信息: {result.Message}");
            }
        }

        static void PrintStatistics(SchedulingStatistics stats)
        {
            if (stats == null)
                return;

            Console.WriteLine("\n----- 排课统计信息 -----");
            Console.WriteLine($"总课程班级数: {stats.TotalSections}");
            Console.WriteLine($"已安排课程班级数: {stats.ScheduledSections}");
            Console.WriteLine($"未安排课程班级数: {stats.UnscheduledSections}");
            Console.WriteLine($"总教师数: {stats.TotalTeachers}");
            Console.WriteLine($"已分配教师数: {stats.AssignedTeachers}");
            Console.WriteLine($"总教室数: {stats.TotalClassrooms}");
            Console.WriteLine($"已使用教室数: {stats.UsedClassrooms}");

            if (stats.AverageClassroomUtilization > 0)
                Console.WriteLine($"平均教室利用率: {stats.AverageClassroomUtilization:P2}");

            if (stats.AverageTimeSlotUtilization > 0)
                Console.WriteLine($"平均时间槽利用率: {stats.AverageTimeSlotUtilization:P2}");

            if (stats.TeacherWorkloadStdDev > 0)
                Console.WriteLine($"教师工作量标准差: {stats.TeacherWorkloadStdDev:F2}");

            // 输出高峰和低谷时段
            if (stats.TimeSlotUtilization != null &&
                stats.TimeSlotUtilization.ContainsKey(stats.PeakTimeSlotId) &&
                stats.TimeSlotUtilization.ContainsKey(stats.LowestTimeSlotId))
            {
                var peakSlot = stats.TimeSlotUtilization[stats.PeakTimeSlotId];
                var lowestSlot = stats.TimeSlotUtilization[stats.LowestTimeSlotId];

                Console.WriteLine($"高峰时段: 星期{GetDayName(peakSlot.DayOfWeek)} {peakSlot.StartTime}-{peakSlot.EndTime}, 利用率: {peakSlot.UtilizationRate:P2}");
                Console.WriteLine($"低谷时段: 星期{GetDayName(lowestSlot.DayOfWeek)} {lowestSlot.StartTime}-{lowestSlot.EndTime}, 利用率: {lowestSlot.UtilizationRate:P2}");
            }
        }

        private static string GetDayName(int day)
        {
            return day switch
            {
                1 => "一",
                2 => "二",
                3 => "三",
                4 => "四",
                5 => "五",
                6 => "六",
                7 => "日",
                _ => "未知"
            };
        }
    }
}