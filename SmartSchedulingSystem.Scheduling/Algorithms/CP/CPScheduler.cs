using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 约束规划排课引擎
    /// </summary>
    public class CPScheduler
    {
        private readonly CPModelBuilder _modelBuilder;
        private readonly SolutionConverter _solutionConverter;
        private readonly SchedulingParameters _parameters;

        public CPScheduler(CPModelBuilder modelBuilder, SolutionConverter solutionConverter, SchedulingParameters parameters)
        {
            _modelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
            _solutionConverter = solutionConverter ?? throw new ArgumentNullException(nameof(solutionConverter));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        /// <summary>
        /// 使用约束规划生成初始解集
        /// </summary>
        public List<SchedulingSolution> GenerateInitialSolutions(SchedulingProblem problem, int solutionCount)
        {
            // 创建CP模型
            var model = _modelBuilder.BuildModel(problem);

            // 创建CP求解器
            var solver = new CpSolver();

            // 配置求解器
            solver.StringParameters = "num_search_workers:8"; // 使用8个线程并行搜索
            if (_parameters.CpTimeLimit > 0)
            {
                solver.StringParameters += $";max_time_in_seconds:{_parameters.CpTimeLimit}";
            }

            // 获取模型中的变量
            var variables = GetVariablesFromModel(model);

            // 创建多样化解回调
            var callback = new DiverseSolutionCallback(variables, solutionCount, model);

            // 求解模型
            var status = solver.Solve(model, callback);

            // 检查是否找到解
            if (status == CpSolverStatus.Unknown)
            {
                throw new InvalidOperationException("CP求解器无法在给定时间内找到解");
            }

            if (callback.SolutionCount == 0)
            {
                throw new InvalidOperationException("CP求解器未找到满足所有硬约束的解");
            }

            // 转换为排课系统解
            var solutions = new List<SchedulingSolution>();
            foreach (var cpSolution in callback.Solutions)
            {
                var solution = _solutionConverter.ConvertToSchedulingSolution(cpSolution, problem);
                solutions.Add(solution);
            }

            return solutions;
        }

        /// <summary>
        /// 从模型中提取变量字典
        /// </summary>
        private Dictionary<string, IntVar> GetVariablesFromModel(CpModel model)
        {
            // 注意：这是一个简化版，实际版本可能需要访问CpModel的内部变量
            // 这些变量应该在ModelBuilder中创建时保存下来
            // 这里我们返回一个空字典作为占位符
            return new Dictionary<string, IntVar>();
        }
    }
}