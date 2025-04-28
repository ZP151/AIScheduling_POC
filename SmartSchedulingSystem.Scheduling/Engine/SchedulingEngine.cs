using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Algorithms.Hybrid;
using SmartSchedulingSystem.Scheduling.Algorithms.LS;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    /// <summary>
    /// 排课引擎核心类，负责协调并执行完整的排课过程
    /// </summary>
    public class SchedulingEngine
    {
        private readonly ILogger<SchedulingEngine> _logger;
        private readonly ConstraintManager _constraintManager;
        private readonly CPScheduler _cpScheduler;
        private readonly LocalSearchOptimizer _localSearchOptimizer;
        private readonly CPLSScheduler _cplsScheduler;
        private readonly ProblemAnalyzer _problemAnalyzer;
        private readonly SolutionEvaluator _solutionEvaluator;
        private readonly SolutionDiversifier _solutionDiversifier;

        public SchedulingEngine(
            ILogger<SchedulingEngine> logger,
            ConstraintManager constraintManager,
            CPScheduler cpScheduler,
            LocalSearchOptimizer localSearchOptimizer,
            CPLSScheduler cplsScheduler,
            ProblemAnalyzer problemAnalyzer,
            SolutionEvaluator solutionEvaluator,
            SolutionDiversifier solutionDiversifier)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _cpScheduler = cpScheduler ?? throw new ArgumentNullException(nameof(cpScheduler));
            _localSearchOptimizer = localSearchOptimizer ?? throw new ArgumentNullException(nameof(localSearchOptimizer));
            _cplsScheduler = cplsScheduler ?? throw new ArgumentNullException(nameof(cplsScheduler));
            _problemAnalyzer = problemAnalyzer ?? throw new ArgumentNullException(nameof(problemAnalyzer));
            _solutionEvaluator = solutionEvaluator ?? throw new ArgumentNullException(nameof(solutionEvaluator));
            _solutionDiversifier = solutionDiversifier ?? throw new ArgumentNullException(nameof(solutionDiversifier));
            
            // 注册全局约束管理器，使其可以在CPScheduler中访问
            GlobalConstraintManager.Initialize(_constraintManager);
            _logger.LogInformation("已将约束管理器注册为全局实例");
        }

        /// <summary>
        /// 生成排课方案
        /// </summary>
        /// <param name="problem">排课问题定义</param>
        /// <param name="parameters">排课参数</param>
        /// <param name="useSimplifiedMode">是否使用简化模式</param>
        /// <returns>排课结果</returns>
        public SchedulingResult GenerateSchedule(SchedulingProblem problem, Utils.SchedulingParameters parameters = null, bool useSimplifiedMode = false)
        {
            try
            {
                _logger.LogInformation("开始生成排课方案，使用渐进式约束策略...");
                
                // 设置是否使用简化约束模式
                if (_constraintManager is ConstraintManager constraintManager)
                {
                    constraintManager.UseSimplifiedConstraints(useSimplifiedMode);
                    
                    if (useSimplifiedMode)
                    {
                        _logger.LogInformation("使用简化约束模式生成排课方案");
                    }
                }

                // 分析问题
                var features = _problemAnalyzer.AnalyzeProblem(problem);
                _logger.LogInformation("问题特征: 课程数={CourseCount}, 教师数={TeacherCount}, 教室数={ClassroomCount}, 复杂度={Complexity}",
                    features.CourseSectionCount, features.TeacherCount, features.ClassroomCount, features.OverallComplexity);

                // 如果没有提供参数，使用推荐参数
                parameters ??= _problemAnalyzer.RecommendParameters(features);
                
                var result = new SchedulingResult
                {
                    Problem = problem,
                    Status = SchedulingStatus.NotStarted,
                    CreatedAt = DateTime.Now,
                    Solutions = new List<SchedulingSolution>(),
                    Message = "使用渐进式约束策略生成排课方案"
                };

                // 默认设置为标准级别
                ConstraintApplicationLevel constraintLevel = ConstraintApplicationLevel.Standard;
                
                // 根据参数决定使用哪个约束级别
                if (parameters?.UseBasicConstraints == true)
                {
                    _logger.LogInformation("根据参数设置，仅使用Basic级别约束(仅Level1)...");
                    constraintLevel = ConstraintApplicationLevel.Basic;
                }
                else if (parameters?.UseEnhancedConstraints == true)
                {
                    _logger.LogInformation("根据参数设置，启用Enhanced级别约束(包含Level3物理软约束)...");
                    constraintLevel = ConstraintApplicationLevel.Enhanced;
                }
                else
                {
                    _logger.LogInformation("使用Standard级别约束(Level1+Level2)...");
                    // 使用默认的Standard级别
                }
                
                // 设置约束应用级别
                _constraintManager.SetConstraintApplicationLevel(constraintLevel);
                
                var startTime = DateTime.Now;
                
                // 生成指定数量的解决方案，使用更新后的渐进式约束策略
                int targetSolutionCount = problem.GenerateMultipleSolutions ? 
                    Math.Max(3, problem.SolutionCount) : 1; // 确保至少生成3个解决方案
                
                // 生成解决方案
                var solutions = _cpScheduler.GenerateRandomSolutions(problem, targetSolutionCount);
                
                if (solutions.Count > 0)
                {
                    _logger.LogInformation($"成功生成 {solutions.Count} 个解决方案");
                    
                    // 检查解决方案的多样性
                    _logger.LogInformation("检查解决方案多样性，当前有 {SolutionsCount} 个解决方案", solutions.Count);
                    if (solutions.Count > 1)
                    {
                        // 确保解决方案足够多样化，最多限制为5个解决方案
                        solutions = _solutionDiversifier.DiversifySolutions(problem, solutions, 5).ToList();
                        _logger.LogInformation("已优化解决方案多样性，最终有 {SolutionsCount} 个解决方案", solutions.Count);
                    }
                    
                    // 检查每个解决方案中是否存在同一课程被安排多次的情况
                    foreach (var solution in solutions)
                    {
                        var sectionIds = new HashSet<int>();
                        var duplicateAssignments = new List<SchedulingAssignment>();
                        
                        foreach (var assignment in solution.Assignments)
                        {
                            if (sectionIds.Contains(assignment.SectionId))
                            {
                                duplicateAssignments.Add(assignment);
                                _logger.LogWarning($"解决方案 #{solution.Id} 中课程 {assignment.SectionId} ({assignment.SectionCode}) 被安排了多次，移除重复安排");
                            }
                            else
                            {
                                sectionIds.Add(assignment.SectionId);
                            }
                        }
                        
                        // 移除重复的课程安排
                        foreach (var duplicate in duplicateAssignments)
                        {
                            solution.Assignments.Remove(duplicate);
                        }
                        
                        if (duplicateAssignments.Count > 0)
                        {
                            _logger.LogInformation($"从解决方案 #{solution.Id} 中移除了 {duplicateAssignments.Count} 个重复的课程安排");
                        }
                    }
                    
                    // 如果解决方案数量不足，尝试创建变体
                    if (solutions.Count < targetSolutionCount)
                    {
                        _logger.LogInformation($"生成的解决方案数量不足（{solutions.Count}/{targetSolutionCount}），创建变体...");
                        var existingSolutions = new List<SchedulingSolution>(solutions);
                        
                        // 基于现有解决方案创建变体
                        for (int i = solutions.Count; i < targetSolutionCount && existingSolutions.Count > 0; i++)
                        {
                            var baseSolution = existingSolutions[i % existingSolutions.Count];
                            var variant = baseSolution.Clone();
                            
                            // 修改一些分配以创建变体
                            CreateSolutionVariant(variant, problem);
                            
                            // 给变体一个新的ID
                            variant.Id = i + 1;
                            variant.Algorithm = $"Variant-{baseSolution.Algorithm}";
                            
                            // 评估变体
                            var evaluation = _solutionEvaluator.Evaluate(variant);
                            variant.Evaluation = evaluation;
                            
                            solutions.Add(variant);
                            _logger.LogInformation($"创建了解决方案变体 #{i+1}，基于解决方案 #{baseSolution.Id}");
                        }
                    }
                    
                    // 确保每个解决方案有唯一ID
                    for (int i = 0; i < solutions.Count; i++)
                    {
                        solutions[i].Id = i + 1;
                        
                        // 对解决方案进行优化（可选）
                        if (parameters.EnableLocalSearch && parameters.MaxLsIterations > 0)
                        {
                            try
                            {
                                _logger.LogInformation($"开始对解决方案 #{solutions[i].Id} 进行局部搜索优化...");
                                
                                var optimizedSolution = _localSearchOptimizer.OptimizeSolution(
                                    solutions[i], 
                                    parameters.MaxLsIterations,
                                    parameters.InitialTemperature,
                                    parameters.CoolingRate);
                                
                                // 如果优化后的解决方案更好，则替换
                                if (optimizedSolution.Score > solutions[i].Score)
                                {
                                    _logger.LogInformation($"解决方案 #{solutions[i].Id} 优化后得分从 {solutions[i].Score:F2} 提高到 {optimizedSolution.Score:F2}");
                                    optimizedSolution.Algorithm = solutions[i].Algorithm + "+LS";
                                    optimizedSolution.Id = solutions[i].Id; // 保持相同的ID
                                    solutions[i] = optimizedSolution;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"优化解决方案 #{solutions[i].Id} 时发生异常");
                            }
                        }
                    }
                    
                    result.Solutions = solutions;
                    result.Status = SchedulingStatus.Success;
                    result.Message = "成功使用渐进式约束策略生成排课方案";
                }
                else
                {
                    _logger.LogWarning("使用渐进式约束策略未能生成任何解决方案");
                    result.Status = SchedulingStatus.Failure;
                    result.Message = "使用渐进式约束策略未能生成任何解决方案";
                }
                
                // 计算执行时间
                result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
                
                // 计算统计信息
                result.Statistics.TotalSections = problem.CourseSections.Count;
                result.Statistics.ScheduledSections = result.Solutions.Count > 0 ? 
                    result.Solutions[0].Assignments.Count : 0;
                result.Statistics.UnscheduledSections = result.Statistics.TotalSections - result.Statistics.ScheduledSections;
                result.Statistics.TotalTeachers = problem.Teachers.Count;
                result.Statistics.TotalClassrooms = problem.Classrooms.Count;
                
                // 恢复约束管理器状态
                if (_constraintManager is ConstraintManager constraintManager2)
                {
                    constraintManager2.UseSimplifiedConstraints(false);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成排课方案时发生异常");
                
                // 恢复约束管理器状态
                if (_constraintManager is ConstraintManager constraintManager)
                {
                    constraintManager.UseSimplifiedConstraints(false);
                }
                
                // 生成随机解作为异常情况下的备选方案
                try
                {
                    var randomResult = new SchedulingResult
                    {
                        Status = SchedulingStatus.Error,
                        Message = $"发生异常: {ex.Message}，返回随机生成的备选方案",
                        Solutions = new List<SchedulingSolution>()
                    };
                    
                    int targetSolutionCount = problem.GenerateMultipleSolutions ? 
                        Math.Max(3, problem.SolutionCount) : 1; // 确保至少生成3个解决方案
                    
                    // 使用最低约束级别强制生成解决方案
                    _constraintManager.SetConstraintApplicationLevel(ConstraintApplicationLevel.Basic);
                    var randomSolutions = _cpScheduler.GenerateRandomSolutions(problem, targetSolutionCount);
                    
                    // 确保每个解决方案有唯一ID
                    for (int i = 0; i < randomSolutions.Count; i++)
                    {
                        randomSolutions[i].Id = i + 1;
                        randomSolutions[i].Algorithm = $"Emergency-Random-{i+1}";
                    }
                    
                    if (randomSolutions.Count > 0)
                    {
                        _logger.LogInformation($"异常情况下生成了 {randomSolutions.Count} 个随机解作为备选方案");
                        randomResult.Solutions = randomSolutions;
                        randomResult.Status = SchedulingStatus.PartialSuccess;
                        randomResult.Message = "遇到异常，返回使用最低约束级别生成的解决方案";
                        return randomResult;
                    }
                    
                    return randomResult;
                }
                catch
                {
                    // 如果随机解也失败了，返回原始错误
                    return new SchedulingResult
                    {
                        Status = SchedulingStatus.Error,
                        Message = $"发生异常: {ex.Message}",
                        Solutions = new List<SchedulingSolution>()
                    };
                }
            }
        }

        /// <summary>
        /// 创建解决方案变体，通过随机修改部分分配来增加多样性
        /// </summary>
        private void CreateSolutionVariant(SchedulingSolution solution, SchedulingProblem problem)
        {
            if (solution.Assignments.Count == 0)
                return;
                
            var random = new Random();
            
            // 决定要修改的分配数量（约20%）
            int modificationCount = Math.Max(1, solution.Assignments.Count / 5);
            
            // 选择要修改的分配
            var assignmentsToModify = solution.Assignments
                .OrderBy(x => random.Next())
                .Take(modificationCount)
                .ToList();
                
            foreach (var assignment in assignmentsToModify)
            {
                // 随机决定修改哪个部分（时间、教室、教师）
                int modType = random.Next(3);
                
                if (modType == 0 && problem.TimeSlots.Count > 1)
                {
                    // 修改时间槽
                    var availableTimeSlots = problem.TimeSlots
                        .Where(ts => ts.Id != assignment.TimeSlotId)
                        .ToList();
                        
                    if (availableTimeSlots.Count > 0)
                    {
                        var newTimeSlot = availableTimeSlots[random.Next(availableTimeSlots.Count)];
                        assignment.TimeSlotId = newTimeSlot.Id;
                        assignment.DayOfWeek = newTimeSlot.DayOfWeek;
                        assignment.StartTime = newTimeSlot.StartTime;
                        assignment.EndTime = newTimeSlot.EndTime;
                    }
                }
                else if (modType == 1 && problem.Classrooms.Count > 1)
                {
                    // 修改教室
                    var availableClassrooms = problem.Classrooms
                        .Where(c => c.Id != assignment.ClassroomId && c.Capacity >= problem.CourseSections.FirstOrDefault(cs => cs.Id == assignment.SectionId)?.Enrollment)
                        .ToList();
                        
                    if (availableClassrooms.Count > 0)
                    {
                        var newClassroom = availableClassrooms[random.Next(availableClassrooms.Count)];
                        assignment.ClassroomId = newClassroom.Id;
                        assignment.ClassroomName = newClassroom.Name;
                    }
                }
                else if (problem.Teachers.Count > 1)
                {
                    // 修改教师
                    var availableTeachers = problem.Teachers
                        .Where(t => t.Id != assignment.TeacherId)
                        .ToList();
                        
                    if (availableTeachers.Count > 0)
                    {
                        var newTeacher = availableTeachers[random.Next(availableTeachers.Count)];
                        assignment.TeacherId = newTeacher.Id;
                        assignment.TeacherName = newTeacher.Name;
                    }
                }
            }
        }

        /// <summary>
        /// 评估排课方案
        /// </summary>
        public SchedulingEvaluation EvaluateSchedule(SchedulingSolution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            try
            {
                _logger.LogInformation("开始评估排课方案...");

                // 使用评估器评估方案
                double score = _solutionEvaluator.Evaluate(solution).Score;
                var hardConstraintSatisfaction = _solutionEvaluator.EvaluateHardConstraints(solution);
                var softConstraintSatisfaction = _solutionEvaluator.EvaluateSoftConstraints(solution);

                return new SchedulingEvaluation
                {
                    SolutionId = solution.Id,
                    Score = score,
                    HardConstraintsSatisfied = hardConstraintSatisfaction >= 1.0,
                    HardConstraintsSatisfactionLevel = hardConstraintSatisfaction,
                    SoftConstraintsSatisfactionLevel = softConstraintSatisfaction
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "评估排课方案时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 优化现有排课方案
        /// </summary>
        /// <param name="solution">排课方案</param>
        /// <param name="parameters">排课参数</param>
        /// <returns>优化后的方案</returns>
        public SchedulingSolution OptimizeSchedule(SchedulingSolution solution, Utils.SchedulingParameters parameters = null)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            try
            {
                _logger.LogInformation("开始优化排课方案...");

                // 使用局部搜索优化器优化方案
                var optimizedSolution = _localSearchOptimizer.OptimizeSolution(solution);

                _logger.LogInformation("排课方案优化完成");

                return optimizedSolution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "优化排课方案时发生异常");
                throw;
            }
        }
    }
}