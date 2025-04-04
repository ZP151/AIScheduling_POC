using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Utils
{
    /// <summary>
    /// 用于生成和评估解多样性的工具类
    /// </summary>
    public class SolutionDiversifier
    {
        private readonly Random _random = new Random();

        /// <summary>
        /// 筛选多样化的解集
        /// </summary>
        /// <param name="solutions">候选解列表</param>
        /// <param name="count">需要的解数量</param>
        /// <param name="evaluator">解评估器</param>
        /// <returns>多样化的解集</returns>
        public List<SchedulingSolution> SelectDiverseSet(
            List<SchedulingSolution> solutions,
            int count,
            SolutionEvaluator evaluator)
        {
            if (solutions.Count <= count)
            {
                return solutions.ToList();
            }

            var diverseSet = new List<SchedulingSolution>();

            // 首先添加评分最高的解
            var remainingSolutions = solutions.ToList();
            remainingSolutions = remainingSolutions.OrderByDescending(s => evaluator.Evaluate(s)).ToList();

            var bestSolution = remainingSolutions.First();
            diverseSet.Add(bestSolution);
            remainingSolutions.Remove(bestSolution);

            // 然后添加与现有解差异最大的解
            while (diverseSet.Count < count && remainingSolutions.Count > 0)
            {
                // 计算每个剩余解与已选解的最小差异度
                var solutionDistances = remainingSolutions.Select(solution =>
                {
                    double minDistance = diverseSet.Min(s => CalculateDistance(s, solution));
                    return new { Solution = solution, MinDistance = minDistance };
                }).ToList();

                // 选择差异最大的解
                var mostDiverseSolution = solutionDistances.OrderByDescending(x => x.MinDistance).First().Solution;
                diverseSet.Add(mostDiverseSolution);
                remainingSolutions.Remove(mostDiverseSolution);
            }

            return diverseSet;
        }

        /// <summary>
        /// 计算两个解之间的差异度(0-1)
        /// </summary>
        public double CalculateDistance(SchedulingSolution solution1, SchedulingSolution solution2)
        {
            if (solution1 == null || solution2 == null)
            {
                throw new ArgumentNullException("解不能为空");
            }

            // 比较两个解的课程分配
            int differentAssignments = 0;
            int totalAssignments = Math.Max(solution1.Assignments.Count, solution2.Assignments.Count);

            // 创建第一个解的课程分配映射(课程ID -> 分配)
            var solution1Map = solution1.Assignments.ToDictionary(a => a.SectionId);

            // 比较第二个解的每个分配与第一个解的差异
            foreach (var assignment2 in solution2.Assignments)
            {
                if (solution1Map.TryGetValue(assignment2.SectionId, out var assignment1))
                {
                    // 检查时间、教室、教师是否相同
                    if (assignment1.TimeSlotId != assignment2.TimeSlotId ||
                        assignment1.ClassroomId != assignment2.ClassroomId ||
                        assignment1.TeacherId != assignment2.TeacherId)
                    {
                        differentAssignments++;
                    }
                }
                else
                {
                    // 第一个解中没有对应的课程分配
                    differentAssignments++;
                }
            }

            // 加上第一个解中有但第二个解中没有的分配
            var solution2SectionIds = solution2.Assignments.Select(a => a.SectionId).ToHashSet();
            differentAssignments += solution1.Assignments.Count(a => !solution2SectionIds.Contains(a.SectionId));

            // 计算差异比例
            return (double)differentAssignments / totalAssignments;
        }

        /// <summary>
        /// 随机修改解以增加多样性
        /// </summary>
        public SchedulingSolution DiversifySolution(SchedulingSolution solution, double diversityLevel)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            // 创建解的深拷贝
            var newSolution = solution.Clone();

            // 根据多样性级别确定要修改的分配数量
            int assignmentsToModify = (int)Math.Ceiling(newSolution.Assignments.Count * diversityLevel);

            // 随机选择要修改的分配
            var assignmentsToChange = newSolution.Assignments
                .OrderBy(x => _random.Next())
                .Take(assignmentsToModify)
                .ToList();

            // 修改选中的分配
            foreach (var assignment in assignmentsToChange)
            {
                // 随机选择修改类型(时间、教室、教师)
                int modificationType = _random.Next(3);

                switch (modificationType)
                {
                    case 0: // 修改时间槽
                        assignment.TimeSlotId = _random.Next(1, 21); // 假设有20个时间槽
                        break;
                    case 1: // 修改教室
                        assignment.ClassroomId = _random.Next(1, 11); // 假设有10个教室
                        break;
                    case 2: // 修改教师
                        assignment.TeacherId = _random.Next(1, 6); // 假设有5个教师
                        break;
                }
            }

            return newSolution;
        }
    }
}