using Google.OrTools.Sat;
using System.Collections.Generic;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// 基础CP求解器回调，用于收集解决方案
    /// </summary>
    public class CPSolutionCallback : CpSolverSolutionCallback
    {
        private readonly Dictionary<string, IntVar> _variables;
        private readonly int _maxSolutions;
        private int _solutionCount = 0;

        /// <summary>
        /// 收集到的解决方案
        /// </summary>
        public List<Dictionary<string, long>> Solutions { get; } = new List<Dictionary<string, long>>();

        public CPSolutionCallback(Dictionary<string, IntVar> variables, int maxSolutions)
        {
            _variables = variables;
            _maxSolutions = maxSolutions;
        }

        public override void OnSolutionCallback()
        {
            // 存储当前解
            var solution = new Dictionary<string, long>();
            foreach (var entry in _variables)
            {
                solution[entry.Key] = Value(entry.Value);
            }

            Solutions.Add(solution);
            _solutionCount++;

            // 如果达到最大解数，停止搜索
            if (_solutionCount >= _maxSolutions)
            {
                StopSearch();
            }
        }

        /// <summary>
        /// 获取找到的解决方案数量
        /// </summary>
        public int SolutionCount => _solutionCount;
    }
}