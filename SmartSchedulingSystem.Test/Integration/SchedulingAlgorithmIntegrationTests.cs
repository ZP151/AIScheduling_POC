using Microsoft.Extensions.DependencyInjection;
using SmartSchedulingSystem.Scheduling;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using SmartSchedulingSystem.Test.TestData;
using System;
using System.Linq;
using Xunit;

namespace SmartSchedulingSystem.Test.Integration
{
    public class SchedulingAlgorithmIntegrationTests
    {
        private readonly IServiceProvider _serviceProvider;

        public SchedulingAlgorithmIntegrationTests()
        {
            // 设置依赖注入
            var services = new ServiceCollection();

            // 首先创建并配置参数
            var parameters = new SchedulingParameters
            {
                CpTimeLimit = 300, // 增加到5分钟
                InitialSolutionCount = 1, // 减少到只需要1个解
                MaxLsIterations = 0 // 关闭局部搜索
            };
            services.AddSingleton(parameters);

            // 注册排课服务
            services.AddSchedulingServices();
            
            services.AddLogging();

            // 构建服务提供者
            _serviceProvider = services.BuildServiceProvider();
            // 验证参数是否正确注册
            var registeredParams = _serviceProvider.GetRequiredService<SchedulingParameters>();
            Console.WriteLine($"CP求解时间限制: {registeredParams.CpTimeLimit}秒");
            Console.WriteLine($"初始解数量: {registeredParams.InitialSolutionCount}");
            Console.WriteLine($"最大LS迭代: {registeredParams.MaxLsIterations}");
        }
        [Fact]
        public void TestSchedulingAlgorithm_WithSuperSimpleData_ShouldSucceed()
        {
            // 创建超简单测试数据
            var testProblem = SuperSimpleTestDataProvider.CreateSuperSimpleTestProblem();

            // 获取排课引擎
            var schedulingEngine = _serviceProvider.GetRequiredService<SchedulingEngine>();
            
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 添加一些调试输出
            Console.WriteLine($"测试问题: {testProblem.Name}");
            Console.WriteLine($"课程数: {testProblem.CourseSections.Count}");
            Console.WriteLine($"教师数: {testProblem.Teachers.Count}");
            Console.WriteLine($"教室数: {testProblem.Classrooms.Count}");
            Console.WriteLine($"时间槽数: {testProblem.TimeSlots.Count}");

            // 运行排课算法
            var result = schedulingEngine.GenerateSchedule(testProblem);

            // 输出结果状态
            Console.WriteLine($"排课结果状态: {result.Status}");
            Console.WriteLine($"排课结果消息: {result.Message}");
            Console.WriteLine($"解决方案数量: {result.Solutions.Count}");

            // 验证结果
            Assert.True(result.Status == SchedulingStatus.Success,
                      $"排课应该成功，但状态为：{result.Status}，消息：{result.Message}");

            // 获取第一个解决方案
            if (result.Solutions.Count > 0)
            {
                var solution = result.Solutions.First();
                Console.WriteLine($"成功生成排课方案，共{solution.Assignments.Count}个分配");

                // 打印分配详情
                foreach (var assignment in solution.Assignments)
                {
                    Console.WriteLine($"课程:{assignment.SectionCode}, 教师:{assignment.TeacherName}, " +
                                      $"教室:{assignment.ClassroomName}, 时间:周{assignment.DayOfWeek}-{assignment.StartTime}");
                }
            }
            // 断言
            Assert.Equal(SchedulingStatus.Success, result.Status);
            Assert.NotEmpty(result.Solutions);

            Console.WriteLine("============ 超简单测试结束 ============");
        }
        [Fact]
        public void Test_CP_Initial_Solution_Generation()
        {
            // 创建超简单测试数据
            var testProblem = SuperSimpleTestDataProvider.CreateSuperSimpleTestProblem();

            // 获取CP调度器
            var cpScheduler = _serviceProvider.GetRequiredService<CPScheduler>();

            // 生成初始解
            var initialSolutions = cpScheduler.GenerateInitialSolutions(testProblem, 1);

            // 验证结果
            Assert.True(initialSolutions.Count > 0, "应该生成至少一个初始解");

            if (initialSolutions.Count > 0)
            {
                var solution = initialSolutions.First();
                Console.WriteLine($"初始解包含 {solution.Assignments.Count} 个分配");

                foreach (var assignment in solution.Assignments)
                {
                    Console.WriteLine($"课程:{assignment.SectionId}, 教师:{assignment.TeacherId}, " +
                                  $"教室:{assignment.ClassroomId}, 时间:{assignment.TimeSlotId}");
                }
            }
        }
        //// 在测试类中添加
        //[Fact]
        //public void Test_CPScheduler_CheckFeasibility()
        //{
        //    var testProblem = SuperSimpleTestDataProvider.CreateSuperSimpleTestProblem();
        //    var cpScheduler = _serviceProvider.GetRequiredService<CPScheduler>();
        //    Google.OrTools.Sat.CpSolverStatus status;

        //    bool isFeasible = cpScheduler.CheckFeasibility(testProblem, out status);

        //    Console.WriteLine($"可行性检查结果: {isFeasible}, 状态: {status}");
        //    Assert.True(isFeasible, $"问题应该是可行的，但状态为: {status}");
        //}
        //// 在测试类中添加
        //[Fact]
        //public void Test_ConstraintManager_Registration()
        //{
        //    var constraintManager = _serviceProvider.GetRequiredService<ConstraintManager>();

        //    var hardConstraints = constraintManager.GetHardConstraints();
        //    var softConstraints = constraintManager.GetSoftConstraints();

        //    Console.WriteLine($"硬约束数量: {hardConstraints.Count}");
        //    foreach (var constraint in hardConstraints)
        //    {
        //        Console.WriteLine($"- {constraint.Name} (ID: {constraint.Id})");
        //    }

        //    Console.WriteLine($"软约束数量: {softConstraints.Count}");
        //    foreach (var constraint in softConstraints)
        //    {
        //        Console.WriteLine($"- {constraint.Name} (ID: {constraint.Id})");
        //    }

        //    Assert.True(hardConstraints.Count > 0, "应该至少有一个硬约束");
        //}
        //// 在测试中添加这段代码
        //[Fact]
        //public void Test_Constraints_Evaluation()
        //{
        //    var testProblem = SuperSimpleTestDataProvider.CreateSuperSimpleTestProblem();
        //    var constraintManager = _serviceProvider.GetRequiredService<ConstraintManager>();

        //    // 创建一个简单的排课分配
        //    var solution = new SchedulingSolution
        //    {
        //        Id = 1,
        //        ProblemId = testProblem.Id,
        //        Problem = testProblem
        //    };

        //    // 手动添加一个简单分配
        //    solution.Assignments.Add(new SchedulingAssignment
        //    {
        //        Id = 1,
        //        SectionId = 1,
        //        SectionCode = "CS101-A",
        //        TeacherId = 1,
        //        TeacherName = "张教授",
        //        ClassroomId = 1,
        //        ClassroomName = "A101",
        //        TimeSlotId = 1,
        //        DayOfWeek = 1,
        //        StartTime = new TimeSpan(8, 0, 0),
        //        EndTime = new TimeSpan(9, 30, 0)
        //    });

        //    // 测试每个硬约束
        //    var hardConstraints = constraintManager.GetHardConstraints();
        //    foreach (var constraint in hardConstraints)
        //    {
        //        var (score, conflicts) = constraint.Evaluate(solution);
        //        Console.WriteLine($"约束 {constraint.Name} 评分: {score}, 冲突数: {conflicts?.Count ?? 0}");
        //        if (conflicts?.Count > 0)
        //        {
        //            Console.WriteLine($"  第一个冲突: {conflicts[0].Description}");
        //        }
        //        Assert.True(score >= 1.0, $"约束 {constraint.Name} 应该满足，但得分为 {score}");
        //    }
        //}
        //[Fact]
        //public void Test_CPModelBuilder()
        //{
        //    var testProblem = SuperSimpleTestDataProvider.CreateSuperSimpleTestProblem();
        //    var modelBuilder = _serviceProvider.GetRequiredService<CPModelBuilder>();

        //    // 获取必要的依赖项
        //    var converter = _serviceProvider.GetRequiredService<SolutionConverter>();

        //    try
        //    {
        //        // 构建模型
        //        var model = modelBuilder.BuildModel(testProblem);
        //        Assert.NotNull(model);
        //        Console.WriteLine("CP模型构建成功");
        //    }
        //    catch (Exception ex)
        //    {
        //        Assert.True(false, $"CP模型构建失败: {ex.Message}\n{ex.StackTrace}");
        //    }
        //}
        //[Fact]
        //public void Test_CP_Model_Variables()
        //{
        //    var testProblem = SuperSimpleTestDataProvider.CreateSuperSimpleTestProblem();
        //    var modelBuilder = _serviceProvider.GetRequiredService<CPModelBuilder>();

        //    try
        //    {
        //        // 尝试构建模型
        //        var model = modelBuilder.BuildModel(testProblem);

        //        // 这里需要添加逻辑检查模型中的变量数量
        //        // 但由于CpModel不容易直接检查内部变量，可以添加日志输出
        //        Console.WriteLine("模型构建成功");
        //    }
        //    catch (Exception ex)
        //    {
        //        Assert.Fail($"模型构建失败: {ex.Message}");
        //    }
        //}
        //[Fact]
        //public void TestSchedulingAlgorithm_WithSimpleData_ShouldSucceed()
        //{
        //    // 创建简单测试数据
        //    var testProblem = SimpleTestDataProvider.CreateSimpleTestProblem();

        //    // 获取排课引擎
        //    var schedulingEngine = _serviceProvider.GetRequiredService<SchedulingEngine>();

        //    // 运行排课算法
        //    var result = schedulingEngine.GenerateSchedule(testProblem);

        //    // 验证结果
        //    Assert.True(result.Status == SchedulingStatus.Success, $"排课应该成功，但状态为：{result.Status}，消息：{result.Message}");
        //    Assert.True(result.Solutions.Count > 0, "应该生成至少一个解");

        //    // 获取第一个解决方案
        //    var solution = result.Solutions.First();
        //    Console.WriteLine($"成功生成排课方案，共{solution.Assignments.Count}个分配");

        //    // 打印分配详情
        //    foreach (var assignment in solution.Assignments)
        //    {
        //        Console.WriteLine($"课程:{assignment.SectionCode}, 教师:{assignment.TeacherName}, " +
        //                         $"教室:{assignment.ClassroomName}, 时间:周{assignment.DayOfWeek}-{assignment.StartTime}");
        //    }

        //    // 验证是否所有课程都被分配
        //    var scheduledSections = solution.Assignments.Select(a => a.SectionId).Distinct().ToList();
        //    Assert.Equal(testProblem.CourseSections.Count, scheduledSections.Count);

        //    // 验证没有教师冲突
        //    var teacherTimeSlots = solution.Assignments
        //        .Select(a => (a.TeacherId, a.TimeSlotId))
        //        .ToList();
        //    Assert.Equal(teacherTimeSlots.Count, teacherTimeSlots.Distinct().Count());

        //    // 验证没有教室冲突
        //    var roomTimeSlots = solution.Assignments
        //        .Select(a => (a.ClassroomId, a.TimeSlotId))
        //        .ToList();
        //    Assert.Equal(roomTimeSlots.Count, roomTimeSlots.Distinct().Count());
        //}
    }
}
