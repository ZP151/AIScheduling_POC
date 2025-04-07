using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartSchedulingSystem.Test
{
    /// <summary>
    /// 排课结果可视化工具
    /// </summary>
    public class ScheduleVisualizer
    {
        private readonly ILogger _logger;

        public ScheduleVisualizer(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 生成按教师视图的排课表
        /// </summary>
        public void GenerateTeacherView(SchedulingSolution solution)
        {
            _logger.LogInformation("### 教师视图 ###");

            var teacherGroups = solution.Assignments
                .GroupBy(a => a.TeacherId)
                .OrderBy(g => g.Key);

            foreach (var group in teacherGroups)
            {
                var teacherName = group.First().TeacherName;
                _logger.LogInformation($"教师: {teacherName} (ID: {group.Key})");

                // 按天和时间排序
                var sortedAssignments = group
                    .OrderBy(a => a.DayOfWeek)
                    .ThenBy(a => a.StartTime)
                    .ToList();

                _logger.LogInformation("课程安排:");
                foreach (var assignment in sortedAssignments)
                {
                    _logger.LogInformation(
                        $"  周{assignment.DayOfWeek} {assignment.StartTime}-{assignment.EndTime}, " +
                        $"课程: {assignment.SectionCode}, 教室: {assignment.ClassroomName}");
                }

                _logger.LogInformation(new string('-', 50));
            }
        }

        /// <summary>
        /// 生成按教室视图的排课表
        /// </summary>
        public void GenerateClassroomView(SchedulingSolution solution)
        {
            _logger.LogInformation("### 教室视图 ###");

            var classroomGroups = solution.Assignments
                .GroupBy(a => a.ClassroomId)
                .OrderBy(g => g.Key);

            foreach (var group in classroomGroups)
            {
                var classroomName = group.First().ClassroomName;
                _logger.LogInformation($"教室: {classroomName} (ID: {group.Key})");

                // 按天和时间排序
                var sortedAssignments = group
                    .OrderBy(a => a.DayOfWeek)
                    .ThenBy(a => a.StartTime)
                    .ToList();

                _logger.LogInformation("课程安排:");
                foreach (var assignment in sortedAssignments)
                {
                    _logger.LogInformation(
                        $"  周{assignment.DayOfWeek} {assignment.StartTime}-{assignment.EndTime}, " +
                        $"课程: {assignment.SectionCode}, 教师: {assignment.TeacherName}");
                }

                _logger.LogInformation(new string('-', 50));
            }
        }

        /// <summary>
        /// 生成按时间表格视图的排课表
        /// </summary>
        public void GenerateTimeTableView(SchedulingSolution solution, List<TimeSlotInfo> timeSlots)
        {
            _logger.LogInformation("### 时间表格视图 ###");

            // 获取所有天和时间段
            var days = timeSlots.Select(ts => ts.DayOfWeek).Distinct().OrderBy(d => d).ToList();
            var timesByDay = timeSlots
                .GroupBy(ts => ts.DayOfWeek)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(ts => ts.StartTime).ToList());

            // 为每天创建表头
            foreach (var day in days)
            {
                var dayName = GetDayName(day);
                _logger.LogInformation($"--- {dayName} ---");

                var timeSlotsForDay = timesByDay[day];
                var headerBuilder = new StringBuilder("时间段 | ");

                foreach (var timeSlot in timeSlotsForDay)
                {
                    headerBuilder.Append($"{timeSlot.StartTime.ToString(@"hh\:mm")}-{timeSlot.EndTime.ToString(@"hh\:mm")} | ");
                }

                _logger.LogInformation(headerBuilder.ToString());
                _logger.LogInformation(new string('-', headerBuilder.Length));

                // 获取当天的所有教室
                var classroomsUsedOnDay = solution.Assignments
                    .Where(a => a.DayOfWeek == day)
                    .Select(a => a.ClassroomId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                foreach (var classroomId in classroomsUsedOnDay)
                {
                    var classroomName = solution.Assignments
                        .First(a => a.ClassroomId == classroomId)
                        .ClassroomName;

                    var rowBuilder = new StringBuilder($"{classroomName} | ");

                    foreach (var timeSlot in timeSlotsForDay)
                    {
                        var assignment = solution.Assignments
                            .FirstOrDefault(a => a.DayOfWeek == day && a.TimeSlotId == timeSlot.Id && a.ClassroomId == classroomId);

                        if (assignment != null)
                        {
                            rowBuilder.Append($"{assignment.SectionCode}\n{assignment.TeacherName} | ");
                        }
                        else
                        {
                            rowBuilder.Append("空闲 | ");
                        }
                    }

                    _logger.LogInformation(rowBuilder.ToString());
                }

                _logger.LogInformation(new string('=', 80));
            }
        }

        /// <summary>
        /// 生成冲突报告
        /// </summary>
        public void GenerateConflictReport(List<SchedulingConflict> conflicts)
        {
            if (conflicts == null || conflicts.Count == 0)
            {
                _logger.LogInformation("未检测到冲突");
                return;
            }

            _logger.LogInformation("### 冲突报告 ###");
            _logger.LogInformation($"检测到 {conflicts.Count} 个冲突");

            // 按冲突类型分组
            var conflictGroups = conflicts
                .GroupBy(c => c.Type)
                .OrderBy(g => g.Key);

            foreach (var group in conflictGroups)
            {
                _logger.LogInformation($"类型: {group.Key}，数量: {group.Count()}");

                // 显示每种类型的前3个冲突
                foreach (var conflict in group.Take(3))
                {
                    _logger.LogInformation($"  描述: {conflict.Description}");
                    _logger.LogInformation($"  严重程度: {conflict.Severity}");
                    _logger.LogInformation(new string('-', 40));
                }

                if (group.Count() > 3)
                {
                    _logger.LogInformation($"  ... 还有 {group.Count() - 3} 个冲突未显示");
                }

                _logger.LogInformation(new string('=', 80));
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