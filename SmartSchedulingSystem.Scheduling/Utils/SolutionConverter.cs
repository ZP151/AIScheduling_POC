using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Utils
{
    /// <summary>
    /// 用于在CP解和SchedulingSolution之间转换的工具类
    /// </summary>
    public class SolutionConverter
    {
        /// <summary>
        /// 将CP解转换为排课系统的SchedulingSolution
        /// </summary>
        /// <param name="cpSolution">CP求解器返回的解</param>
        /// <param name="problem">排课问题</param>
        /// <returns>排课系统解决方案</returns>
        public SchedulingSolution ConvertToSchedulingSolution(Dictionary<string, long> cpSolution, SchedulingProblem problem)
        {
            Console.WriteLine($"开始转换CP解，解大小: {cpSolution.Count}");

            var solution = new SchedulingSolution
            {
                ProblemId = problem.Id,
                CreatedAt = DateTime.Now,
                Assignments = new List<SchedulingAssignment>()
            };

            // 筛选出表示分配的变量(值为1)
            var assignmentVariables = cpSolution
                .Where(kv => kv.Key.Contains("_") && kv.Value == 1)
                .Select(kv => kv.Key)
                .ToList();
            Console.WriteLine($"找到 {assignmentVariables.Count} 个值为1的决策变量");

            int assignmentId = 1;
            foreach (var varName in assignmentVariables)
            {
                // 解析变量名(例如: "c1_t2_r3_f4" 表示课程1在时间段2使用教室3由教师4教授)
                var parts = varName.Split('_');
                if (parts.Length < 4)
                {
                    continue; // 跳过格式不正确的变量名
                }

                // 提取ID部分
                int sectionId = ExtractId(parts[0], 'c');
                int timeSlotId = ExtractId(parts[1], 't');
                int roomId = ExtractId(parts[2], 'r');
                int teacherId = ExtractId(parts[3], 'f');

                // 创建课程分配
                var assignment = new SchedulingAssignment
                {
                    Id = assignmentId++,
                    SectionId = sectionId,
                    TimeSlotId = timeSlotId,
                    ClassroomId = roomId,
                    TeacherId = teacherId
                };

                solution.Assignments.Add(assignment);
            }
            Console.WriteLine($"转换完成，创建了 {solution.Assignments.Count} 个分配");

            return solution;
        }

        /// <summary>
        /// 将排课系统解转换为CP模型的变量赋值
        /// </summary>
        /// <param name="solution">排课系统解</param>
        /// <returns>变量赋值字典</returns>
        public Dictionary<string, long> ConvertToCpSolution(SchedulingSolution solution)
        {
            var cpSolution = new Dictionary<string, long>();

            // 为每个分配创建对应的变量赋值
            foreach (var assignment in solution.Assignments)
            {
                string varName = $"c{assignment.SectionId}_t{assignment.TimeSlotId}_r{assignment.ClassroomId}_f{assignment.TeacherId}";
                cpSolution[varName] = 1;
            }

            return cpSolution;
        }

        /// <summary>
        /// 从带前缀的标识符中提取ID
        /// </summary>
        /// <param name="idString">带前缀的ID字符串(例如: "c1")</param>
        /// <param name="prefix">前缀字符</param>
        /// <returns>提取的ID</returns>
        private int ExtractId(string idString, char prefix)
        {
            if (string.IsNullOrEmpty(idString) || idString[0] != prefix)
            {
                throw new ArgumentException($"无效的ID格式: {idString}");
            }

            string numberPart = idString.Substring(1);
            if (int.TryParse(numberPart, out int id))
            {
                return id;
            }

            throw new ArgumentException($"无法从字符串解析ID: {idString}");
        }
    }
}