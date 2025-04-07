using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartSchedulingSystem.Test
{
    /// <summary>
    /// 排课结果导出工具
    /// </summary>
    public class ScheduleResultsExporter
    {
        private readonly ILogger _logger;

        public ScheduleResultsExporter(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 导出排课结果到文本文件
        /// </summary>
        public void ExportToTextFile(SchedulingResult result, string filePath)
        {
            try
            {
                if (result.Solutions.Count == 0)
                {
                    _logger.LogWarning("没有可导出的排课方案");
                    return;
                }

                // 使用最优解进行导出
                var solution = result.Solutions[0];

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // 写入标题
                    writer.WriteLine("智能排课系统 - 排课结果");
                    writer.WriteLine($"生成时间: {DateTime.Now}");
                    writer.WriteLine($"算法状态: {result.Status}");
                    writer.WriteLine($"执行时间: {result.ExecutionTimeMs}ms");
                    writer.WriteLine(new string('=', 80));

                    // 写入统计信息
                    writer.WriteLine("统计信息:");
                    if (result.Statistics != null)
                    {
                        writer.WriteLine($"总班级数: {result.Statistics.TotalSections}, 已安排: {result.Statistics.ScheduledSections}, 未安排: {result.Statistics.UnscheduledSections}");
                        writer.WriteLine($"总教师数: {result.Statistics.TotalTeachers}, 已分配: {result.Statistics.AssignedTeachers}");
                        writer.WriteLine($"总教室数: {result.Statistics.TotalClassrooms}, 已使用: {result.Statistics.UsedClassrooms}");
                        writer.WriteLine($"平均教室利用率: {result.Statistics.AverageClassroomUtilization:P2}");
                        writer.WriteLine($"平均时间槽利用率: {result.Statistics.AverageTimeSlotUtilization:P2}");
                        writer.WriteLine($"教师工作量标准差: {result.Statistics.TeacherWorkloadStdDev:F2}");
                    }
                    writer.WriteLine(new string('-', 80));

                    // 写入冲突信息
                    if (solution.Evaluation != null && solution.Evaluation.Conflicts != null && solution.Evaluation.Conflicts.Count > 0)
                    {
                        writer.WriteLine($"检测到 {solution.Evaluation.Conflicts.Count} 个冲突:");

                        foreach (var conflict in solution.Evaluation.Conflicts.Take(10))
                        {
                            writer.WriteLine($"- 类型: {conflict.Type}, 严重程度: {conflict.Severity}");
                            writer.WriteLine($"  描述: {conflict.Description}");
                        }

                        if (solution.Evaluation.Conflicts.Count > 10)
                        {
                            writer.WriteLine($"  ... 还有 {solution.Evaluation.Conflicts.Count - 10} 个冲突未显示");
                        }

                        writer.WriteLine(new string('-', 80));
                    }

                    // 按教师分组写入课表
                    writer.WriteLine("按教师分组的课表:");
                    var teacherGroups = solution.Assignments
                        .GroupBy(a => a.TeacherId)
                        .OrderBy(g => g.Key);

                    foreach (var group in teacherGroups)
                    {
                        var teacherName = group.First().TeacherName;
                        writer.WriteLine($"教师: {teacherName} (ID: {group.Key})");

                        // 按天和时间排序
                        var sortedAssignments = group
                            .OrderBy(a => a.DayOfWeek)
                            .ThenBy(a => a.StartTime)
                            .ToList();

                        writer.WriteLine("课程安排:");
                        foreach (var assignment in sortedAssignments)
                        {
                            writer.WriteLine(
                                $"  周{assignment.DayOfWeek} {assignment.StartTime}-{assignment.EndTime}, " +
                                $"课程: {assignment.SectionCode}, 教室: {assignment.ClassroomName}");
                        }

                        writer.WriteLine(new string('-', 50));
                    }

                    // 按教室分组写入课表
                    writer.WriteLine("按教室分组的课表:");
                    var classroomGroups = solution.Assignments
                        .GroupBy(a => a.ClassroomId)
                        .OrderBy(g => g.Key);

                    foreach (var group in classroomGroups)
                    {
                        var classroomName = group.First().ClassroomName;
                        writer.WriteLine($"教室: {classroomName} (ID: {group.Key})");

                        // 按天和时间排序
                        var sortedAssignments = group
                            .OrderBy(a => a.DayOfWeek)
                            .ThenBy(a => a.StartTime)
                            .ToList();

                        writer.WriteLine("课程安排:");
                        foreach (var assignment in sortedAssignments)
                        {
                            writer.WriteLine(
                                $"  周{assignment.DayOfWeek} {assignment.StartTime}-{assignment.EndTime}, " +
                                $"课程: {assignment.SectionCode}, 教师: {assignment.TeacherName}");
                        }

                        writer.WriteLine(new string('-', 50));
                    }

                    writer.WriteLine(new string('=', 80));
                    writer.WriteLine("排课结果导出完成");
                }

                _logger.LogInformation($"排课结果已导出到文件: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导出排课结果时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出排课结果到CSV文件
        /// </summary>
        public void ExportToCsv(SchedulingResult result, string filePath)
        {
            try
            {
                if (result.Solutions.Count == 0)
                {
                    _logger.LogWarning("没有可导出的排课方案");
                    return;
                }

                // 使用最优解进行导出
                var solution = result.Solutions[0];

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // 写入CSV表头
                    writer.WriteLine("课程代码,课程名称,教师姓名,教室名称,教学楼,星期几,开始时间,结束时间");

                    // 按时间和教室排序
                    var sortedAssignments = solution.Assignments
                        .OrderBy(a => a.DayOfWeek)
                        .ThenBy(a => a.StartTime)
                        .ThenBy(a => a.ClassroomName)
                        .ToList();

                    // 写入每一行课程安排
                    foreach (var assignment in sortedAssignments)
                    {
                        string courseName = ""; // 课程名称可能需要从其他地方获取

                        if (solution.Problem != null)
                        {
                            var section = solution.Problem.CourseSections
                                .FirstOrDefault(s => s.Id == assignment.SectionId);

                            if (section != null)
                            {
                                courseName = section.CourseName;
                            }
                        }

                        string building = ""; // 教学楼信息可能需要从其他地方获取

                        if (solution.Problem != null)
                        {
                            var classroom = solution.Problem.Classrooms
                                .FirstOrDefault(c => c.Id == assignment.ClassroomId);

                            if (classroom != null)
                            {
                                building = classroom.Building;
                            }
                        }

                        string dayName = GetDayName(assignment.DayOfWeek);

                        writer.WriteLine($"{assignment.SectionCode},{courseName},{assignment.TeacherName}," +
                                      $"{assignment.ClassroomName},{building},{dayName}," +
                                      $"{assignment.StartTime},{assignment.EndTime}");
                    }
                }

                _logger.LogInformation($"排课结果已导出到CSV文件: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导出排课结果到CSV文件时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取星期几的名称
        /// </summary>
        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "周一",
                2 => "周二",
                3 => "周三",
                4 => "周四",
                5 => "周五",
                6 => "周六",
                7 => "周日",
                _ => "未知"
            };
        }
    }
}