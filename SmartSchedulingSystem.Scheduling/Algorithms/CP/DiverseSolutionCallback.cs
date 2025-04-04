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
        private int _solutionCount = 0;
        private CpModel _model;

        /// <summary>
        /// 收集到的解决方案
        /// </summary>
        public List<Dictionary<string, long>> Solutions { get; } = new List<Dictionary<string, long>>();

        // 使用HashSet记录已找到解的特征
        private HashSet<string> _solutionSignatures = new HashSet<string>();

        public DiverseSolutionCallback(Dictionary<string, IntVar> variables, int maxSolutions, CpModel model)
        {
            _variables = variables;
            _maxSolutions = maxSolutions;
            _model = model;
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
                Solutions.Add(solution);

                _solutionCount++;

                // 添加排除当前解的约束以促进多样性
                AddDiversificationConstraint();
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
            var selectedVars = _variables
                .Where(kv => kv.Key.Contains("_") && Value(kv.Value) == 1)
                .OrderBy(kv => kv.Key)
                .ToList();

            var sb = new StringBuilder();
            foreach (var kv in selectedVars)
            {
                sb.Append(kv.Key).Append(';');
            }

            return sb.ToString();
        }

        /// <summary>
        /// 添加约束排除当前解
        /// </summary>
        private void AddDiversificationConstraint()
        {
            // 获取当前所有值为1的变量
            var activeVars = _variables
                .Where(kv => kv.Key.Contains("_") && Value(kv.Value) == 1)
                .ToList();

            // 创建约束：sum(activeVars) <= activeVars.Count - 1
            // 这确保下一个解与当前解至少有一个分配不同
            if (activeVars.Count > 0)
            {
                var constraint = LinearExpr.Sum(activeVars.Select(kv => kv.Value).ToArray());
                _model.Add(constraint <= activeVars.Count - 1);
            }
        }

        /// <summary>
        /// 获取找到的解决方案数量
        /// </summary>
        public int SolutionCount => _solutionCount;
    }
}