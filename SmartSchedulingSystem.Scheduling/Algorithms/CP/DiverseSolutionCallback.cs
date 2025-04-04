using Google.OrTools.Sat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 增强的CP回调，用于生成多样化解决方案
    /// </summary>
    public class DiverseSolutionCallback : CpSolverSolutionCallback
    {
        private readonly Dictionary<string, IntVar> _variables;
        private readonly int _maxSolutions;
        private readonly CpModel _model;
        private int _solutionCount = 0;
        private readonly double _diversityThreshold;

        /// <summary>
        /// 收集到的解决方案
        /// </summary>
        public List<Dictionary<string, long>> Solutions { get; } = new List<Dictionary<string, long>>();

        // 使用HashSet记录已找到解的特征
        private readonly HashSet<string> _solutionSignatures = new HashSet<string>();

        public DiverseSolutionCallback(
            Dictionary<string, IntVar> variables,
            int maxSolutions,
            CpModel model,
            double diversityThreshold = 0.2)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _maxSolutions = maxSolutions;
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _diversityThreshold = diversityThreshold;
        }

        public override void OnSolutionCallback()
        {
            // 计算当前解的特征签名
            string signature = ComputeSolutionSignature();

            // 如果是新的解结构
            if (!_solutionSignatures.Contains(signature))
            {
                _solutionSignatures.Add(signature);

                // 收集解
                var solution = new Dictionary<string, long>();
                foreach (var entry in _variables)
                {
                    solution[entry.Key] = Value(entry.Value);
                }

                // 检查与现有解的多样性
                if (IsSufficientlyDiverse(solution))
                {
                    Solutions.Add(solution);
                    _solutionCount++;

                    // 添加排除当前解的约束以促进多样性
                    AddDiversificationConstraint();
                }
            }

            // 如果已找到足够多的解，停止搜索
            if (_solutionCount >= _maxSolutions)
            {
                StopSearch();
            }
        }

        /// <summary>
        /// 计算解的特征签名，用于识别不同结构的解
        /// </summary>
        private string ComputeSolutionSignature()
        {
            // 只考虑值为1的变量（表示被选中的分配）
            var assignmentVars = _variables
                .Where(kv => kv.Key.Contains("_") && Value(kv.Value) == 1)
                .Select(kv => kv.Key)
                .OrderBy(k => k)
                .ToList();

            var sb = new StringBuilder();
            foreach (var varName in assignmentVars)
            {
                sb.Append(varName).Append(';');
            }

            return sb.ToString();
        }

        /// <summary>
        /// 检查当前解与现有解的多样性是否足够
        /// </summary>
        private bool IsSufficientlyDiverse(Dictionary<string, long> newSolution)
        {
            // 如果是第一个解，直接接受
            if (Solutions.Count == 0)
            {
                return true;
            }

            // 计算当前解与每个现有解的差异度
            foreach (var existingSolution in Solutions)
            {
                double diversityScore = CalculateDiversity(newSolution, existingSolution);

                // 如果与任何现有解过于相似，拒绝这个解
                if (diversityScore < _diversityThreshold)
                {
                    return false;
                }
            }

            // 与所有现有解差异都足够大
            return true;
        }

        /// <summary>
        /// 计算两个解之间的差异度（0-1范围，1表示完全不同）
        /// </summary>
        private double CalculateDiversity(Dictionary<string, long> solution1, Dictionary<string, long> solution2)
        {
            // 计算两个解中值为1的变量（表示选中的分配）
            var selectedVars1 = solution1
                .Where(kv => kv.Value == 1)
                .Select(kv => kv.Key)
                .ToHashSet();

            var selectedVars2 = solution2
                .Where(kv => kv.Value == 1)
                .Select(kv => kv.Key)
                .ToHashSet();

            // 计算共同选择的变量数
            int commonVars = selectedVars1.Intersect(selectedVars2).Count();

            // 计算所有选择的变量总数
            int totalVars = selectedVars1.Count + selectedVars2.Count - commonVars;

            // 差异度 = 1 - 共同部分比例
            return totalVars > 0 ? 1.0 - ((double)commonVars / totalVars) : 0.0;
        }

        /// <summary>
        /// 添加约束排除当前解
        /// </summary>
        private void AddDiversificationConstraint()
        {
            try
            {
                // 获取当前所有值为1的分配变量
                var activeVars = new List<IntVar>();
                var activeVarIndices = new List<string>();

                foreach (var entry in _variables)
                {
                    if (Value(entry.Value) == 1)
                    {
                        activeVars.Add(entry.Value);
                        activeVarIndices.Add(entry.Key);
                    }
                }

                // 创建约束：sum(activeVars) <= activeVars.Count - 1
                // 这确保下一个解与当前解至少有一个分配不同
                if (activeVars.Count > 0)
                {
                    var constraint = LinearExpr.Sum(activeVars.ToArray());
                    _model.Add(constraint <= activeVars.Count - 1);
                }
            }
            catch (Exception ex)
            {
                // 在实际项目中应该使用日志记录这个异常
                Console.WriteLine($"添加多样性约束时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取找到的解决方案数量
        /// </summary>
        public int SolutionCount => _solutionCount;
    }
}