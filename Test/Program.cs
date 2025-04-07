using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Engine.Hybrid;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using SmartSchedulingSystem.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 配置依赖注入容器
            var serviceProvider = ConfigureServices();

            // 创建日志记录器
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("智能排课系统算法测试开始");

            try
            {
                // 获取测试数据生成器
                var testDataGenerator = serviceProvider.GetRequiredService<TestDataGenerator>();

                // 生成测试排课问题
                int courseSectionCount = 20;  // 课程班级数量
                int teacherCount = 10;        // 教师数量
                int classroomCount = 15;      // 教室数量
                int timeSlotCount = 30;       // 时间槽数量

                logger.LogInformation($"生成测试排课问题: {courseSectionCount}个班级, {teacherCount}个教师, {classroomCount}个教室, {timeSlotCount}个时间槽");

                var problem = testDataGenerator.GenerateTestProblem(
                    courseSectionCount,
                    teacherCount,
                    classroomCount,
                    timeSlotCount);

                // 获取排课引擎
                var schedulingEngine = serviceProvider.GetRequiredService<SchedulingEngine>();

                // 执行排课
                logger.LogInformation("开始执行排课算法...");
                var result = schedulingEngine.GenerateSchedule(problem);

                // 分析结果
                AnalyzeResult(result, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "排课测试过程中发生错误");
            }

            logger.LogInformation("智能排课系统算法测试结束");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 添加日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            // 注册容量提供者（必须）
            services.AddSingleton<IClassroomCapacityProvider, TestClassroomCapacityProvider>();
            services.AddSingleton(new Dictionary<int, int>()); // <-- 就是它！
            services.AddSingleton(new Dictionary<int, List<int>>());

            // 添加排课服务
            services.AddSchedulingServices();

            // 添加测试数据生成器s
            services.AddSingleton<TestDataGenerator>();

            return services.BuildServiceProvider();
        }

        private static void AnalyzeResult(SchedulingResult result, ILogger logger)
        {
            if (result.Solutions.Count == 0)
            {
                logger.LogWarning("未找到可行的排课方案");
                return;
            }

            logger.LogInformation($"排课结果状态: {result.Status}");
            logger.LogInformation($"执行时间: {result.ExecutionTimeMs}ms");
            logger.LogInformation($"找到 {result.Solutions.Count} 个解决方案");

            // 分析最佳解决方案
            var bestSolution = result.Solutions[0];
            logger.LogInformation($"最佳方案ID: {bestSolution.Id}, 分配数量: {bestSolution.Assignments.Count}");

            // 输出统计信息
            if (result.Statistics != null)
            {
                logger.LogInformation("\n统计信息:");
                logger.LogInformation($"总班级数: {result.Statistics.TotalSections}, 已安排: {result.Statistics.ScheduledSections}, 未安排: {result.Statistics.UnscheduledSections}");
                logger.LogInformation($"总教师数: {result.Statistics.TotalTeachers}, 已分配: {result.Statistics.AssignedTeachers}");
                logger.LogInformation($"总教室数: {result.Statistics.TotalClassrooms}, 已使用: {result.Statistics.UsedClassrooms}");
                logger.LogInformation($"平均教室利用率: {result.Statistics.AverageClassroomUtilization:P2}");
                logger.LogInformation($"平均时间槽利用率: {result.Statistics.AverageTimeSlotUtilization:P2}");
                logger.LogInformation($"教师工作量标准差: {result.Statistics.TeacherWorkloadStdDev:F2}");
            }

            // 创建可视化工具
            var visualizer = new ScheduleVisualizer(logger);

            // 获取并显示冲突信息
            var conflicts = GetConflicts(bestSolution);
            if (conflicts.Count > 0)
            {
                logger.LogWarning($"检测到 {conflicts.Count} 个冲突");
                visualizer.GenerateConflictReport(conflicts);
            }
            else
            {
                logger.LogInformation("排课方案没有冲突");
            }

            logger.LogInformation("\n是否显示详细排课结果? (y/n)");
            var input = Console.ReadLine()?.ToLower();

            if (input == "y")
            {
                // 显示各种排课视图
                visualizer.GenerateTeacherView(bestSolution);
                Console.WriteLine("\n按任意键显示教室视图...");
                Console.ReadKey();

                visualizer.GenerateClassroomView(bestSolution);
                Console.WriteLine("\n按任意键继续...");
                Console.ReadKey();

                // 时间表格视图需要时间槽信息
                if (bestSolution.Problem != null && bestSolution.Problem.TimeSlots != null)
                {
                    visualizer.GenerateTimeTableView(bestSolution, bestSolution.Problem.TimeSlots);
                }
            }
            else
            {
                // 只显示简单摘要
                PrintSampleSchedule(bestSolution, logger);
            }
        }

        private static List<SchedulingConflict> GetConflicts(SchedulingSolution solution)
        {
            var evaluator = new SimpleEvaluator(null);
            return evaluator.CheckHardConstraints(solution);
        }

        private static void PrintSampleSchedule(SchedulingSolution solution, ILogger logger)
        {
            // 打印前10个排课分配作为示例
            int count = Math.Min(10, solution.Assignments.Count);

            logger.LogInformation("\n排课方案样本:");
            for (int i = 0; i < count; i++)
            {
                var assignment = solution.Assignments[i];
                logger.LogInformation(
                    $"分配 #{i + 1}: 课程={assignment.SectionCode}, " +
                    $"教师={assignment.TeacherName}, " +
                    $"教室={assignment.ClassroomName}, " +
                    $"时间=周{assignment.DayOfWeek} {assignment.StartTime}-{assignment.EndTime}");
            }

            if (solution.Assignments.Count > 10)
            {
                logger.LogInformation($"... 还有 {solution.Assignments.Count - 10} 个分配");
            }
        }
    }
}