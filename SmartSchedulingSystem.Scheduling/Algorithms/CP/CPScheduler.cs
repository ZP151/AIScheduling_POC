using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 约束规划排课引擎
    /// </summary>
    public class CPScheduler
    {
        private readonly CPModelBuilder _modelBuilder;
        private readonly SolutionConverter _solutionConverter;
        private readonly ILogger<CPScheduler> _logger;
        private readonly SmartSchedulingSystem.Scheduling.Utils.SchedulingParameters _parameters;
        private readonly Random _random;
        private readonly Dictionary<string, ICPConstraintConverter> _constraintConverters;

        public CPScheduler(
            CPModelBuilder modelBuilder,
            SolutionConverter solutionConverter,
            ILogger<CPScheduler> logger,
            SchedulingParameters parameters = null)
        {
            _modelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
            _solutionConverter = solutionConverter ?? throw new ArgumentNullException(nameof(solutionConverter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parameters = parameters ?? new SchedulingParameters();
            _random = new Random();
            _constraintConverters = new Dictionary<string, ICPConstraintConverter>();
            InitializeConstraintConverters();
        }

        /// <summary>
        /// 初始化约束转换器
        /// </summary>
        private void InitializeConstraintConverters()
        {
            // 这里添加约束转换器的初始化代码
            // 例如:
            // _constraintConverters["TeacherConflict"] = new TeacherConflictConverter();
            // _constraintConverters["ClassroomConflict"] = new ClassroomConflictConverter();
            
            _logger.LogInformation("初始化约束转换器完成");
        }

        /// <summary>
        /// 使用约束规划生成初始解集
        /// </summary>
        public List<SchedulingSolution> GenerateInitialSolutions(SchedulingProblem problem, int solutionCount = 5)
        {
            try
            {
                _logger.LogInformation($"开始使用CP求解器生成初始解，目标数量：{solutionCount}");
                Console.WriteLine("============ CP求解开始 ============");
                Console.WriteLine($"问题ID: {problem.Id}, 名称: {problem.Name}");
                Console.WriteLine($"请求解决方案数量: {solutionCount}");
                
                // 检查问题数据完整性
                ValidateProblemData(problem);
                DebugProblemData(problem); // 添加这行来调试问题数据

                var sw = Stopwatch.StartNew();

                // 采用渐进式约束应用方案
                List<SchedulingSolution> solutions = null;
                
                // 首先尝试使用最小级别约束生成解
                _logger.LogInformation("尝试使用Basic级别约束生成初始解...");
                solutions = TryGenerateWithConstraintLevel(problem, solutionCount, ConstraintApplicationLevel.Basic);
                
                // 如果没有找到解，尝试进一步放宽约束
                if (solutions.Count == 0)
                {
                    _logger.LogWarning("使用Basic级别约束未能找到解，尝试使用更宽松的约束生成随机解...");
                    solutions = GenerateRandomSolutions(problem, solutionCount);
                }
                
                _logger.LogInformation($"CP阶段完成，共生成 {solutions.Count} 个初始解");
                
                return solutions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成初始解时发生异常");
                return new List<SchedulingSolution>();
            }
        }
        
        /// <summary>
        /// 使用指定约束级别尝试生成解决方案
        /// </summary>
        private List<SchedulingSolution> TryGenerateWithConstraintLevel(
            SchedulingProblem problem, 
            int solutionCount,
            ConstraintApplicationLevel level)
        {
            try
            {
                var sw = Stopwatch.StartNew();
            
                // 创建CP模型，使用指定约束级别
                var model = _modelBuilder.BuildModel(problem, level);

                _logger.LogInformation($"使用{level}级别约束构建CP模型，耗时：{sw.ElapsedMilliseconds}ms");

                // 创建CP求解器
                var solver = new CpSolver();
                
                // 配置求解器
                int numThreads = Math.Max(1, Environment.ProcessorCount / 2);
                int timeLimit = _parameters.CpTimeLimit > 0 ? _parameters.CpTimeLimit : 60;
                
                solver.StringParameters = $"num_search_workers:{numThreads};max_time_in_seconds:{timeLimit}";
                solver.StringParameters += ";log_search_progress:true;collect_all_solutions_as_last_solution:true";
                
                _logger.LogInformation($"设置CP求解参数：{solver.StringParameters}");
                
                // 创建变量字典
                var variableDict = ExtractVariablesDictionary(model);
                
                // 创建解回调
                var callback = new CPSolutionCallback(variableDict, solutionCount);

                // 启动求解，但使用可取消的方式
                sw.Restart();
                
                // 设置超时监控
                var tokenSource = new CancellationTokenSource();
                Task timeoutTask = Task.Run(() => {
                    try 
                    {
                        // 使用额外的1分钟作为安全余量
                        int timeoutMs = (timeLimit + 60) * 1000;
                        Thread.Sleep(timeoutMs);
                        
                        // 如果执行到这里，说明超时了
                        _logger.LogWarning($"CP求解超过预设时间 {timeLimit}秒，强制终止求解");
                        tokenSource.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "超时监控任务异常");
                    }
                });
                
                Task<CpSolverStatus> solveTask = Task.Run(() => {
                    try
                    {
                        return solver.Solve(model, callback);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "CP求解过程中发生异常");
                        return CpSolverStatus.Unknown;
                    }
                });

                CpSolverStatus status;
                try
                {
                    // 等待求解完成或被取消
                    if (Task.WaitAny(new Task[] { solveTask }, timeLimit * 1000 + 10000) == 0)
                    {
                        // 正常完成
                        status = solveTask.Result;
                        _logger.LogInformation($"CP求解正常完成，状态: {status}");
                    }
                    else
                    {
                        // 超时
                        tokenSource.Cancel();
                        status = CpSolverStatus.Unknown;
                        _logger.LogWarning("CP求解超时，强制中断");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "等待CP求解结果过程中发生异常");
                    status = CpSolverStatus.Unknown;
                }
                
                // 取消超时监控任务
                tokenSource.Cancel();
                
                sw.Stop();
                _logger.LogInformation($"使用{level}级别约束的CP求解耗时：{sw.ElapsedMilliseconds}ms，状态：{status}，找到解数量：{callback.SolutionCount}");

                // 如果没有找到足够的解但计算被中断，尝试使用求解器收集的中间解
                if (callback.SolutionCount == 0 && status == CpSolverStatus.Unknown)
                {
                    _logger.LogWarning("CP求解被中断且没有返回有效解，尝试获取中间解");
                    
                    // 创建一个基本解
                    try
                    {
                        var partialSolution = CollectPartialSolution(problem, model, solver);
                        if (partialSolution != null)
                        {
                            _logger.LogInformation("成功构建部分解决方案");
                            return new List<SchedulingSolution> { partialSolution };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "尝试构建部分解决方案时出错");
                    }
                }

                // 转换为排课系统解
                var solutions = new List<SchedulingSolution>();

                _logger.LogInformation($"开始转换CP解为排课系统解...");
                sw.Restart();

                foreach (var cpSolution in callback.Solutions)
                {
                    try
                    {
                        var solution = _solutionConverter.ConvertToDomainSolution(problem, cpSolution);
                        solution.ConstraintLevel = level; // 标记解是在哪个约束级别下生成的
                        
                        // 计算解的评分
                        double score = EvaluateSolutionQuality(solution, problem);
                        _logger.LogDebug($"解评分: {score}");
                        
                        solutions.Add(solution);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "转换CP解为排课系统解时出错");
                    }
                }

                sw.Stop();
                _logger.LogInformation($"转换完成，耗时：{sw.ElapsedMilliseconds}ms，成功转换 {solutions.Count} 个解");

                return solutions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"使用{level}约束级别生成解时发生异常");
                return new List<SchedulingSolution>();
            }
        }
        
        /// <summary>
        /// 生成随机解，即使有约束冲突也能返回部分可行解
        /// </summary>
        public List<SchedulingSolution> GenerateRandomSolutions(SchedulingProblem problem, int solutionCount)
        {
            _logger.LogInformation($"开始生成 {solutionCount} 个随机解");
            var solutions = new List<SchedulingSolution>();
            var random = new Random();
            
            // 定义简化后的约束应用级别序列
            var constraintLevels = new[] {
                ConstraintApplicationLevel.Basic,
                ConstraintApplicationLevel.Standard,
                ConstraintApplicationLevel.Complete
            };
            
            _logger.LogInformation("使用三级约束应用策略，逐步增加约束级别");
            
            // 先尝试用最高级别约束生成所有解决方案
            _logger.LogInformation($"第一阶段：尝试用完整级别约束 {ConstraintApplicationLevel.Complete} 生成解决方案");
            solutions = TryGenerateSolutionsWithConstraintLevel(problem, solutionCount, ConstraintApplicationLevel.Complete, random);
            
            // 确保每个解决方案中没有重复的课程安排
            EnsureNoDuplicateAssignments(solutions);
            
            // 如果找不到足够的解决方案，降低约束级别
            if (solutions.Count < solutionCount)
            {
                _logger.LogInformation($"完整级别约束仅生成了 {solutions.Count}/{solutionCount} 个解决方案，降级到标准约束");
                
                // 尝试标准级别
                int remainingSolutions = solutionCount - solutions.Count;
                var standardSolutions = TryGenerateSolutionsWithConstraintLevel(problem, remainingSolutions, ConstraintApplicationLevel.Standard, random);
                
                // 确保新生成的解决方案中没有重复的课程安排
                EnsureNoDuplicateAssignments(standardSolutions);
                
                if (standardSolutions.Count > 0)
                {
                    _logger.LogInformation($"在标准级别约束下额外生成了 {standardSolutions.Count} 个解决方案");
                    solutions.AddRange(standardSolutions);
                }
                
                // 如果仍然不够，使用基本级别
                if (solutions.Count < solutionCount)
                {
                    _logger.LogInformation($"标准级别约束后仅有 {solutions.Count}/{solutionCount} 个解决方案，降级到基本约束");
                    
                    remainingSolutions = solutionCount - solutions.Count;
                    var basicSolutions = TryGenerateSolutionsWithConstraintLevel(problem, remainingSolutions, ConstraintApplicationLevel.Basic, random);
                    
                    // 确保新生成的解决方案中没有重复的课程安排
                    EnsureNoDuplicateAssignments(basicSolutions);
                    
                    if (basicSolutions.Count > 0)
                    {
                        _logger.LogInformation($"在基本级别约束下额外生成了 {basicSolutions.Count} 个解决方案");
                        solutions.AddRange(basicSolutions);
                    }
                }
            }
            
            _logger.LogInformation($"最终生成了 {solutions.Count}/{solutionCount} 个解决方案");
            return solutions;
        }
        
        /// <summary>
        /// 确保每个解决方案中没有重复的课程安排
        /// </summary>
        private void EnsureNoDuplicateAssignments(List<SchedulingSolution> solutions)
        {
            foreach (var solution in solutions)
            {
                var sectionIds = new HashSet<int>();
                var duplicates = new List<SchedulingAssignment>();
                
                foreach (var assignment in solution.Assignments)
                {
                    if (sectionIds.Contains(assignment.SectionId))
                    {
                        duplicates.Add(assignment);
                        _logger.LogWarning($"在解决方案 #{solution.Id} 中发现课程 {assignment.SectionId} 被重复安排，将移除重复安排");
                    }
                    else
                    {
                        sectionIds.Add(assignment.SectionId);
                    }
                }
                
                // 移除重复安排
                foreach (var duplicate in duplicates)
                {
                    solution.Assignments.Remove(duplicate);
                }
                
                if (duplicates.Count > 0)
                {
                    _logger.LogInformation($"从解决方案 #{solution.Id} 中移除了 {duplicates.Count} 个重复的课程安排");
                }
            }
        }
        
        /// <summary>
        /// 尝试在特定约束级别下生成指定数量的解决方案
        /// </summary>
        private List<SchedulingSolution> TryGenerateSolutionsWithConstraintLevel(
            SchedulingProblem problem, 
            int targetCount,
            ConstraintApplicationLevel level,
            Random random)
        {
            var solutions = new List<SchedulingSolution>();
            var constraintManager = GetConstraintManager();
            
            // 保存原来的约束级别
            var originalLevel = constraintManager?.GetCurrentApplicationLevel() ?? ConstraintApplicationLevel.Standard;
            
            try
            {
                // 设置新的约束级别
                constraintManager?.SetConstraintApplicationLevel(level);
                _logger.LogInformation($"临时设置约束应用级别: {level}");
                
                // 尝试生成解决方案
                for (int i = 0; i < targetCount; i++)
                {
                    try
                    {
                        var solution = GenerateConstraintAwareRandomSolution(problem, level, random);
                        
                        if (solution != null && solution.Assignments.Count > 0)
                        {
                            // 标记解决方案使用的约束级别
                            solution.Algorithm = $"ConstraintAwareRandomCP-Level{level}";
                            solutions.Add(solution);
                            _logger.LogInformation($"在约束级别 {level} 下生成了解决方案 #{solutions.Count}，包含 {solution.Assignments.Count} 个分配");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"在约束级别 {level} 下生成解决方案时发生异常");
                    }
                }
            }
            finally
            {
                // 恢复原来的约束级别
                if (constraintManager != null && originalLevel != level)
                {
                    constraintManager.SetConstraintApplicationLevel(originalLevel);
                    _logger.LogInformation($"恢复约束应用级别: {originalLevel}");
                }
            }
            
            return solutions;
        }
        
        /// <summary>
        /// 生成单个考虑约束的随机解
        /// </summary>
        private SchedulingSolution GenerateConstraintAwareRandomSolution(
            SchedulingProblem problem,
            ConstraintApplicationLevel constraintLevel,
            Random random)
        {
            var solution = new SchedulingSolution();
            var courseSections = problem.CourseSections.ToList();
            
            _logger.LogInformation($"使用约束级别 {constraintLevel} 为 {courseSections.Count} 个课程分配时间、教室和教师");
            
            // 记录已分配的资源，避免冲突
            var usedRoomTimes = new HashSet<(int roomId, int timeSlotId)>();
            var usedTeacherTimes = new HashSet<(int teacherId, int timeSlotId)>();
            
            // 记录课程代码到教师的映射，确保同一课程的不同班级由同一教师教授
            var courseTeacherMap = new Dictionary<string, int>();
            
            // 随机排列课程顺序
            courseSections = courseSections.OrderBy(_ => random.Next()).ToList();
            
            foreach (var section in courseSections)
            {
                bool isAssigned = false;
                
                switch (constraintLevel)
                {
                    case ConstraintApplicationLevel.Complete:
                        _logger.LogDebug($"尝试为课程 {section.Id} 使用完整约束分配资源");
                        isAssigned = TryAssignWithAllHardConstraints(
                            problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                        
                        if (!isAssigned)
                        {
                            _logger.LogDebug($"使用完整约束分配失败，尝试降级到增强约束");
                            isAssigned = TryAssignWithStandardConstraints(
                                problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                        }
                        
                        if (!isAssigned)
                        {
                            _logger.LogDebug($"使用增强约束分配失败，尝试降级到标准约束");
                            isAssigned = TryAssignWithStandardConstraints(
                                problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                        }
                        
                        if (!isAssigned)
                        {
                            _logger.LogDebug($"使用标准约束分配失败，尝试降级到基本约束");
                            isAssigned = TryAssignWithBasicConstraints(
                                problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                        }
                        
                        if (!isAssigned)
                        {
                            _logger.LogWarning($"所有约束级别均无法分配课程 {section.Id}，使用强制分配");
                            ForceAssignmentWithMinimalConstraints(
                                problem, section, solution, random);
                        }
                        break;
                        
                    case ConstraintApplicationLevel.Enhanced:
                        _logger.LogDebug($"尝试为课程 {section.Id} 使用增强约束分配资源");
                        isAssigned = TryAssignWithStandardConstraints(
                            problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                        
                        if (!isAssigned)
                        {
                            _logger.LogDebug($"使用增强约束分配失败，尝试降级到标准约束");
                            isAssigned = TryAssignWithStandardConstraints(
                                problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                        }
                        
                        if (!isAssigned)
                        {
                            _logger.LogDebug($"使用标准约束分配失败，尝试降级到基本约束");
                            isAssigned = TryAssignWithBasicConstraints(
                                problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                        }
                        
                        if (!isAssigned)
                        {
                            _logger.LogWarning($"基本约束也无法分配课程 {section.Id}，使用强制分配");
                            ForceAssignmentWithMinimalConstraints(
                                problem, section, solution, random);
                        }
                        break;
                        
                    case ConstraintApplicationLevel.Standard:
                        _logger.LogDebug($"尝试为课程 {section.Id} 使用标准约束分配资源");
                        isAssigned = TryAssignWithStandardConstraints(
                            problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                        
                        if (!isAssigned)
                        {
                            _logger.LogDebug($"使用标准约束分配失败，尝试降级到基本约束");
                            isAssigned = TryAssignWithBasicConstraints(
                                problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                        }
                        
                        if (!isAssigned)
                        {
                            _logger.LogWarning($"基本约束也无法分配课程 {section.Id}，使用强制分配");
                            ForceAssignmentWithMinimalConstraints(
                                problem, section, solution, random);
                        }
                        break;
                        
                    case ConstraintApplicationLevel.Basic:
                        _logger.LogDebug($"尝试为课程 {section.Id} 使用基本约束分配资源");
                        isAssigned = TryAssignWithBasicConstraints(
                            problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                        
                        if (!isAssigned)
                        {
                            _logger.LogWarning($"基本约束无法分配课程 {section.Id}，使用强制分配");
                            ForceAssignmentWithMinimalConstraints(
                                problem, section, solution, random);
                        }
                        break;
                }
            }
            
            // 评估解的质量
            var quality = EvaluateSolutionQuality(solution, problem);
            _logger.LogInformation($"使用 {constraintLevel} 约束级别生成解决方案，质量得分: {quality}");
            
            return solution;
        }
        
        /// <summary>
        /// 获取约束管理器实例
        /// </summary>
        private IConstraintManager GetConstraintManager()
        {
            // 此处直接使用构造注入的约束管理器
            // 首先尝试通过SchedulingEngine获取，如果不可用则尝试使用服务定位器
            try
            {
                // 如果系统中有全局访问点，可以尝试从那里获取
                // 下面使用简化的全局访问方式，实际项目中需要替换为依赖注入
                var constraintManager = SmartSchedulingSystem.Scheduling.Engine.GlobalConstraintManager.Current;
                if (constraintManager != null)
                {
                    _logger.LogDebug("从全局实例获取了约束管理器");
                    return constraintManager;
                }
                
                // 作为备选，创建一个具有基本约束的临时管理器
                _logger.LogWarning("无法获取全局约束管理器，创建临时约束管理器实例");
                
                var tempConstraints = new List<IConstraint>();
                // 添加基本的约束，如教师/教室冲突约束
                return new SmartSchedulingSystem.Scheduling.Engine.ConstraintManager(
                    tempConstraints, 
                    LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning)).CreateLogger<ConstraintManager>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取约束管理器时发生异常");
                return null;
            }
        }
        
        /// <summary>
        /// 评估解决方案的质量，计算各种约束违反情况
        /// </summary>
        public double EvaluateSolutionQuality(SchedulingSolution solution, SchedulingProblem problem)
        {
            try
            {
                _logger.LogInformation($"开始评估解决方案质量，包含 {solution.Assignments.Count} 个分配");
                
                // 初始化总分和违反计数
                double totalScore = 100.0; // 从满分开始，扣除违反项
                int violationCount = 0;
                
                // 记录已使用的资源，用于检测冲突
                var usedRoomTimes = new HashSet<(int roomId, int timeSlotId)>();
                var usedTeacherTimes = new HashSet<(int teacherId, int timeSlotId)>();
                var courseSectionTeachers = new Dictionary<string, int>(); // 记录每个课程代码对应的教师
                
                // 各类违反的计数
                int capacityViolations = 0;
                int teacherQualificationViolations = 0;
                int sameCourseTeacherViolations = 0;
                int teacherAvailabilityViolations = 0;
                int roomAvailabilityViolations = 0;
                int roomConflictViolations = 0;
                int teacherConflictViolations = 0;
                
                // 对每个分配进行检查
                foreach (var assignment in solution.Assignments)
                {
                    // 1. 检查教室容量是否足够
                    if (assignment.Classroom.Capacity < assignment.CourseSection.Enrollment)
                    {
                        capacityViolations++;
                        _logger.LogWarning($"教室容量违反: 教室 {assignment.Classroom.Name} (容量: {assignment.Classroom.Capacity}) 不足以容纳课程 {assignment.CourseSection.CourseName} (学生数: {assignment.CourseSection.Enrollment})");
                    }
                    
                    // 2. 检查教师资质
                    if (!IsTeacherQualified(problem, assignment.Teacher.Id, assignment.CourseSection.CourseId))
                    {
                        teacherQualificationViolations++;
                        _logger.LogWarning($"教师资质违反: 教师 {assignment.Teacher.Name} 可能不具备教授课程 {assignment.CourseSection.CourseName} 的足够资质");
                    }
                    
                    // 3. 确保同一课程的不同班级由同一教师教授
                    if (courseSectionTeachers.TryGetValue(assignment.CourseSection.CourseCode, out int existingTeacherId))
                    {
                        if (existingTeacherId != assignment.Teacher.Id)
                        {
                            sameCourseTeacherViolations++;
                            _logger.LogWarning($"同一课程不同教师违反: 课程 {assignment.CourseSection.CourseCode} 的不同班级由不同教师教授");
                        }
                    }
                    else
                    {
                        courseSectionTeachers[assignment.CourseSection.CourseCode] = assignment.Teacher.Id;
                    }
                    
                    // 4. 检查教师在该时间段是否可用
                    var teacherUnavailable = problem.TeacherAvailabilities
                        .Any(ta => ta.TeacherId == assignment.Teacher.Id && 
                                 ta.TimeSlotId == assignment.TimeSlot.Id && 
                                 !ta.IsAvailable);
                                 
                    if (teacherUnavailable)
                    {
                        teacherAvailabilityViolations++;
                        _logger.LogWarning($"教师时间冲突: 教师 {assignment.Teacher.Name} 在时间段 {assignment.TimeSlot.DayName} {assignment.TimeSlot.StartTime} 不可用");
                    }
                    
                    // 5. 检查教室在该时间段是否可用
                    var roomUnavailable = problem.ClassroomAvailabilities
                        .Any(ca => ca.ClassroomId == assignment.Classroom.Id && 
                                ca.TimeSlotId == assignment.TimeSlot.Id && 
                                !ca.IsAvailable);
                                
                    if (roomUnavailable)
                    {
                        roomAvailabilityViolations++;
                        _logger.LogWarning($"教室时间冲突: 教室 {assignment.Classroom.Name} 在时间段 {assignment.TimeSlot.DayName} {assignment.TimeSlot.StartTime} 不可用");
                    }
                    
                    // 6. 检查教室冲突（同一时间段一个教室只能安排一个课程）
                    var roomTimeKey = (assignment.Classroom.Id, assignment.TimeSlot.Id);
                    if (usedRoomTimes.Contains(roomTimeKey))
                    {
                        roomConflictViolations++;
                        _logger.LogWarning($"教室冲突: 教室 {assignment.Classroom.Name} 在时间段 {assignment.TimeSlot.DayName} {assignment.TimeSlot.StartTime} 已被其他课程使用");
                    }
                    else
                    {
                        usedRoomTimes.Add(roomTimeKey);
                    }
                    
                    // 7. 检查教师冲突（同一时间段一个教师只能教授一个课程）
                    var teacherTimeKey = (assignment.Teacher.Id, assignment.TimeSlot.Id);
                    if (usedTeacherTimes.Contains(teacherTimeKey))
                    {
                        teacherConflictViolations++;
                        _logger.LogWarning($"教师冲突: 教师 {assignment.Teacher.Name} 在时间段 {assignment.TimeSlot.DayName} {assignment.TimeSlot.StartTime} 已被安排教授其他课程");
                    }
                    else
                    {
                        usedTeacherTimes.Add(teacherTimeKey);
                    }
                }
                
                // 计算总违反数
                violationCount = capacityViolations + teacherQualificationViolations + sameCourseTeacherViolations
                                + teacherAvailabilityViolations + roomAvailabilityViolations 
                                + roomConflictViolations + teacherConflictViolations;
                
                // 根据违反数扣减分数
                // 硬约束违反每项扣10分
                double hardConstraintPenalty = (roomConflictViolations + teacherConflictViolations) * 10.0;
                // 软约束违反每项扣5分
                double softConstraintPenalty = (capacityViolations + teacherQualificationViolations + 
                                              sameCourseTeacherViolations + teacherAvailabilityViolations + 
                                              roomAvailabilityViolations) * 5.0;
                
                totalScore = Math.Max(0, totalScore - hardConstraintPenalty - softConstraintPenalty);
                
                // 日志记录评估结果
                _logger.LogInformation($"解决方案质量评估结果:");
                _logger.LogInformation($"- 教室容量违反: {capacityViolations}");
                _logger.LogInformation($"- 教师资质违反: {teacherQualificationViolations}");
                _logger.LogInformation($"- 同一课程不同教师违反: {sameCourseTeacherViolations}");
                _logger.LogInformation($"- 教师可用性违反: {teacherAvailabilityViolations}");
                _logger.LogInformation($"- 教室可用性违反: {roomAvailabilityViolations}");
                _logger.LogInformation($"- 教室冲突违反: {roomConflictViolations}");
                _logger.LogInformation($"- 教师冲突违反: {teacherConflictViolations}");
                _logger.LogInformation($"- 总违反数: {violationCount}");
                _logger.LogInformation($"- 最终得分: {totalScore}/100");
                
                return totalScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "评估解决方案质量时发生异常");
                return 0.0; // 出现异常返回最低分
            }
        }
        
        /// <summary>
        /// 检查教师是否有资格教授特定课程
        /// </summary>
        private bool IsTeacherQualified(SchedulingProblem problem, int teacherId, int courseId)
        {
            // 查找教师对该课程的资质偏好
            var preference = problem.TeacherCoursePreferences
                .FirstOrDefault(tcp => tcp.TeacherId == teacherId && tcp.CourseId == courseId);
                
            // 如果没有特定偏好记录，假设教师不具备资质
            if (preference == null)
            {
                return false;
            }
            
            // 查找课程的难度级别（如果有的话）
            var courseDifficulty = 1; // 默认难度级别
            var courseInfo = problem.CourseSections
                .FirstOrDefault(cs => cs.CourseId == courseId);
                
            if (courseInfo != null)
            {
                courseDifficulty = courseInfo.DifficultyLevel;
            }
            
            // 检查教师资质是否符合或超过课程难度
            return preference.ProficiencyLevel >= courseDifficulty;
        }
        
        /// <summary>
        /// 基本级别分配课程，只考虑资源冲突避免（教师/教室冲突、教室容量）
        /// </summary>
        private bool TryAssignWithBasicConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            HashSet<(int roomId, int timeSlotId)> usedRoomTimes,
            HashSet<(int teacherId, int timeSlotId)> usedTeacherTimes,
            Dictionary<string, int> courseTeacherMap,
            SchedulingSolution solution,
            Random random)
        {
            try
            {
                _logger.LogDebug($"使用基本约束为课程 {section.Id} ({section.CourseName}) 分配资源");
                
                // 筛选容量足够的教室
                var suitableRooms = problem.Classrooms
                    .Where(room => room.Capacity >= section.Enrollment)
                    .ToList();
                
                // 如果没有容量足够的教室，选择容量最大的教室
                if (suitableRooms.Count == 0)
                {
                    suitableRooms = problem.Classrooms
                        .OrderByDescending(room => room.Capacity)
                        .Take(1)
                        .ToList();
                    
                    _logger.LogWarning($"课程 {section.Id} 无法找到容量足够的教室，选择了容量最大的教室");
                }
                
                // 准备所有教师列表
                var teachers = problem.Teachers.ToList();
                
                // 随机尝试多次找到可行的分配
                int maxAttempts = 15;
                
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // 随机选择时间槽
                    var timeSlots = problem.TimeSlots.ToList();
                    var selectedTimeSlot = timeSlots[random.Next(timeSlots.Count)];
                    
                    // 筛选未被占用的教室
                    var availableRooms = suitableRooms
                        .Where(room => !usedRoomTimes.Contains((room.Id, selectedTimeSlot.Id)))
                        .ToList();
                    
                    if (availableRooms.Count == 0) 
                    {
                        // 没有可用教室，尝试下一个时间槽
                        continue;
                    }
                    
                    // 随机选择一个可用教室
                    var selectedRoom = availableRooms[random.Next(availableRooms.Count)];
                    
                    // 筛选未被占用的教师
                    var availableTeachers = teachers
                        .Where(teacher => !usedTeacherTimes.Contains((teacher.Id, selectedTimeSlot.Id)))
                        .ToList();
                    
                    if (availableTeachers.Count == 0) 
                    {
                        // 没有可用教师，尝试下一个时间槽
                        continue;
                    }
                    
                    // 随机选择一个可用教师
                    var selectedTeacher = availableTeachers[random.Next(availableTeachers.Count)];
                    
                    // 创建分配
                    var courseAssignment = new CourseAssignment(section, selectedTeacher, selectedRoom, selectedTimeSlot);
                    
                    // 转换为SchedulingAssignment并添加到解中
                    solution.Assignments.Add(courseAssignment.ToSchedulingAssignment(problem));
                    
                    // 更新已使用的资源
                    usedRoomTimes.Add((selectedRoom.Id, selectedTimeSlot.Id));
                    usedTeacherTimes.Add((selectedTeacher.Id, selectedTimeSlot.Id));
                    
                    _logger.LogDebug($"成功为课程 {section.Id} 分配资源: 时间={selectedTimeSlot.DayName} {selectedTimeSlot.StartTime}, 教室={selectedRoom.Name}, 教师={selectedTeacher.Name}");
                    
                    return true;
                }
                
                _logger.LogWarning($"尝试 {maxAttempts} 次后，无法为课程 {section.Id} 找到符合基本约束的分配方案");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"尝试为课程 {section.Id} 分配资源时发生异常");
                return false;
            }
        }
        
        /// <summary>
        /// 考虑所有硬约束分配课程
        /// </summary>
        private bool TryAssignWithAllHardConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            HashSet<(int roomId, int timeSlotId)> usedRoomTimes,
            HashSet<(int teacherId, int timeSlotId)> usedTeacherTimes,
            Dictionary<string, int> courseTeacherMap,
            SchedulingSolution solution,
            Random random)
        {
            try
            {
                // 基本约束检查
                var coreConstraintsSatisfied = TryAssignWithCoreConstraints(problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                
                if (!coreConstraintsSatisfied)
                {
                    return false;
                }
                
                // 获取最新分配的课程
                var assignment = solution.Assignments.Last();
                
                // 检查额外的可变硬约束
                
                // 1. 教师可用性
                bool teacherAvailable = true;
                if (assignment.Teacher != null && assignment.TimeSlot != null)
                {
                    teacherAvailable = !problem.TeacherAvailabilities.Any(ta => 
                    ta.TeacherId == assignment.Teacher.Id && 
                    ta.TimeSlotId == assignment.TimeSlot.Id && 
                    !ta.IsAvailable);
                }
                
                if (!teacherAvailable)
                {
                    // 移除分配，标记为失败
                    solution.Assignments.RemoveAt(solution.Assignments.Count - 1);
                    if (assignment.Classroom != null && assignment.TimeSlot != null)
                    usedRoomTimes.Remove((assignment.Classroom.Id, assignment.TimeSlot.Id));
                    if (assignment.Teacher != null && assignment.TimeSlot != null)
                    usedTeacherTimes.Remove((assignment.Teacher.Id, assignment.TimeSlot.Id));
                    return false;
                }
                
                // 2. 教室可用性
                bool roomAvailable = true;
                if (assignment.Classroom != null && assignment.TimeSlot != null)
                {
                    roomAvailable = !problem.ClassroomAvailabilities.Any(ca => 
                    ca.ClassroomId == assignment.Classroom.Id && 
                    ca.TimeSlotId == assignment.TimeSlot.Id && 
                    !ca.IsAvailable);
                }
                
                if (!roomAvailable)
                {
                    // 移除分配，标记为失败
                    solution.Assignments.RemoveAt(solution.Assignments.Count - 1);
                    if (assignment.Classroom != null && assignment.TimeSlot != null)
                    usedRoomTimes.Remove((assignment.Classroom.Id, assignment.TimeSlot.Id));
                    if (assignment.Teacher != null && assignment.TimeSlot != null)
                    usedTeacherTimes.Remove((assignment.Teacher.Id, assignment.TimeSlot.Id));
                    return false;
                }
                
                // 3. 教师资质约束
                bool teacherQualified = true;
                if (assignment.Teacher != null && section != null)
                {
                    teacherQualified = problem.TeacherCoursePreferences.Any(tcp => 
                    tcp.TeacherId == assignment.Teacher.Id && 
                    tcp.CourseId == section.CourseId && 
                    tcp.ProficiencyLevel >= 2);
                }
                
                if (!teacherQualified)
                {
                    // 移除分配，标记为失败
                    solution.Assignments.RemoveAt(solution.Assignments.Count - 1);
                    if (assignment.Classroom != null && assignment.TimeSlot != null)
                    usedRoomTimes.Remove((assignment.Classroom.Id, assignment.TimeSlot.Id));
                    if (assignment.Teacher != null && assignment.TimeSlot != null)
                    usedTeacherTimes.Remove((assignment.Teacher.Id, assignment.TimeSlot.Id));
                    return false;
                }
                
                // 所有硬约束都满足
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"尝试分配课程 {section.Id} 时发生异常");
                return false;
            }
        }
        
        /// <summary>
        /// 尝试使用基本排课规则分配课程
        /// </summary>
        private bool TryAssignWithCoreConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            HashSet<(int roomId, int timeSlotId)> usedRoomTimes,
            HashSet<(int teacherId, int timeSlotId)> usedTeacherTimes,
            Dictionary<string, int> courseTeacherMap,
            SchedulingSolution solution,
            Random random)
        {
            // 首先筛选容量足够的教室（满足教室容量约束）
            var suitableRooms = problem.Classrooms
                .Where(room => room.Capacity >= section.Enrollment)
                .ToList();
            
            if (suitableRooms.Count == 0)
            {
                _logger.LogWarning($"课程 {section.Id} ({section.CourseName}) 没有找到容量足够的教室");
                return false;
            }
            
            // 筛选有资格教授的教师
            var qualifiedTeachers = problem.Teachers.ToList();
            var teacherPreferences = problem.TeacherCoursePreferences
                .Where(tcp => tcp.CourseId == section.CourseId && tcp.ProficiencyLevel >= 2)
                .ToList();
            
            if (teacherPreferences.Count > 0)
            {
                var preferredTeacherIds = teacherPreferences.Select(tp => tp.TeacherId).ToHashSet();
                qualifiedTeachers = problem.Teachers
                    .Where(t => preferredTeacherIds.Contains(t.Id))
                    .ToList();
            }
            
            // 检查同一课程的不同班级是否应由同一教师教授
            if (courseTeacherMap.TryGetValue(section.CourseCode, out int existingTeacherId))
            {
                qualifiedTeachers = qualifiedTeachers.Where(t => t.Id == existingTeacherId).ToList();
                if (qualifiedTeachers.Count == 0)
                {
                    _logger.LogWarning($"课程 {section.CourseCode} 的先前班级已分配教师 {existingTeacherId}，但当前班级没有此教师可用");
                    return false;
                }
            }
            
            // 随机尝试 20 次找到可行的分配
            int maxAttempts = 20;
            
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 计算每个时间槽被选中的概率权重
                var timeSlotCandidates = problem.TimeSlots.ToList();
                var timeSlotWeights = CalculateTimeSlotWeights(timeSlotCandidates);
                
                // 根据权重随机选择时间槽
                TimeSlotInfo selectedTimeSlot = SelectRandomByWeight(timeSlotCandidates, timeSlotWeights, random);
                
                // 检查教室冲突（教室在此时间段是否已被使用）
                var availableRooms = suitableRooms
                    .Where(room => !usedRoomTimes.Contains((room.Id, selectedTimeSlot.Id)))
                    .ToList();
                
                if (availableRooms.Count == 0) continue; // 没有可用教室，尝试下一次
                
                var selectedRoom = availableRooms[random.Next(availableRooms.Count)];
                
                // 检查教师冲突（教师在此时间段是否已被安排）
                var availableTeachers = qualifiedTeachers
                    .Where(teacher => !usedTeacherTimes.Contains((teacher.Id, selectedTimeSlot.Id)))
                    .ToList();
                
                if (availableTeachers.Count == 0) continue; // 没有可用教师，尝试下一次
                
                var selectedTeacher = availableTeachers[random.Next(availableTeachers.Count)];
                
                // 创建分配
                var courseAssignment = new CourseAssignment(section, selectedTeacher, selectedRoom, selectedTimeSlot);
                
                // 转换为SchedulingAssignment并添加到解中
                solution.Assignments.Add(courseAssignment.ToSchedulingAssignment(problem));
                
                // 更新已使用的资源
                usedRoomTimes.Add((selectedRoom.Id, selectedTimeSlot.Id));
                usedTeacherTimes.Add((selectedTeacher.Id, selectedTimeSlot.Id));
                
                // 记录课程-教师映射
                if (!courseTeacherMap.ContainsKey(section.CourseCode))
                {
                    courseTeacherMap[section.CourseCode] = selectedTeacher.Id;
                }
                
                return true;
            }
            
            return false; // 尝试失败
        }
        
        /// <summary>
        /// 标准级别约束分配课程，考虑教师资质、教室容量、教师偏好等标准约束
        /// </summary>
        private bool TryAssignWithStandardConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            HashSet<(int roomId, int timeSlotId)> usedRoomTimes,
            HashSet<(int teacherId, int timeSlotId)> usedTeacherTimes,
            Dictionary<string, int> courseTeacherMap,
            SchedulingSolution solution,
            Random random)
        {
            try
            {
                _logger.LogDebug($"使用标准约束为课程 {section.Id} ({section.CourseName}) 分配资源");
                
                // 筛选容量足够的教室
            var suitableRooms = problem.Classrooms
                .Where(room => room.Capacity >= section.Enrollment)
                .ToList();
                
            if (suitableRooms.Count == 0)
            {
                    _logger.LogWarning($"课程 {section.Id} 没有找到容量足够的教室");
                    return false;
                }
                
                // 筛选有资格教授的教师
            var qualifiedTeachers = problem.Teachers.ToList();
            var teacherPreferences = problem.TeacherCoursePreferences
                .Where(tcp => tcp.CourseId == section.CourseId && tcp.ProficiencyLevel >= 1)
                .ToList();
            
            if (teacherPreferences.Count > 0)
            {
                var preferredTeacherIds = teacherPreferences.Select(tp => tp.TeacherId).ToHashSet();
                    qualifiedTeachers = problem.Teachers
                    .Where(t => preferredTeacherIds.Contains(t.Id))
                    .ToList();
                    
                    if (qualifiedTeachers.Count == 0)
                    {
                        _logger.LogWarning($"课程 {section.Id} 没有找到合格的教师");
                        qualifiedTeachers = problem.Teachers.ToList(); // 回退到所有教师
                    }
                }
                
                // 检查同一课程的不同班级是否应由同一教师教授
                if (courseTeacherMap.TryGetValue(section.CourseCode, out int existingTeacherId))
                {
                    var sameTeachers = qualifiedTeachers.Where(t => t.Id == existingTeacherId).ToList();
                    if (sameTeachers.Count > 0)
                    {
                        qualifiedTeachers = sameTeachers;
                        _logger.LogDebug($"课程 {section.CourseCode} 的先前班级已分配教师 {existingTeacherId}，将使用相同教师");
                    }
                }
                
                // 随机尝试多次找到可行的分配
                int maxAttempts = 30;
                
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // 随机选择时间槽
                    var timeSlots = problem.TimeSlots.ToList();
                    var selectedTimeSlot = timeSlots[random.Next(timeSlots.Count)];
                    
                    // 筛选未被占用的教室
                    var availableRooms = suitableRooms
                        .Where(room => !usedRoomTimes.Contains((room.Id, selectedTimeSlot.Id)))
                        .ToList();
                    
                    if (availableRooms.Count == 0) 
                    {
                        // 没有可用教室，尝试下一个时间槽
                        continue;
                    }
                    
                    // 筛选教室可用性
                    availableRooms = availableRooms
                        .Where(room => !problem.ClassroomAvailabilities.Any(ca => 
                            ca.ClassroomId == room.Id && 
                            ca.TimeSlotId == selectedTimeSlot.Id && 
                            !ca.IsAvailable))
                        .ToList();
                        
                    if (availableRooms.Count == 0) 
                    {
                        // 没有可用教室，尝试下一个时间槽
                        continue;
                    }
                    
                    // 随机选择一个可用教室
                    var selectedRoom = availableRooms[random.Next(availableRooms.Count)];
                    
                    // 筛选未被占用的教师
                    var availableTeachers = qualifiedTeachers
                        .Where(teacher => !usedTeacherTimes.Contains((teacher.Id, selectedTimeSlot.Id)))
                        .ToList();
                        
                    if (availableTeachers.Count == 0) 
                    {
                        // 没有可用教师，尝试下一个时间槽
                        continue;
                    }
                    
                    // 筛选教师可用性
                    availableTeachers = availableTeachers
                        .Where(teacher => !problem.TeacherAvailabilities.Any(ta => 
                            ta.TeacherId == teacher.Id && 
                            ta.TimeSlotId == selectedTimeSlot.Id && 
                            !ta.IsAvailable))
                    .ToList();

                    if (availableTeachers.Count == 0) 
                    {
                        // 没有可用教师，尝试下一个时间槽
                        continue;
                    }
                    
                    // 优先选择课程偏好较高的教师
                    var preferredAvailableTeachers = availableTeachers
                        .Where(teacher => teacherPreferences.Any(tp => 
                            tp.TeacherId == teacher.Id && 
                            tp.CourseId == section.CourseId && 
                            tp.ProficiencyLevel >= 2))
                    .ToList();

                    if (preferredAvailableTeachers.Count > 0)
                    {
                        availableTeachers = preferredAvailableTeachers;
                    }
                    
                    // 随机选择一个可用教师
                    var selectedTeacher = availableTeachers[random.Next(availableTeachers.Count)];
                    
                    // 创建分配
                    var courseAssignment = new CourseAssignment(section, selectedTeacher, selectedRoom, selectedTimeSlot);
                    
                    // 转换为SchedulingAssignment并添加到解中
                    solution.Assignments.Add(courseAssignment.ToSchedulingAssignment(problem));
                    
                    // 更新已使用的资源
                    usedRoomTimes.Add((selectedRoom.Id, selectedTimeSlot.Id));
                    usedTeacherTimes.Add((selectedTeacher.Id, selectedTimeSlot.Id));
                    
                    // 更新课程-教师映射
                    if (!courseTeacherMap.ContainsKey(section.CourseCode))
                    {
                        courseTeacherMap[section.CourseCode] = selectedTeacher.Id;
                    }
                    
                    _logger.LogDebug($"成功为课程 {section.Id} 分配资源: 时间={selectedTimeSlot.DayName} {selectedTimeSlot.StartTime}, 教室={selectedRoom.Name}, 教师={selectedTeacher.Name}");
                    
                    return true;
                }
                
                _logger.LogWarning($"尝试 {maxAttempts} 次后，无法为课程 {section.Id} 找到符合标准约束的分配方案");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"尝试为课程 {section.Id} 分配资源时发生异常");
                return false;
            }
        }

        /// <summary>
        /// 忽略大部分约束，强制分配课程（仅保留最低限度的约束）
        /// </summary>
        private void ForceAssignmentWithMinimalConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            SchedulingSolution solution,
            Random random)
        {
            try
            {
                // 随机选择时间槽（无偏向）
                var timeSlotCandidates = problem.TimeSlots.ToList();
                var selectedTimeSlot = timeSlotCandidates[random.Next(timeSlotCandidates.Count)];
                
                // 尽量选择容量足够的教室
                var suitableRooms = problem.Classrooms
                    .Where(room => room.Capacity >= section.Enrollment)
                    .ToList();
                    
                if (suitableRooms.Count == 0)
                {
                    suitableRooms = problem.Classrooms
                        .OrderByDescending(room => room.Capacity)
                        .Take(1)
                        .ToList();
                }
                
                var selectedRoom = suitableRooms[random.Next(suitableRooms.Count)];
                
                // 尽量选择有资格的教师
                var qualifiedTeachers = problem.Teachers.ToList();
                var teacherPreferences = problem.TeacherCoursePreferences
                    .Where(tcp => tcp.CourseId == section.CourseId && tcp.ProficiencyLevel >= 1)
                    .ToList();
                
                if (teacherPreferences.Count > 0)
                {
                    var preferredTeacherIds = teacherPreferences.Select(tp => tp.TeacherId).ToHashSet();
                    var preferredTeachers = problem.Teachers
                        .Where(t => preferredTeacherIds.Contains(t.Id))
                        .ToList();
                        
                    if (preferredTeachers.Count > 0)
                    {
                        qualifiedTeachers = preferredTeachers;
                    }
                }
                
                // 优先选择在当前时间段可用的教师
                var availableTeachers = qualifiedTeachers
                    .Where(teacher => !problem.TeacherAvailabilities.Any(ta => 
                        ta.TeacherId == teacher.Id && 
                        ta.TimeSlotId == selectedTimeSlot.Id && 
                        !ta.IsAvailable))
                    .ToList();

                TeacherInfo selectedTeacher;
                if (availableTeachers.Count > 0)
                {
                    selectedTeacher = availableTeachers[random.Next(availableTeachers.Count)];
                    _logger.LogInformation($"在最小约束模式下，找到在时间槽 {selectedTimeSlot.Id} 可用的教师 {selectedTeacher.Id}");
                }
                else
                {
                    // 如果没有满足可用性约束的教师，回退到所有教师
                    selectedTeacher = qualifiedTeachers[random.Next(qualifiedTeachers.Count)];
                    _logger.LogWarning($"在最小约束模式下，未找到在时间槽 {selectedTimeSlot.Id} 可用的教师，随机选择教师 {selectedTeacher.Id}，可能违反可用性约束");
                }
                
                // 创建分配
                var courseAssignment = new CourseAssignment(section, selectedTeacher, selectedRoom, selectedTimeSlot);
                
                _logger.LogWarning($"强制分配课程 {section.Id} 到时间槽 {selectedTimeSlot.Id}，教室 {selectedRoom.Id}，教师 {selectedTeacher.Id}");
                
                // 转换为SchedulingAssignment并添加到解中
                solution.Assignments.Add(courseAssignment.ToSchedulingAssignment(problem));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"强制分配课程 {section.Id} 时发生异常");
                
                // 即使出现异常，也尝试创建一个简单的分配
                try
                {
                    var timeSlot = problem.TimeSlots.ElementAt(random.Next(problem.TimeSlots.Count()));
                    var room = problem.Classrooms.ElementAt(random.Next(problem.Classrooms.Count()));
                    var teacher = problem.Teachers.ElementAt(random.Next(problem.Teachers.Count()));
                    
                    var courseAssignment = new CourseAssignment(section, teacher, room, timeSlot);
                    
                    solution.Assignments.Add(courseAssignment.ToSchedulingAssignment(problem));
                    _logger.LogWarning($"异常恢复：为课程 {section.Id} 创建了应急分配方案");
                }
                catch
                {
                    _logger.LogError($"无法为课程 {section.Id} 创建任何分配方案，该课程将被忽略");
                }
            }
        }
        
        /// <summary>
        /// 计算每个时间槽的选择权重
        /// </summary>
        private List<double> CalculateTimeSlotWeights(List<TimeSlotInfo> timeSlots)
        {
            // 简单实现：所有时间槽权重相等
            return timeSlots.Select(_ => 1.0).ToList();
        }

        /// <summary>
        /// 根据权重随机选择元素
        /// </summary>
        private T SelectRandomByWeight<T>(List<T> items, List<double> weights, Random random)
        {
            if (items.Count == 0) 
                throw new ArgumentException("项目列表不能为空");
            
            if (items.Count != weights.Count)
                throw new ArgumentException("项目数量和权重数量必须相同");
            
            // 简单实现：如果所有权重相等，直接随机选择
            if (weights.All(w => Math.Abs(w - weights[0]) < 0.0001))
            {
                return items[random.Next(items.Count)];
            }
            
            // 计算权重总和
            double totalWeight = weights.Sum();
            // 随机选择一个权重位置
            double randomValue = random.NextDouble() * totalWeight;
            
            // 找到对应的项目
            double cumulativeWeight = 0;
            for (int i = 0; i < items.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue <= cumulativeWeight)
                    return items[i];
            }
            
            // 如果出现浮点误差，返回最后一项
            return items[items.Count - 1];
        }

        /// <summary>
        /// 验证问题数据的完整性和有效性
        /// </summary>
        private void ValidateProblemData(SchedulingProblem problem)
        {
            if (problem == null)
                throw new ArgumentNullException(nameof(problem), "排课问题数据不能为空");
                
            if (problem.CourseSections == null || !problem.CourseSections.Any())
                throw new ArgumentException("排课问题必须包含至少一个课程班级", nameof(problem));
                
            if (problem.Teachers == null || !problem.Teachers.Any())
                throw new ArgumentException("排课问题必须包含至少一名教师", nameof(problem));
                
            if (problem.Classrooms == null || !problem.Classrooms.Any())
                throw new ArgumentException("排课问题必须包含至少一个教室", nameof(problem));
                
            if (problem.TimeSlots == null || !problem.TimeSlots.Any())
                throw new ArgumentException("排课问题必须包含至少一个时间槽", nameof(problem));
                
            _logger.LogInformation($"问题数据验证通过: {problem.CourseSections.Count()} 个课程班级, " +
                                 $"{problem.Teachers.Count()} 名教师, " +
                                 $"{problem.Classrooms.Count()} 个教室, " +
                                 $"{problem.TimeSlots.Count()} 个时间槽");
        }
        
        /// <summary>
        /// 调试输出问题数据
        /// </summary>
        private void DebugProblemData(SchedulingProblem problem)
        {
            _logger.LogDebug($"问题详情: {problem.Name}");
            _logger.LogDebug($"课程班级数量: {problem.CourseSections.Count()}");
            _logger.LogDebug($"教师数量: {problem.Teachers.Count()}");
            _logger.LogDebug($"教室数量: {problem.Classrooms.Count()}");
            _logger.LogDebug($"时间槽数量: {problem.TimeSlots.Count()}");
            _logger.LogDebug($"教师课程偏好数量: {problem.TeacherCoursePreferences.Count()}");
            _logger.LogDebug($"教师可用性数量: {problem.TeacherAvailabilities.Count()}");
            _logger.LogDebug($"教室可用性数量: {problem.ClassroomAvailabilities.Count()}");
        }
        
        /// <summary>
        /// 从CP模型中提取变量字典
        /// </summary>
        private Dictionary<string, IntVar> ExtractVariablesDictionary(CpModel model)
        {
            // 简化实现，实际应该从模型中提取变量
            return new Dictionary<string, IntVar>();
        }
        
        /// <summary>
        /// 收集部分解决方案
        /// </summary>
        private SchedulingSolution CollectPartialSolution(SchedulingProblem problem, CpModel model, CpSolver solver)
        {
            // 创建一个空的解决方案
            var solution = new SchedulingSolution
            {
                Id = new Random().Next(1, 1000000),  // 使用随机数作为ID，而不是GUID字符串
                ProblemId = problem.Id,
                Name = $"部分解决方案_{DateTime.Now:yyyyMMdd_HHmmss}",
                ConstraintLevel = ConstraintApplicationLevel.Basic,
                Algorithm = "CP_Partial"
            };
            
            try
            {
                // 使用随机分配作为备选
                var random = new Random();
                
                // 为每个课程尝试创建一个最基本的分配
                foreach (var section in problem.CourseSections)
                {
                    try
                    {
                        // 随机选择资源
                        var timeSlot = problem.TimeSlots.ElementAt(random.Next(problem.TimeSlots.Count()));
                        var room = problem.Classrooms.ElementAt(random.Next(problem.Classrooms.Count()));
                        var teacher = problem.Teachers.ElementAt(random.Next(problem.Teachers.Count()));
                        
                        // 创建一个简单的分配
                        var assignment = new SchedulingAssignment
                        {
                            SectionId = section.Id,
                            CourseSection = section,
                            TeacherId = teacher.Id,
                            Teacher = teacher,
                            ClassroomId = room.Id,
                            Classroom = room,
                            TimeSlotId = timeSlot.Id,
                            TimeSlot = timeSlot
                        };
                        
                        solution.Assignments.Add(assignment);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"为课程 {section.Id} 创建部分解时出错");
                    }
                }
                
                _logger.LogInformation($"创建了部分解决方案，包含 {solution.Assignments.Count} 个分配");
                return solution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建部分解决方案时出错");
                return null;
            }
        }

        /// <summary>
        /// 检查解决方案的可行性
        /// </summary>
        public bool CheckFeasibility(SchedulingSolution solution, SchedulingProblem problem)
        {
            if (solution == null || problem == null)
                return false;
                
            try
            {
                // 记录已使用的资源，用于检测冲突
                var usedRoomTimes = new HashSet<(int roomId, int timeSlotId)>();
                var usedTeacherTimes = new HashSet<(int teacherId, int timeSlotId)>();
                
                // 检查每个分配
                foreach (var assignment in solution.Assignments)
                {
                    // 检查教室冲突
                    var roomTimeKey = (assignment.ClassroomId, assignment.TimeSlotId);
                    if (usedRoomTimes.Contains(roomTimeKey))
                    {
                        _logger.LogWarning($"教室冲突: 教室 {assignment.ClassroomId} 在时间段 {assignment.TimeSlotId} 被多次使用");
                        return false;
                    }
                    
                    // 检查教师冲突
                    var teacherTimeKey = (assignment.TeacherId, assignment.TimeSlotId);
                    if (usedTeacherTimes.Contains(teacherTimeKey))
                    {
                        _logger.LogWarning($"教师冲突: 教师 {assignment.TeacherId} 在时间段 {assignment.TimeSlotId} 被多次安排");
                        return false;
                    }
                    
                    // 记录已使用的资源
                    usedRoomTimes.Add(roomTimeKey);
                    usedTeacherTimes.Add(teacherTimeKey);
                }
                
                _logger.LogInformation("解决方案可行性检查通过");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查解决方案可行性时出错");
                return false;
            }
        }
    }
}