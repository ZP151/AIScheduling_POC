using SmartSchedulingSystem.Scheduling.Engine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// 管理多个排课方案的集合
    /// </summary>
    public class SchedulingSolutionSet
    {
        /// <summary>
        /// 集合的唯一ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 所属排课问题的ID
        /// </summary>
        public int ProblemId { get; set; }

        /// <summary>
        /// 方案集合名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 包含的解决方案列表
        /// </summary>
        public List<SchedulingSolution> Solutions { get; set; } = new List<SchedulingSolution>();

        /// <summary>
        /// 主方案的ID
        /// </summary>
        public int PrimarySolutionId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 获取主方案
        /// </summary>
        public SchedulingSolution PrimarySolution => Solutions.FirstOrDefault(s => s.Id == PrimarySolutionId);
        public double AverageScore { get; set; }
        public double HighestScore { get; set; }
        public double LowestScore { get; set; }
        public double Diversity { get; set; }
        /// <summary>
        /// 添加解决方案到集合
        /// </summary>
        public void AddSolution(SchedulingSolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            Solutions.Add(solution);

            // 如果是第一个添加的方案，则设为主方案
            if (Solutions.Count == 1)
            {
                PrimarySolutionId = solution.Id;
            }
        }

        /// <summary>
        /// 设置主方案
        /// </summary>
        public void SetPrimarySolution(int solutionId)
        {
            if (Solutions.Any(s => s.Id == solutionId))
            {
                PrimarySolutionId = solutionId;
            }
            else
            {
                throw new ArgumentException($"Solution with ID {solutionId} not found in this set");
            }
        }

        /// <summary>
        /// 比较两个解决方案的差异
        /// </summary>
        public SolutionComparisonResult CompareSolutions(int solutionId1, int solutionId2)
        {
            var solution1 = Solutions.FirstOrDefault(s => s.Id == solutionId1);
            var solution2 = Solutions.FirstOrDefault(s => s.Id == solutionId2);

            if (solution1 == null || solution2 == null)
                throw new ArgumentException("One or both solutions not found in this set");

            return CompareSolutions(solution1, solution2);
        }

        /// <summary>
        /// 比较两个解决方案的差异
        /// </summary>
        public SolutionComparisonResult CompareSolutions(SchedulingSolution solution1, SchedulingSolution solution2)
        {
            var result = new SolutionComparisonResult
            {
                FirstSolutionId = solution1.Id,
                SecondSolutionId = solution2.Id
            };

            // 找出两个方案中的所有课程
            var allSectionIds = solution1.Assignments.Select(a => a.SectionId)
                .Union(solution2.Assignments.Select(a => a.SectionId))
                .Distinct()
                .ToList();

            // 比较每个课程的分配情况
            foreach (var sectionId in allSectionIds)
            {
                var section1 = solution1.Assignments.FirstOrDefault(a => a.SectionId == sectionId);
                var section2 = solution2.Assignments.FirstOrDefault(a => a.SectionId == sectionId);

                if (section1 != null && section2 != null)
                {
                    // 两个方案都包含此课程，检查差异
                    if (section1.TeacherId != section2.TeacherId ||
                        section1.ClassroomId != section2.ClassroomId ||
                        section1.TimeSlotId != section2.TimeSlotId)
                    {
                        result.DifferentAssignments.Add(new AssignmentDifference
                        {
                            SectionId = sectionId,
                            FirstAssignment = section1,
                            SecondAssignment = section2
                        });
                    }
                    else
                    {
                        result.IdenticalAssignments.Add(sectionId);
                    }
                }
                else if (section1 != null)
                {
                    // 仅在第一个方案中存在
                    result.FirstOnlyAssignments.Add(section1);
                }
                else if (section2 != null)
                {
                    // 仅在第二个方案中存在
                    result.SecondOnlyAssignments.Add(section2);
                }
            }

            // 计算差异度
            result.DifferencePercentage = (double)result.DifferentAssignments.Count / allSectionIds.Count * 100;

            return result;
        }

        /// <summary>
        /// 计算解决方案集的多样性
        /// </summary>
        public double CalculateDiversity()
        {
            if (Solutions.Count <= 1)
                return 0;

            double totalDifference = 0;
            int comparisons = 0;

            for (int i = 0; i < Solutions.Count; i++)
            {
                for (int j = i + 1; j < Solutions.Count; j++)
                {
                    var comparison = CompareSolutions(Solutions[i], Solutions[j]);
                    totalDifference += comparison.DifferencePercentage;
                    comparisons++;
                }
            }

            return comparisons > 0 ? totalDifference / comparisons : 0;
        }
        // 并添加一个计算方法:
        public void CalculateMetrics(ISolutionEvaluator evaluator)
        {
            if (Solutions.Count == 0)
                return;

            HighestScore = double.MinValue;
            LowestScore = double.MaxValue;
            double totalScore = 0;

            foreach (var solution in Solutions)
            {
                var evaluation = evaluator.Evaluate(solution);
                double score = evaluation.Score;

                HighestScore = Math.Max(HighestScore, score);
                LowestScore = Math.Min(LowestScore, score);
                totalScore += score;
            }

            AverageScore = totalScore / Solutions.Count;
            Diversity = CalculateDiversity();
        }
    }

    /// <summary>
    /// 解决方案比较结果
    /// </summary>
    public class SolutionComparisonResult
    {
        /// <summary>
        /// 第一个解决方案ID
        /// </summary>
        public int FirstSolutionId { get; set; }

        /// <summary>
        /// 第二个解决方案ID
        /// </summary>
        public int SecondSolutionId { get; set; }

        /// <summary>
        /// 两个方案相同的分配
        /// </summary>
        public List<int> IdenticalAssignments { get; set; } = new List<int>();

        /// <summary>
        /// 两个方案不同的分配
        /// </summary>
        public List<AssignmentDifference> DifferentAssignments { get; set; } = new List<AssignmentDifference>();

        /// <summary>
        /// 仅在第一个方案中存在的分配
        /// </summary>
        public List<SchedulingAssignment> FirstOnlyAssignments { get; set; } = new List<SchedulingAssignment>();

        /// <summary>
        /// 仅在第二个方案中存在的分配
        /// </summary>
        public List<SchedulingAssignment> SecondOnlyAssignments { get; set; } = new List<SchedulingAssignment>();

        /// <summary>
        /// 差异百分比
        /// </summary>
        public double DifferencePercentage { get; set; }
    }

    /// <summary>
    /// 课程分配的差异
    /// </summary>
    public class AssignmentDifference
    {
        /// <summary>
        /// 课程ID
        /// </summary>
        public int SectionId { get; set; }

        /// <summary>
        /// 第一个方案的分配
        /// </summary>
        public SchedulingAssignment FirstAssignment { get; set; }

        /// <summary>
        /// 第二个方案的分配
        /// </summary>
        public SchedulingAssignment SecondAssignment { get; set; }

        /// <summary>
        /// 获取详细的差异描述
        /// </summary>
       

    }
}