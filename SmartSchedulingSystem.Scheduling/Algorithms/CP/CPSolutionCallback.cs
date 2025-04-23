using Google.OrTools.Sat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// CP求解器的解决方案回调类，用于收集求解过程中的解决方案
    /// </summary>
    public class CPSolutionCallback : CpSolverSolutionCallback
    {
        private readonly Dictionary<string, IntVar> _variableDict;
        private readonly int _targetSolutionCount;
        
        public List<Dictionary<string, long>> Solutions { get; private set; }
        public int SolutionCount => Solutions.Count;
        
        /// <summary>
        /// 初始化解决方案回调
        /// </summary>
        /// <param name="variableDict">变量字典</param>
        /// <param name="targetSolutionCount">目标解数量</param>
        public CPSolutionCallback(Dictionary<string, IntVar> variableDict, int targetSolutionCount)
        {
            _variableDict = variableDict ?? throw new ArgumentNullException(nameof(variableDict));
            _targetSolutionCount = Math.Max(1, targetSolutionCount);
            Solutions = new List<Dictionary<string, long>>();
        }
        
        /// <summary>
        /// 当找到新解时调用
        /// </summary>
        public override void OnSolutionCallback()
        {
            // 如果已经收集了足够的解决方案，可以提前停止求解
            if (Solutions.Count >= _targetSolutionCount)
            {
                StopSearch();
                return;
            }
            
            // 收集当前解决方案的变量值
            var solution = new Dictionary<string, long>();
            
            foreach (var entry in _variableDict)
            {
                solution[entry.Key] = Value(entry.Value);
            }
            
            // 添加到解决方案列表
            Solutions.Add(solution);
            
            // 如果达到目标解数量，停止搜索
            if (Solutions.Count >= _targetSolutionCount)
            {
                StopSearch();
            }
        }
        
        /// <summary>
        /// 重置回调状态
        /// </summary>
        public void Reset()
        {
            Solutions.Clear();
        }
    }
}