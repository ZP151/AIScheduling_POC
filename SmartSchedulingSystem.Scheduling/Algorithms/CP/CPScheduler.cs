using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

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
        private readonly SchedulingParameters _parameters;

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

                // 创建CP模型
                var model = _modelBuilder.BuildModel(problem);

                _logger.LogInformation($"CP模型构建完成，耗时：{sw.ElapsedMilliseconds}ms");

                // 创建CP求解器
                var solver = new CpSolver();
                Console.WriteLine("求解器参数设置...");

                // 配置求解器
                solver.StringParameters = $"num_search_workers:{Math.Max(1, Environment.ProcessorCount / 2)}"; // 使用部分CPU核心

                if (_parameters.CpTimeLimit > 0)
                {
                    solver.StringParameters += $";max_time_in_seconds:{_parameters.CpTimeLimit}";
                    _logger.LogInformation($"设置CP求解时间限制：{_parameters.CpTimeLimit}秒");
                }
                Console.WriteLine($"求解器参数: {solver.StringParameters}");
                
                // 创建变量字典
                var variableDict = ExtractVariablesDictionary(model);
                Console.WriteLine($"变量字典大小: {variableDict.Count}");
                
                // 创建多样化解回调
                //var callback = new DiverseSolutionCallback(variableDict, solutionCount, model);

                // 使用基本回调代替DiverseSolutionCallback进行测试
                var callback = new CPSolutionCallback(variableDict, solutionCount);

                // 求解模型
                _logger.LogInformation("开始CP求解...");

                sw.Restart();

                Console.WriteLine("开始CP求解...");
                var status = solver.Solve(model, callback);
                Console.WriteLine($"CP求解完成，状态: {status}, 找到解数量: {callback.SolutionCount}");

                sw.Stop();
                _logger.LogInformation($"CP求解完成，状态：{status}，耗时：{sw.ElapsedMilliseconds}ms，找到解数量：{callback.SolutionCount}");

                //// 检查是否找到解
                //if (status == CpSolverStatus.Unknown)
                //{
                //    _logger.LogWarning("CP求解器无法在给定时间内找到解");
                //    return new List<SchedulingSolution>();
                //}

                //if (callback.SolutionCount == 0)
                //{
                //    _logger.LogWarning("CP求解器未找到满足所有硬约束的解");
                //    return new List<SchedulingSolution>();
                //}
                // 关键调试点：检查回调中的解
                if (callback.SolutionCount > 0)
                {
                    Console.WriteLine("解详情:");
                    foreach (var solution in callback.Solutions)
                    {
                        Console.WriteLine($"  解包含 {solution.Count} 个变量赋值");

                        // 查看值为1的变量
                        var assignments = solution.Where(kv => kv.Value == 1).ToList();
                        Console.WriteLine($"  其中 {assignments.Count} 个变量值为1");

                        foreach (var kvp in assignments)
                        {
                            Console.WriteLine($"    {kvp.Key} = {kvp.Value}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("未找到任何解!");
                }


                // 转换为排课系统解
                var solutions = new List<SchedulingSolution>();

                _logger.LogInformation($"开始转换CP解为排课系统解...");
                sw.Restart();

                foreach (var cpSolution in callback.Solutions)
                {
                    var solution = _solutionConverter.ConvertToSchedulingSolution(cpSolution, problem);
                    solution.Algorithm = "CP";
                    solutions.Add(solution);
                    Console.WriteLine($"转换后的解包含 {solution.Assignments.Count} 个分配");

                }

                sw.Stop();
                Console.WriteLine("============ CP求解结束 ============");
                _logger.LogInformation($"解转换完成，耗时：{sw.ElapsedMilliseconds}ms，共{solutions.Count}个解");

                return solutions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成初始解过程中发生异常");
                throw;
            }
        }

        private void ValidateProblemData(SchedulingProblem problem)
        {
            _logger.LogInformation("验证问题数据...");
            _logger.LogInformation($"课程班级数: {problem.CourseSections.Count}");
            _logger.LogInformation($"教师数: {problem.Teachers.Count}");
            _logger.LogInformation($"教室数: {problem.Classrooms.Count}");
            _logger.LogInformation($"时间槽数: {problem.TimeSlots.Count}");
            _logger.LogInformation($"教师课程偏好数: {problem.TeacherCoursePreferences.Count}");

            // 检查数据关联
            foreach (var section in problem.CourseSections)
            {
                var teachersForCourse = problem.TeacherCoursePreferences
                    .Where(tcp => tcp.CourseId == section.CourseId)
                    .Select(tcp => tcp.TeacherId)
                    .ToList();

                if (teachersForCourse.Count == 0)
                {
                    _logger.LogWarning($"警告: 课程 {section.CourseCode} 没有符合条件的教师!");
                }

                var suitableRooms = problem.Classrooms
                    .Where(r => r.Capacity >= section.Enrollment)
                    .ToList();

                if (suitableRooms.Count == 0)
                {
                    _logger.LogWarning($"警告: 课程 {section.CourseCode} (需容量: {section.Enrollment}) 没有容量足够的教室!");
                }
            }
        }

        private void DebugProblemData(SchedulingProblem problem)
        {
            _logger.LogInformation("=== 调试问题数据 ===");
            _logger.LogInformation($"问题ID: {problem.Id}, 名称: {problem.Name}");
            _logger.LogInformation($"课程数: {problem.CourseSections.Count}");
            if (problem.CourseSections.Count > 0)
            {
                _logger.LogInformation($"第一门课程: ID={problem.CourseSections[0].Id}, 名称={problem.CourseSections[0].CourseName}");
            }

            _logger.LogInformation($"教师数: {problem.Teachers.Count}");
            if (problem.Teachers.Count > 0)
            {
                _logger.LogInformation($"第一位教师: ID={problem.Teachers[0].Id}, 名称={problem.Teachers[0].Name}");
            }

            _logger.LogInformation($"教室数: {problem.Classrooms.Count}");
            if (problem.Classrooms.Count > 0)
            {
                _logger.LogInformation($"第一个教室: ID={problem.Classrooms[0].Id}, 名称={problem.Classrooms[0].Name}, 容量={problem.Classrooms[0].Capacity}");
            }

            _logger.LogInformation($"时间槽数: {problem.TimeSlots.Count}");
            if (problem.TimeSlots.Count > 0)
            {
                _logger.LogInformation($"第一个时间槽: ID={problem.TimeSlots[0].Id}, 日期={problem.TimeSlots[0].DayOfWeek}, 时间={problem.TimeSlots[0].StartTime}-{problem.TimeSlots[0].EndTime}");
            }

            _logger.LogInformation($"教师课程偏好数: {problem.TeacherCoursePreferences.Count}");
            if (problem.TeacherCoursePreferences.Count > 0)
            {
                var pref = problem.TeacherCoursePreferences[0];
                _logger.LogInformation($"第一个偏好: 教师ID={pref.TeacherId}, 课程ID={pref.CourseId}, 能力级别={pref.ProficiencyLevel}");
            }

            _logger.LogInformation("=== 调试结束 ===");
        }

        /// <summary>
        /// 从模型中提取变量字典
        /// </summary>
        private Dictionary<string, IntVar> ExtractVariablesDictionary(CpModel model)
        {
            // 注意：这是一个简化版，实际版本需要访问CpModel的内部变量
            // 在实际项目中，需要在ModelBuilder中保存变量映射并返回

            // 为了演示目的，我们返回一个空字典
            //_logger.LogWarning("使用空变量字典，这在实际项目中需要替换为真实实现");
            //return new Dictionary<string, IntVar>();

            return _modelBuilder.GetVariables();

        }

        /// <summary>
        /// 检查排课问题的可行性（是否存在满足所有硬约束的解）
        /// </summary>
        public bool CheckFeasibility(SchedulingProblem problem, out CpSolverStatus status,SchedulingParameters parameters = null)
        {
            try
            {
                _logger.LogInformation("开始检查排课问题可行性");

                var sw = Stopwatch.StartNew();

                // 创建CP模型
                var model = _modelBuilder.BuildModel(problem);

                _logger.LogInformation($"CP模型构建完成，耗时：{sw.ElapsedMilliseconds}ms");

                // 创建CP求解器
                var solver = new CpSolver();

                // 配置求解器（只用于快速找到一个可行解）
                int timeLimit = parameters?.CpTimeLimit ?? 60;
                solver.StringParameters = $"num_search_workers:8;max_time_in_seconds:{timeLimit};log_search_progress:true";

                // 求解模型
                _logger.LogInformation("开始CP可行性求解...");
                sw.Restart();

                status = solver.Solve(model);

                sw.Stop();
                _logger.LogInformation($"CP可行性检查完成，状态：{status}，耗时：{sw.ElapsedMilliseconds}ms");

                // 解释结果
                bool isFeasible = status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible;
                // 对于Unknown状态，增加一个松弛检查，看能否找到一个宽松模型的解
                if (status == CpSolverStatus.Unknown && parameters != null)
                {
                    _logger.LogInformation("使用松弛模型重新检查可行性...");

                    // 尝试一个更简单的模型
                    var relaxedModel = BuildRelaxedModel(problem);
                    status = solver.Solve(relaxedModel);

                    // 如果松弛模型有解，则认为原问题也可能有解
                    if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
                    {
                        _logger.LogInformation("松弛模型有解，认为原问题可能有解");
                        isFeasible = true;
                    }
                }
                switch (status)
                {
                    case CpSolverStatus.Optimal:
                    case CpSolverStatus.Feasible:
                        isFeasible = true;
                        _logger.LogInformation("排课问题有可行解");
                        break;
                    case CpSolverStatus.Infeasible:
                        _logger.LogInformation("排课问题无可行解，约束冲突");
                        // 添加更详细的问题诊断
                        _logger.LogWarning("问题被确定为不可行，详细诊断中...");

                        // 记录资源和约束的基本信息
                        _logger.LogWarning($"课程数量: {problem.CourseSections.Count}");
                        _logger.LogWarning($"教师数量: {problem.Teachers.Count}");
                        _logger.LogWarning($"教室数量: {problem.Classrooms.Count}");
                        _logger.LogWarning($"时间槽数量: {problem.TimeSlots.Count}");
                        _logger.LogWarning($"教师课程偏好数量: {problem.TeacherCoursePreferences.Count}");
                        _logger.LogWarning($"教师不可用时间段数量: {problem.TeacherAvailabilities.Count}");
                        _logger.LogWarning($"教室不可用时间段数量: {problem.ClassroomAvailabilities.Count}");

                        // 检查一些可能的问题原因
                        DiagnoseInfeasibilityReasons(problem);
                        break;
                    case CpSolverStatus.Unknown:
                        _logger.LogInformation("排课问题不确定是否有解，求解时间不足");
                        break;
                    case CpSolverStatus.ModelInvalid:
                        _logger.LogError("CP模型无效");
                        break;
                }

                return isFeasible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查可行性过程中发生异常");
                status = CpSolverStatus.Unknown;
                return false;
            }
        }
        private void DiagnoseInfeasibilityReasons(SchedulingProblem problem)
        {
            // 检查课程和教师的匹配情况
            foreach (var section in problem.CourseSections)
            {
                var qualifiedTeachers = problem.TeacherCoursePreferences
                    .Where(p => p.CourseId == section.CourseId && p.ProficiencyLevel >= 3)
                    .Select(p => p.TeacherId)
                    .ToList();

                if (qualifiedTeachers.Count == 0)
                {
                    _logger.LogError($"课程 {section.CourseName} (ID: {section.Id}) 没有合格的教师可以教授！");
                }
                else
                {
                    // 检查所有合格教师是否都有不可用时间冲突
                    bool allTeachersUnavailable = true;
                    foreach (var teacherId in qualifiedTeachers)
                    {
                        var unavailableTimes = problem.TeacherAvailabilities
                            .Where(a => a.TeacherId == teacherId && !a.IsAvailable)
                            .Select(a => a.TimeSlotId)
                            .ToHashSet();

                        if (unavailableTimes.Count < problem.TimeSlots.Count)
                        {
                            allTeachersUnavailable = false;
                            break;
                        }
                    }

                    if (allTeachersUnavailable)
                    {
                        _logger.LogError($"课程 {section.CourseName} (ID: {section.Id}) 的所有合格教师都没有可用时间段！");
                    }
                }
            }

            // 检查教室容量是否满足课程需求
            foreach (var section in problem.CourseSections)
            {
                var suitableRooms = problem.Classrooms
                    .Where(r => r.Capacity >= section.Enrollment)
                    .ToList();

                if (suitableRooms.Count == 0)
                {
                    _logger.LogError($"课程 {section.CourseName} (ID: {section.Id}, 学生数: {section.Enrollment}) 没有容量足够的教室！");
                }
                else if (section.RequiredRoomType != null && section.RequiredRoomType != "Regular")
                {
                    // 检查是否有符合要求类型的教室
                    var typeMatchingRooms = suitableRooms
                        .Where(r => r.Type == section.RequiredRoomType)
                        .ToList();

                    if (typeMatchingRooms.Count == 0)
                    {
                        _logger.LogError($"课程 {section.CourseName} (ID: {section.Id}) 需要类型为 {section.RequiredRoomType} 的教室，但没有符合条件的教室！");
                    }
                }
            }

            // 分析时间槽和教室的可用性
            if (problem.TimeSlots.Count < problem.CourseSections.Count)
            {
                _logger.LogError($"时间槽数量 ({problem.TimeSlots.Count}) 小于课程数量 ({problem.CourseSections.Count})，可能导致无法分配所有课程！");
            }

            if (problem.Classrooms.Count < problem.CourseSections.Count)
            {
                _logger.LogError($"教室数量 ({problem.Classrooms.Count}) 小于课程数量 ({problem.CourseSections.Count})，如果有同时段课程，将无法分配所有课程！");
            }
        }
        // 构建松弛模型，仅保留最基本的约束
        private CpModel BuildRelaxedModel(SchedulingProblem problem)
        {
            var model = new CpModel();

            // 为每个课程创建一个变量，代表它是否被安排
            var courseVars = new Dictionary<int, IntVar>();
            foreach (var section in problem.CourseSections)
            {
                string varName = $"course_{section.Id}";
                courseVars[section.Id] = model.NewBoolVar(varName);
            }

            // 约束：每个课程必须安排
            foreach (var entry in courseVars)
            {
                model.Add(entry.Value == 1);
            }

            return model;
        }
    }
}