using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Test
{
    /// <summary>
    /// 简化版解决方案评估器，用于测试环境
    /// </summary>
    public class SimpleEvaluator
    {
        private readonly ILogger _logger;

        public SimpleEvaluator(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 评估解决方案
        /// </summary>
        public double EvaluateSolution(SchedulingSolution solution)
        {
            if (solution == null || solution.Assignments.Count == 0)
            {
                return 0;
            }

            // 计算总分 = 资源利用率 * 0.4 + 分配完整率 * 0.4 + 平衡性 * 0.2
            double resourceUtilizationScore = CalculateResourceUtilizationScore(solution);
            double assignmentCompletionScore = CalculateAssignmentCompletionScore(solution);
            double balanceScore = CalculateBalanceScore(solution);

            double totalScore =
                (resourceUtilizationScore * 0.4) +
                (assignmentCompletionScore * 0.4) +
                (balanceScore * 0.2);

            return totalScore;
        }

        /// <summary>
        /// 计算资源利用率得分
        /// </summary>
        private double CalculateResourceUtilizationScore(SchedulingSolution solution)
        {
            if (solution.Problem == null)
            {
                return 0;
            }

            // 教室利用率
            int totalClassrooms = solution.Problem.Classrooms.Count;
            int usedClassrooms = solution.Assignments.Select(a => a.ClassroomId).Distinct().Count();
            double classroomUtilizationRate = totalClassrooms > 0
                ? (double)usedClassrooms / totalClassrooms
                : 0;

            // 时间槽利用率 (每个时间槽平均使用的教室数)
            var timeSlotUtilization = solution.Assignments
                .GroupBy(a => a.TimeSlotId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count() / (double)totalClassrooms);

            double averageTimeSlotUtilizationRate = timeSlotUtilization.Count > 0
                ? timeSlotUtilization.Values.Average()
                : 0;

            // 综合资源利用率得分
            return (classroomUtilizationRate + averageTimeSlotUtilizationRate) / 2;
        }

        /// <summary>
        /// 计算分配完整率得分
        /// </summary>
        private double CalculateAssignmentCompletionScore(SchedulingSolution solution)
        {
            if (solution.Problem == null)
            {
                return 0;
            }

            // 课程分配完整率
            int totalSections = solution.Problem.CourseSections.Count;
            int assignedSections = solution.Assignments.Select(a => a.SectionId).Distinct().Count();
            double sectionAssignmentRate = totalSections > 0
                ? (double)assignedSections / totalSections
                : 0;

            return sectionAssignmentRate;
        }

        /// <summary>
        /// 计算平衡性得分
        /// </summary>
        private double CalculateBalanceScore(SchedulingSolution solution)
        {
            // 教师工作量平衡性
            var teacherWorkloads = solution.Assignments
                .GroupBy(a => a.TeacherId)
                .Select(g => g.Count())
                .ToList();

            double teacherWorkloadBalance = 0;
            if (teacherWorkloads.Count > 1)
            {
                double avg = teacherWorkloads.Average();
                double stdDev = Math.Sqrt(teacherWorkloads.Sum(x => Math.Pow(x - avg, 2)) / teacherWorkloads.Count);
                double cv = stdDev / avg; // 变异系数
                teacherWorkloadBalance = Math.Max(0, 1 - cv); // 转换为0-1分数
            }
            else if (teacherWorkloads.Count == 1)
            {
                teacherWorkloadBalance = 1; // 只有一个教师时，认为是完全平衡的
            }

            // 每天课程分布平衡性
            var dailyAssignments = solution.Assignments
                .GroupBy(a => a.DayOfWeek)
                .Select(g => g.Count())
                .ToList();

            double dailyAssignmentBalance = 0;
            if (dailyAssignments.Count > 1)
            {
                double avg = dailyAssignments.Average();
                double stdDev = Math.Sqrt(dailyAssignments.Sum(x => Math.Pow(x - avg, 2)) / dailyAssignments.Count);
                double cv = stdDev / avg; // 变异系数
                dailyAssignmentBalance = Math.Max(0, 1 - cv); // 转换为0-1分数
            }
            else if (dailyAssignments.Count == 1)
            {
                dailyAssignmentBalance = 0; // 只有一天有课，认为是不平衡的
            }

            // 综合平衡性得分
            return (teacherWorkloadBalance + dailyAssignmentBalance) / 2;
        }

        /// <summary>
        /// 检查硬约束冲突
        /// </summary>
        public List<SchedulingConflict> CheckHardConstraints(SchedulingSolution solution)
        {
            var conflicts = new List<SchedulingConflict>();

            // 检查教师冲突
            CheckTeacherConflicts(solution, conflicts);

            // 检查教室冲突
            CheckClassroomConflicts(solution, conflicts);

            // 检查教室容量约束
            if (solution.Problem != null)
            {
                CheckClassroomCapacityConstraints(solution, conflicts);
            }

            return conflicts;
        }

        /// <summary>
        /// 检查教师冲突
        /// </summary>
        private void CheckTeacherConflicts(SchedulingSolution solution, List<SchedulingConflict> conflicts)
        {
            // 按教师和时间槽分组
            var groups = solution.Assignments
                .GroupBy(a => new { a.TeacherId, a.TimeSlotId })
                .Where(g => g.Count() > 1);

            foreach (var group in groups)
            {
                var assignments = group.ToList();
                var teacher = assignments.First().TeacherName;
                var timeSlotId = group.Key.TimeSlotId;

                conflicts.Add(new SchedulingConflict
                {
                    Type = SchedulingConflictType.TeacherConflict,
                    Description = $"教师 {teacher} 在同一时间段有多门课程安排",
                    Severity = ConflictSeverity.Critical,
                    InvolvedEntities = new Dictionary<string, List<int>>
                    {
                        { "Teachers", new List<int> { group.Key.TeacherId } },
                        { "Sections", assignments.Select(a => a.SectionId).ToList() }
                    },
                    InvolvedTimeSlots = new List<int> { timeSlotId }
                });
            }
        }

        /// <summary>
        /// 检查教室冲突
        /// </summary>
        private void CheckClassroomConflicts(SchedulingSolution solution, List<SchedulingConflict> conflicts)
        {
            // 按教室和时间槽分组
            var groups = solution.Assignments
                .GroupBy(a => new { a.ClassroomId, a.TimeSlotId })
                .Where(g => g.Count() > 1);

            foreach (var group in groups)
            {
                var assignments = group.ToList();
                var classroom = assignments.First().ClassroomName;
                var timeSlotId = group.Key.TimeSlotId;

                conflicts.Add(new SchedulingConflict
                {
                    Type = SchedulingConflictType.ClassroomConflict,
                    Description = $"教室 {classroom} 在同一时间段有多门课程安排",
                    Severity = ConflictSeverity.Critical,
                    InvolvedEntities = new Dictionary<string, List<int>>
                    {
                        { "Classrooms", new List<int> { group.Key.ClassroomId } },
                        { "Sections", assignments.Select(a => a.SectionId).ToList() }
                    },
                    InvolvedTimeSlots = new List<int> { timeSlotId }
                });
            }
        }

        /// <summary>
        /// 检查教室容量约束
        /// </summary>
        private void CheckClassroomCapacityConstraints(SchedulingSolution solution, List<SchedulingConflict> conflicts)
        {
            foreach (var assignment in solution.Assignments)
            {
                var section = solution.Problem.CourseSections.FirstOrDefault(s => s.Id == assignment.SectionId);
                var classroom = solution.Problem.Classrooms.FirstOrDefault(c => c.Id == assignment.ClassroomId);

                if (section != null && classroom != null && section.Enrollment > classroom.Capacity)
                {
                    conflicts.Add(new SchedulingConflict
                    {
                        Type = SchedulingConflictType.ClassroomCapacityExceeded,
                        Description = $"教室 {classroom.Name} 容量 ({classroom.Capacity}) 不足以容纳课程 {section.SectionCode} 的学生数 ({section.Enrollment})",
                        Severity = ConflictSeverity.Severe,
                        InvolvedEntities = new Dictionary<string, List<int>>
                        {
                            { "Classrooms", new List<int> { classroom.Id } },
                            { "Sections", new List<int> { section.Id } }
                        }
                    });
                }
            }
        }
    }
}