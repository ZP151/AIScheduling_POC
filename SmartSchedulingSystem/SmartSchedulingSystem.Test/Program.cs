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
using Xunit;

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
                //RunSmallTest(schedulingEngine, testDataGenerator);
                //RunMediumTest(schedulingEngine, testDataGenerator);
                //RunConflictTest(schedulingEngine, testDataGenerator);
                Console.WriteLine("\n=== 第一步：验证简单可行问题 ===");
                // 先验证CreateDebugFeasibleProblem是否成功
                RunSimpleTest(schedulingEngine, testDataGenerator);

                //Console.WriteLine("\n=== 第二步：测试现实场景数据 ===");
                //// 再运行现实场景测试
                //RunRealisticTests(schedulingEngine, testDataGenerator);
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
        static void RunSimpleTest(SchedulingEngine schedulingEngine, TestDataGenerator testDataGenerator)
        {
            Console.WriteLine("运行简单可行性测试...");

            // 使用DebugFeasibleProblem
            var problem = testDataGenerator.CreateDebugFeasibleProblem();

            Console.WriteLine($"生成了简单问题: {problem.CourseSections.Count}门课程, " +
                            $"{problem.Teachers.Count}位教师, {problem.Classrooms.Count}间教室, " +
                            $"{problem.TimeSlots.Count}个时间槽");

            // 设置算法参数
            var parameters = new SchedulingParameters
            {
                InitialSolutionCount = 1,
                CpTimeLimit = 60,
                MaxLsIterations = 100,
                EnableParallelOptimization = false
            };

            // 执行排课
            Console.WriteLine("开始排课...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var result = schedulingEngine.GenerateSchedule(problem, parameters);

            sw.Stop();
            Console.WriteLine($"排课完成！耗时: {sw.ElapsedMilliseconds}ms, 状态: {result.Status}");

            // 验证结果
            Assert.Equal(SchedulingStatus.Success, result.Status);
            Assert.True(result.Solutions.Count > 0, "应该生成至少一个解");

            // 分析结果
            AnalyzeResult(result);
        }

        static void RunRealisticTests(SchedulingEngine schedulingEngine, TestDataGenerator testDataGenerator)
        {
            // 使用改进的测试数据生成器
            RunRealisticTest(schedulingEngine, testDataGenerator, "小型现实场景 (5门课)", 5);
            RunRealisticTest(schedulingEngine, testDataGenerator, "中型现实场景 (10门课)", 10);
            RunRealisticTest(schedulingEngine, testDataGenerator, "大型现实场景 (20门课)", 20);
        }

        static void RunRealisticTest(SchedulingEngine schedulingEngine, TestDataGenerator testDataGenerator, string testName, int courseCount)
        {
            Console.WriteLine($"\n=== {testName} ===");

            // 生成现实场景测试数据
            var problem = testDataGenerator.GenerateTestProblem(
                courseSectionCount: courseCount,
                teacherCount: courseCount * 2, // 多一些教师选择
                classroomCount: courseCount * 3, // 多一些教室选择
                timeSlotCount: courseCount * 4 // 多一些时间选择
            );

            Console.WriteLine($"生成了现实场景测试数据: {problem.CourseSections.Count}门课程, " +
                            $"{problem.Teachers.Count}位教师, {problem.Classrooms.Count}间教室, " +
                            $"{problem.TimeSlots.Count}个时间槽");
            Console.WriteLine($"教师课程偏好: {problem.TeacherCoursePreferences.Count}个");
            Console.WriteLine($"教师不可用时间段: {problem.TeacherAvailabilities.Count}个");
            Console.WriteLine($"教室不可用时间段: {problem.ClassroomAvailabilities.Count}个");

            // 设置算法参数 - 增加超时时间
            var parameters = new SchedulingParameters
            {
                InitialSolutionCount = 2,
                CpTimeLimit = courseCount * 10, // 根据课程数量动态设置CP超时时间
                MaxLsIterations = 200,
                EnableParallelOptimization = true,
                MaxParallelism = 4
            };

            // 执行排课
            Console.WriteLine("开始排课...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var result = schedulingEngine.GenerateSchedule(problem, parameters);

            sw.Stop();
            Console.WriteLine($"排课完成！耗时: {sw.ElapsedMilliseconds}ms, 状态: {result.Status}");

            // 分析结果
            AnalyzeResult(result);

            // 如果成功，分析排课质量更详细指标
            if (result.Status == SchedulingStatus.Success && result.Solutions.Count > 0)
            {
                AnalyzeScheduleQuality(result.Solutions.First(), problem);
            }
        }

        static void RunSmallTest(SchedulingEngine schedulingEngine, TestDataGenerator testDataGenerator)
        {

            //var problem = testDataGenerator.CreateSimpleValidProblem();
            //var problem = testDataGenerator.CreateDebugFeasibleProblem();
            var problem = testDataGenerator.GenerateTestProblem();
            //var problem = testDataGenerator.CreateGuaranteedFeasibleProblem();

            Console.WriteLine($"生成了 {problem.CourseSections.Count} 门课程, {problem.Teachers.Count} 位教师, " +
                            $"{problem.Classrooms.Count} 间教室, {problem.TimeSlots.Count} 个时间槽");

            // 设置算法参数
            var parameters = new SchedulingParameters
            {
                InitialSolutionCount = 1,
                CpTimeLimit = 120,
                MaxLsIterations = 100,
                EnableParallelOptimization = false
            };
            // 执行排课
            Console.WriteLine("开始排课...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var result = schedulingEngine.GenerateSchedule(problem, parameters);

            sw.Stop();
            Console.WriteLine($"排课完成！耗时: {sw.ElapsedMilliseconds}ms");
            
            // 验证结果
            Assert.NotNull(result);
            Assert.Equal(SchedulingStatus.Success, result.Status);
            Assert.True(result.Solutions.Count > 0);

            // 验证解决方案
            var solution = result.Solutions.First();
            Assert.NotNull(solution);
            Assert.Equal(3, solution.Assignments.Count); // 应该有3个分配

            // 验证所有课程都被分配
            var assignedCourses = solution.Assignments.Select(a => a.SectionId).Distinct().ToList();
            Assert.Equal(3, assignedCourses.Count);

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
        static void AnalyzeScheduleQuality(SchedulingSolution solution, SchedulingProblem problem)
        {
            Console.WriteLine("\n----- 排课质量分析 -----");

            // 1. 教师负荷分析
            var teacherWorkloads = solution.Assignments
                .GroupBy(a => a.TeacherId)
                .Select(g => new {
                    TeacherId = g.Key,
                    TeacherName = g.First().TeacherName,
                    CourseCount = g.Count(),
                    SectionIds = g.Select(a => a.SectionId).Distinct().Count()
                })
                .OrderByDescending(x => x.CourseCount)
                .ToList();

            Console.WriteLine("教师负荷分布:");
            foreach (var teacher in teacherWorkloads.Take(5))
            {
                Console.WriteLine($"  {teacher.TeacherName}: {teacher.CourseCount}次课，{teacher.SectionIds}门不同课程");
            }

            double avgTeacherWorkload = teacherWorkloads.Select(t => t.CourseCount).Average();
            double stdDevTeacherWorkload = Math.Sqrt(
                teacherWorkloads.Sum(t => Math.Pow(t.CourseCount - avgTeacherWorkload, 2)) / teacherWorkloads.Count);

            Console.WriteLine($"平均教师负荷: {avgTeacherWorkload:F2}次课，标准差: {stdDevTeacherWorkload:F2}");

            // 2. 教室利用率分析
            var roomUtilizations = solution.Assignments
                .GroupBy(a => a.ClassroomId)
                .Select(g => new {
                    RoomId = g.Key,
                    RoomName = g.First().ClassroomName,
                    UsageCount = g.Count()
                })
                .OrderByDescending(x => x.UsageCount)
                .ToList();

            Console.WriteLine("\n教室利用率:");
            foreach (var room in roomUtilizations.Take(5))
            {
                Console.WriteLine($"  {room.RoomName}: {room.UsageCount}次课");
            }

            // 3. 时间槽分布
            var timeSlotDistribution = solution.Assignments
                .GroupBy(a => a.DayOfWeek)
                .Select(g => new {
                    Day = g.Key,
                    DayName = GetDayName(g.Key),
                    Count = g.Count()
                })
                .OrderBy(x => x.Day)
                .ToList();

            Console.WriteLine("\n各天课程分布:");
            foreach (var day in timeSlotDistribution)
            {
                Console.WriteLine($"  {day.DayName}: {day.Count}次课");
            }

            // 4. 匹配度分析
            int matchingRoomTypes = 0;
            int totalAssignments = solution.Assignments.Count;

            foreach (var assignment in solution.Assignments)
            {
                var section = problem.CourseSections.FirstOrDefault(s => s.Id == assignment.SectionId);
                var room = problem.Classrooms.FirstOrDefault(r => r.Id == assignment.ClassroomId);

                if (section != null && room != null)
                {
                    if (string.IsNullOrEmpty(section.RequiredRoomType) ||
                        section.RequiredRoomType == "Regular" ||
                        section.RequiredRoomType == room.Type)
                    {
                        matchingRoomTypes++;
                    }
                }
            }

            double roomTypeMatchRate = totalAssignments > 0 ? (double)matchingRoomTypes / totalAssignments : 0;
            Console.WriteLine($"\n教室类型匹配率: {roomTypeMatchRate:P2}");
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