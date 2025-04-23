using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 将CP求解器的解转换为排课系统的解
    /// </summary>
    public class SolutionConverter
    {
        /// <summary>
        /// 将CP求解器的解转换为排课系统的解 (别名方法)
        /// </summary>
        public SchedulingSolution ConvertToDomainSolution(SchedulingProblem problem, Dictionary<string, long> cpSolution)
        {
            // 调用原来的方法
            return ConvertToSchedulingSolution(cpSolution, problem);
        }
        
        /// <summary>
        /// 将CP求解器的解转换为排课系统的解
        /// </summary>
        public SchedulingSolution ConvertToSchedulingSolution(Dictionary<string, long> cpSolution, SchedulingProblem problem)
        {
            var solution = new SchedulingSolution
            {
                Problem = problem,
                Algorithm = "CP",
                GeneratedAt = DateTime.Now
            };

            // 解析变量名称并创建分配
            var assignments = new List<SchedulingAssignment>();
            int assignmentId = 1;

            foreach (var entry in cpSolution)
            {
                // 只处理值为1的变量（表示该分配被选中）
                if (entry.Value != 1)
                    continue;

                string varName = entry.Key;
                
                // 解析变量名格式： c{sectionId}_t{timeSlotId}_r{classroomId}_f{teacherId}
                var parts = varName.Split('_');
                if (parts.Length != 4)
                    continue;
                
                // 提取ID
                int sectionId = int.Parse(parts[0].Substring(1));
                int timeSlotId = int.Parse(parts[1].Substring(1));
                int classroomId = int.Parse(parts[2].Substring(1));
                int teacherId = int.Parse(parts[3].Substring(1));

                // 查找相关信息
                var timeSlot = problem.TimeSlots.FirstOrDefault(t => t.Id == timeSlotId);
                if (timeSlot == null)
                    continue;
                
                var classroom = problem.Classrooms.FirstOrDefault(r => r.Id == classroomId);
                if (classroom == null)
                    continue;

                // 创建分配
                var assignment = new SchedulingAssignment
                {
                    Id = assignmentId++,
                    SectionId = sectionId,
                    TimeSlotId = timeSlotId,
                    ClassroomId = classroomId,
                    TeacherId = teacherId,
                    DayOfWeek = timeSlot.DayOfWeek,
                    StartTime = timeSlot.StartTime,
                    EndTime = timeSlot.EndTime,
                    Building = classroom.Building,
                    ClassroomName = classroom.Name
                };

                assignments.Add(assignment);
            }

            solution.Assignments = assignments;
            return solution;
        }
    }
} 