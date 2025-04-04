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

                var sw = Stopwatch.StartNew();

                // 创建CP模型
                var model = _modelBuilder.BuildModel(problem);

                _logger.LogInformation($"CP模型构建完成，耗时：{sw.ElapsedMilliseconds}ms");

                // 创建CP求解器
                var solver = new CpSolver();

                // 配置求解器
                solver.StringParameters = $"num_search_workers:{Math.Max(1, Environment.ProcessorCount / 2)}"; // 使用部分CPU核心

                if (_parameters.CpTimeLimit > 0)
                {
                    solver.StringParameters += $";max_time_in_seconds:{_parameters.CpTimeLimit}";
                    _logger.LogInformation($"设置CP求解时间限制：{_parameters.CpTimeLimit}秒");
                }

                // 创建多样化解回调
                var variableDict = ExtractVariablesDictionary(model);
                var callback = new DiverseSolutionCallback(variableDict, solutionCount, model);

                // 求解模型
                _logger.LogInformation("开始CP求解...");
                sw.Restart();

                var status = solver.Solve(model, callback);

                sw.Stop();
                _logger.LogInformation($"CP求解完成，状态：{status}，耗时：{sw.ElapsedMilliseconds}ms，找到解数量：{callback.SolutionCount}");

                // 检查是否找到解
                if (status == CpSolverStatus.Unknown)
                {
                    _logger.LogWarning("CP求解器无法在给定时间内找到解");
                    return new List<SchedulingSolution>();
                }

                if (callback.SolutionCount == 0)
                {
                    _logger.LogWarning("CP求解器未找到满足所有硬约束的解");
                    return new List<SchedulingSolution>();
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
                }

                sw.Stop();
                _logger.LogInformation($"解转换完成，耗时：{sw.ElapsedMilliseconds}ms，共{solutions.Count}个解");

                return solutions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成初始解过程中发生异常");
                throw;
            }
        }

        /// <summary>
        /// 从模型中提取变量字典
        /// </summary>
        private Dictionary<string, IntVar> ExtractVariablesDictionary(CpModel model)
        {
            // 注意：这是一个简化版，实际版本需要访问CpModel的内部变量
            // 在实际项目中，需要在ModelBuilder中保存变量映射并返回

            // 为了演示目的，我们返回一个空字典
            _logger.LogWarning("使用空变量字典，这在实际项目中需要替换为真实实现");
            return new Dictionary<string, IntVar>();
        }

        /// <summary>
        /// 检查排课问题的可行性（是否存在满足所有硬约束的解）
        /// </summary>
        public bool CheckFeasibility(SchedulingProblem problem, out CpSolverStatus status)
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
                solver.StringParameters = "num_search_workers:8;max_time_in_seconds:60";

                // 求解模型
                _logger.LogInformation("开始CP可行性求解...");
                sw.Restart();

                status = solver.Solve(model);

                sw.Stop();
                _logger.LogInformation($"CP可行性检查完成，状态：{status}，耗时：{sw.ElapsedMilliseconds}ms");

                // 解释结果
                bool isFeasible = false;

                switch (status)
                {
                    case CpSolverStatus.Optimal:
                    case CpSolverStatus.Feasible:
                        isFeasible = true;
                        _logger.LogInformation("排课问题有可行解");
                        break;
                    case CpSolverStatus.Infeasible:
                        _logger.LogInformation("排课问题无可行解，约束冲突");
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
    }
}