// 在Test项目中添加测试类
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SmartSchedulingSystem.Test.Scheduling
{
    public class SchedulingEngineTests
    {
        private readonly ITestOutputHelper _output;

        public SchedulingEngineTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestSchedulingEngine_GeneratesValidSchedule()
        {
            // 设置服务
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSchedulingServices(); // 使用DependencyInjection中的扩展方法

            var serviceProvider = services.BuildServiceProvider();

            // 获取日志工厂以将日志输出到测试窗口
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));

            // 获取调度引擎
            var schedulingEngine = serviceProvider.GetRequiredService<SchedulingEngine>();

            // 生成测试数据
            var testDataGenerator = new TestDataGenerator();
            var problem = testDataGenerator.GenerateTestProblem(
                courseSectionCount: 10,
                teacherCount: 5,
                classroomCount: 8,
                timeSlotCount: 15);

            // 记录问题规模
            _output.WriteLine($"问题规模: 课程数={problem.CourseSections.Count}, " +
                             $"教师数={problem.Teachers.Count}, " +
                             $"教室数={problem.Classrooms.Count}, " +
                             $"时间槽数={problem.TimeSlots.Count}");

            // 生成排课方案
            var result = schedulingEngine.GenerateSchedule(problem);

            // 验证结果
            Assert.NotNull(result);
            Assert.Equal(SchedulingStatus.Success, result.Status);
            Assert.NotEmpty(result.Solutions);

            // 验证最佳解是否合法
            var bestSolution = result.Solutions.First();

            // 检查是否所有课程都被分配
            var scheduledSections = bestSolution.Assignments
                .Select(a => a.SectionId)
                .Distinct()
                .Count();

            _output.WriteLine($"排课结果: 已安排课程数={scheduledSections}/" +
                             $"{problem.CourseSections.Count}");

            Assert.Equal(problem.CourseSections.Count, scheduledSections);

            // 检查教师冲突
            var teacherConflicts = CheckTeacherConflicts(bestSolution);
            _output.WriteLine($"教师冲突数: {teacherConflicts.Count}");
            Assert.Empty(teacherConflicts);

            // 检查教室冲突
            var roomConflicts = CheckRoomConflicts(bestSolution);
            _output.WriteLine($"教室冲突数: {roomConflicts.Count}");
            Assert.Empty(roomConflicts);

            // 检查教师可用性
            var teacherAvailabilityConflicts = CheckTeacherAvailability(bestSolution);
            _output.WriteLine($"教师可用性冲突数: {teacherAvailabilityConflicts.Count}");
            Assert.Empty(teacherAvailabilityConflicts);

            // 检查教室可用性
            var roomAvailabilityConflicts = CheckRoomAvailability(bestSolution);
            _output.WriteLine($"教室可用性冲突数: {roomAvailabilityConflicts.Count}");
            Assert.Empty(roomAvailabilityConflicts);

            // 输出排课统计信息
            if (result.Statistics != null)
            {
                _output.WriteLine("排课统计信息:");
                _output.WriteLine($"  教室利用率: {result.Statistics.AverageClassroomUtilization:P2}");
                _output.WriteLine($"  时间槽利用率: {result.Statistics.AverageTimeSlotUtilization:P2}");
                _output.WriteLine($"  教师工作量平衡度: {result.Statistics.TeacherWorkloadStdDev:F2}");
            }
        }

        private List<(int TeacherId, int TimeSlotId)> CheckTeacherConflicts(SchedulingSolution solution)
        {
            var conflicts = new List<(int TeacherId, int TimeSlotId)>();

            var groupedByTeacherAndTime = solution.Assignments
                .GroupBy(a => new { a.TeacherId, a.TimeSlotId })
                .Where(g => g.Count() > 1);

            foreach (var group in groupedByTeacherAndTime)
            {
                conflicts.Add((group.Key.TeacherId, group.Key.TimeSlotId));
            }

            return conflicts;
        }

        private List<(int ClassroomId, int TimeSlotId)> CheckRoomConflicts(SchedulingSolution solution)
        {
            var conflicts = new List<(int ClassroomId, int TimeSlotId)>();

            var groupedByRoomAndTime = solution.Assignments
                .GroupBy(a => new { a.ClassroomId, a.TimeSlotId })
                .Where(g => g.Count() > 1);

            foreach (var group in groupedByRoomAndTime)
            {
                conflicts.Add((group.Key.ClassroomId, group.Key.TimeSlotId));
            }

            return conflicts;
        }

        private List<(int TeacherId, int TimeSlotId)> CheckTeacherAvailability(SchedulingSolution solution)
        {
            var conflicts = new List<(int TeacherId, int TimeSlotId)>();

            if (solution.Problem == null)
                return conflicts;

            foreach (var assignment in solution.Assignments)
            {
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId &&
                                       ta.TimeSlotId == assignment.TimeSlotId);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                {
                    conflicts.Add((assignment.TeacherId, assignment.TimeSlotId));
                }
            }

            return conflicts;
        }

        private List<(int ClassroomId, int TimeSlotId)> CheckRoomAvailability(SchedulingSolution solution)
        {
            var conflicts = new List<(int ClassroomId, int TimeSlotId)>();

            if (solution.Problem == null)
                return conflicts;

            foreach (var assignment in solution.Assignments)
            {
                var roomAvailability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ra => ra.ClassroomId == assignment.ClassroomId &&
                                       ra.TimeSlotId == assignment.TimeSlotId);

                if (roomAvailability != null && !roomAvailability.IsAvailable)
                {
                    conflicts.Add((assignment.ClassroomId, assignment.TimeSlotId));
                }
            }

            return conflicts;
        }
    }

    // 用于将测试日志输出到测试窗口的自定义日志提供程序
    internal class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;

        public XunitLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XunitLogger(_output, categoryName);
        }

        public void Dispose() { }
    }

    internal class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly string _categoryName;

        public XunitLogger(ITestOutputHelper output, string categoryName)
        {
            _output = output;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                _output.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{logLevel}] {_categoryName}: {formatter(state, exception)}");

                if (exception != null)
                {
                    _output.WriteLine($"Exception: {exception}");
                }
            }
            catch
            {
                // 忽略可能出现的输出异常
            }
        }
    }
}