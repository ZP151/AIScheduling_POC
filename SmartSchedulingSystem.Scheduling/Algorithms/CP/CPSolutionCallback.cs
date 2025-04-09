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
            Console.WriteLine("收到回调，找到新解!");
            // 为了调试，输出一些变量的值
            var activeVariables = _variables.Where(v => Value(v.Value) > 0).ToList();
            Console.WriteLine($"发现 {activeVariables.Count} 个活跃变量(值>0)");

            // 存储当前解
            var solution = new Dictionary<string, long>();
            foreach (var entry in _variables)
            {
                long value = Value(entry.Value);
                solution[entry.Key] = value;

                // 输出活跃变量
                if (value > 0)
                {
                    Console.WriteLine($"变量 {entry.Key} = {value}");
                }
            }

            Solutions.Add(solution);
            _solutionCount++;

            Console.WriteLine($"当前已收集 {_solutionCount} 个解");

            // 如果达到最大解数，停止搜索
            if (_solutionCount >= _maxSolutions)
            {
                Console.WriteLine("达到最大解数，停止搜索");
                StopSearch();
            }
        }

        /// <summary>
        /// 获取找到的解决方案数量
        /// </summary>
        public int SolutionCount => _solutionCount;
    }
}