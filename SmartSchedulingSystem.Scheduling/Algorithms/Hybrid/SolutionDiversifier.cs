using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.Hybrid
{
    /// <summary>
    /// 用于生成和评估解多样性的工具类
    /// </summary>
    public class SolutionDiversifier
    {
        private readonly Random _random = new Random();
        private readonly ILogger<SolutionDiversifier> _logger;

        public SolutionDiversifier(ILogger<SolutionDiversifier> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
                _logger.LogInformation($"解决方案数量 {solutions.Count} 不超过需要的数量 {count}，无需筛选");
                return solutions.ToList();
            }

            _logger.LogInformation($"开始筛选 {count} 个多样化解决方案，原始解决方案数量: {solutions.Count}");
            var diverseSet = new List<SchedulingSolution>();

            // 首先添加评分最高的解
            var remainingSolutions = solutions.ToList();
            remainingSolutions = remainingSolutions.OrderByDescending(s => evaluator.Evaluate(s)).ToList();

            var bestSolution = remainingSolutions.First();
            diverseSet.Add(bestSolution);
            remainingSolutions.Remove(bestSolution);
            _logger.LogDebug($"已添加最高评分解: #{bestSolution.Id}, 评分: {evaluator.Evaluate(bestSolution).Score:F2}");

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
                double maxDistance = solutionDistances.Max(x => x.MinDistance);
                
                diverseSet.Add(mostDiverseSolution);
                remainingSolutions.Remove(mostDiverseSolution);
                _logger.LogDebug($"添加多样化解 #{mostDiverseSolution.Id}, 与已选解最小差异度: {maxDistance:F2}");
            }

            _logger.LogInformation($"多样化筛选完成，共选择 {diverseSet.Count} 个解决方案");
            return diverseSet;
        }
        
        /// <summary>
        /// 多样化解决方案集合
        /// </summary>
        /// <param name="problem">排课问题</param>
        /// <param name="solutions">原始解决方案列表</param>
        /// <param name="count">需要返回的解决方案数量</param>
        /// <returns>多样化的解决方案集合</returns>
        public IEnumerable<SchedulingSolution> DiversifySolutions(SchedulingProblem problem, List<SchedulingSolution> solutions, int count)
        {
            if (solutions == null || solutions.Count == 0)
            {
                _logger.LogWarning("无法多样化空的解决方案列表");
                return new List<SchedulingSolution>();
            }
            
            if (solutions.Count <= count)
            {
                _logger.LogInformation($"解决方案数量 {solutions.Count} 不超过需要的数量 {count}，无需多样化");
                return solutions.ToList();
            }
            
            _logger.LogInformation($"开始对 {solutions.Count} 个解决方案进行多样化筛选，目标数量: {count}");
            
            // 首先过滤出满足所有硬约束的解决方案
            var validSolutions = new List<SchedulingSolution>();
            foreach (var solution in solutions)
            {
                bool satisfiesHardConstraints = true;
                
                // 检查教师可用性约束
                if (problem.TeacherAvailabilities.Count > 0)
                {
                    foreach (var assignment in solution.Assignments)
                    {
                        var teacherAvailability = problem.TeacherAvailabilities
                            .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId && 
                                                  ta.TimeSlotId == assignment.TimeSlotId);
                        
                        if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                        {
                            // 如果教师在这个时间段不可用，那么解决方案不满足硬约束
                            satisfiesHardConstraints = false;
                            _logger.LogDebug($"解决方案 #{solution.Id} 违反教师可用性约束: 教师 {assignment.TeacherId} 在时段 {assignment.TimeSlotId} 不可用");
                            break;
                        }
                    }
                }
                
                // 检查教室可用性约束
                if (satisfiesHardConstraints && problem.ClassroomAvailabilities.Count > 0)
                {
                    foreach (var assignment in solution.Assignments)
                    {
                        var classroomAvailability = problem.ClassroomAvailabilities
                            .FirstOrDefault(ca => ca.ClassroomId == assignment.ClassroomId && 
                                                  ca.TimeSlotId == assignment.TimeSlotId);
                        
                        if (classroomAvailability != null && !classroomAvailability.IsAvailable)
                        {
                            // 如果教室在这个时间段不可用，那么解决方案不满足硬约束
                            satisfiesHardConstraints = false;
                            _logger.LogDebug($"解决方案 #{solution.Id} 违反教室可用性约束: 教室 {assignment.ClassroomId} 在时段 {assignment.TimeSlotId} 不可用");
                            break;
                        }
                    }
                }
                
                // 如果解决方案满足所有硬约束，将其添加到有效解决方案列表
                if (satisfiesHardConstraints)
                {
                    validSolutions.Add(solution);
                }
            }
            
            _logger.LogInformation($"硬约束检查完成，有 {validSolutions.Count}/{solutions.Count} 个解决方案满足所有硬约束");
            
            // 如果没有足够的有效解决方案，将不足的部分用原始解决方案补足
            if (validSolutions.Count < count)
            {
                var invalidSolutions = solutions.Except(validSolutions).ToList();
                validSolutions.AddRange(invalidSolutions.Take(count - validSolutions.Count));
                _logger.LogWarning($"有效解决方案数量不足，从无效解决方案中添加 {count - validSolutions.Count} 个作为补充");
            }
            
            // 创建多样化的解集
            var diverseSet = new List<SchedulingSolution>();
            
            // 使用Id而不是Score来排序，避免Score为0导致的排序问题
            var remainingSolutions = validSolutions.OrderByDescending(s => s.Id).ToList();
            
            var bestSolution = remainingSolutions.First();
            diverseSet.Add(bestSolution);
            remainingSolutions.Remove(bestSolution);
            _logger.LogDebug($"已添加首个解决方案: #{bestSolution.Id}");
            
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
                double maxDistance = solutionDistances.Max(x => x.MinDistance);
                
                diverseSet.Add(mostDiverseSolution);
                remainingSolutions.Remove(mostDiverseSolution);
                _logger.LogDebug($"添加多样化解 #{mostDiverseSolution.Id}, 与已选解最小差异度: {maxDistance:F2}");
            }
            
            _logger.LogInformation($"多样化筛选完成，共选择 {diverseSet.Count} 个解决方案");
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
            return totalAssignments > 0 ? (double)differentAssignments / totalAssignments : 0;
        }

        /// <summary>
        /// 随机修改解以增加多样性
        /// </summary>
        /// <param name="solution">原始解决方案</param>
        /// <param name="diversityLevel">多样性级别</param>
        /// <param name="problem">排课问题实例，用于检查约束</param>
        /// <returns>多样化的解决方案</returns>
        public SchedulingSolution DiversifySolution(SchedulingSolution solution, double diversityLevel, SchedulingProblem problem = null)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            _logger.LogInformation($"开始多样化解决方案 #{solution.Id}，多样化级别: {diversityLevel:F2}");
            
            // 创建解的深拷贝
            var newSolution = solution.Clone();

            // 根据多样性级别确定要修改的分配数量
            int assignmentsToModify = (int)Math.Ceiling(newSolution.Assignments.Count * diversityLevel);
            _logger.LogDebug($"计划修改 {assignmentsToModify}/{newSolution.Assignments.Count} 个分配");

            // 随机选择要修改的分配
            var assignmentsToChange = newSolution.Assignments
                .OrderBy(x => _random.Next())
                .Take(assignmentsToModify)
                .ToList();

            int modifiedCount = 0;
            
            // 修改选中的分配
            foreach (var assignment in assignmentsToChange)
            {
                // 保存原始值，以便在违反约束时恢复
                int originalTimeSlotId = assignment.TimeSlotId;
                int originalClassroomId = assignment.ClassroomId;
                int originalTeacherId = assignment.TeacherId;

                // 尝试最多10次来找到满足约束的修改
                bool validModificationFound = false;
                for (int attempt = 0; attempt < 10 && !validModificationFound; attempt++)
                {
                    // 随机选择修改类型(时间、教室、教师)
                    int modificationType = _random.Next(3);
                    
                    // 尝试修改
                    bool modified = false;
                    
                    if (modificationType == 0 && problem?.TimeSlots != null && problem.TimeSlots.Count > 1)
                    {
                        // 修改时间槽
                        var availableTimeSlots = problem.TimeSlots
                            .Where(ts => ts.Id != assignment.TimeSlotId)
                            .ToList();
                            
                        if (availableTimeSlots.Count > 0)
                        {
                            var newTimeSlot = availableTimeSlots[_random.Next(availableTimeSlots.Count)];
                            assignment.TimeSlotId = newTimeSlot.Id;
                            assignment.DayOfWeek = newTimeSlot.DayOfWeek;
                            assignment.StartTime = newTimeSlot.StartTime;
                            assignment.EndTime = newTimeSlot.EndTime;
                            modified = true;
                            _logger.LogDebug($"修改时间槽: {originalTimeSlotId} -> {newTimeSlot.Id}");
                        }
                    }
                    else if (modificationType == 1 && problem?.Classrooms != null && problem.Classrooms.Count > 1)
                    {
                        // 修改教室
                        var courseSection = problem.CourseSections.FirstOrDefault(cs => cs.Id == assignment.SectionId);
                        var availableClassrooms = problem.Classrooms
                            .Where(c => c.Id != assignment.ClassroomId && 
                                   (courseSection == null || c.Capacity >= courseSection.Enrollment))
                            .ToList();
                            
                        if (availableClassrooms.Count > 0)
                        {
                            var newClassroom = availableClassrooms[_random.Next(availableClassrooms.Count)];
                            assignment.ClassroomId = newClassroom.Id;
                            assignment.ClassroomName = newClassroom.Name;
                            modified = true;
                            _logger.LogDebug($"修改教室: {originalClassroomId} -> {newClassroom.Id}");
                        }
                    }
                    else if (problem?.Teachers != null && problem.Teachers.Count > 1)
                    {
                        // 修改教师
                        var availableTeachers = problem.Teachers
                            .Where(t => t.Id != assignment.TeacherId)
                            .ToList();
                            
                        if (availableTeachers.Count > 0)
                        {
                            var newTeacher = availableTeachers[_random.Next(availableTeachers.Count)];
                            assignment.TeacherId = newTeacher.Id;
                            assignment.TeacherName = newTeacher.Name;
                            modified = true;
                            _logger.LogDebug($"修改教师: {originalTeacherId} -> {newTeacher.Id}");
                        }
                    }
                    
                    if (modified)
                    {
                        // 检查约束是否满足
                        bool constraintsSatisfied = CheckConstraints(newSolution, assignment, problem);
                        
                        if (constraintsSatisfied)
                        {
                            validModificationFound = true;
                            modifiedCount++;
                        }
                        else
                        {
                            // 恢复原值
                            assignment.TimeSlotId = originalTimeSlotId;
                            assignment.ClassroomId = originalClassroomId;
                            assignment.TeacherId = originalTeacherId;
                            _logger.LogDebug("修改违反约束，恢复原值");
                        }
                    }
                }
            }
            
            _logger.LogInformation($"多样化完成，实际修改了 {modifiedCount}/{assignmentsToModify} 个分配");
            return newSolution;
        }
        
        /// <summary>
        /// 检查约束是否满足
        /// </summary>
        private bool CheckConstraints(SchedulingSolution solution, SchedulingAssignment modifiedAssignment, SchedulingProblem problem)
        {
            if (problem == null)
                return true;
                
            try
            {
                // 检查教师时间冲突
                var teacherTimeConflict = solution.Assignments
                    .Where(a => a != modifiedAssignment && a.TeacherId == modifiedAssignment.TeacherId && a.TimeSlotId == modifiedAssignment.TimeSlotId)
                    .Any();
                    
                if (teacherTimeConflict)
                {
                    _logger.LogDebug($"发现教师时间冲突: 教师 {modifiedAssignment.TeacherId} 在时间槽 {modifiedAssignment.TimeSlotId} 已有其他课程");
                    return false;
                }
                
                // 检查教室时间冲突
                var roomTimeConflict = solution.Assignments
                    .Where(a => a != modifiedAssignment && a.ClassroomId == modifiedAssignment.ClassroomId && a.TimeSlotId == modifiedAssignment.TimeSlotId)
                    .Any();
                    
                if (roomTimeConflict)
                {
                    _logger.LogDebug($"发现教室时间冲突: 教室 {modifiedAssignment.ClassroomId} 在时间槽 {modifiedAssignment.TimeSlotId} 已有其他课程");
                    return false;
                }
                
                // 检查教师可用性
                var teacherAvailability = problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == modifiedAssignment.TeacherId && ta.TimeSlotId == modifiedAssignment.TimeSlotId);
                    
                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                {
                    _logger.LogDebug($"教师 {modifiedAssignment.TeacherId} 在时间槽 {modifiedAssignment.TimeSlotId} 不可用");
                    return false;
                }
                
                // 检查教室可用性
                var roomAvailability = problem.ClassroomAvailabilities
                    .FirstOrDefault(ca => ca.ClassroomId == modifiedAssignment.ClassroomId && ca.TimeSlotId == modifiedAssignment.TimeSlotId);
                    
                if (roomAvailability != null && !roomAvailability.IsAvailable)
                {
                    _logger.LogDebug($"教室 {modifiedAssignment.ClassroomId} 在时间槽 {modifiedAssignment.TimeSlotId} 不可用");
                    return false;
                }
                
                // 检查教室容量
                var courseSection = problem.CourseSections.FirstOrDefault(cs => cs.Id == modifiedAssignment.SectionId);
                var classroom = problem.Classrooms.FirstOrDefault(c => c.Id == modifiedAssignment.ClassroomId);
                
                if (courseSection != null && classroom != null && classroom.Capacity < courseSection.Enrollment)
                {
                    _logger.LogDebug($"教室 {classroom.Id} 容量 {classroom.Capacity} 不足以容纳 {courseSection.Enrollment} 名学生");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查约束时发生异常");
                return false;
            }
        }
    }
} 