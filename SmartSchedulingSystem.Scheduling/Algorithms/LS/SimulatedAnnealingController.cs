using System;

namespace SchedulingSystem.Scheduling.Engine.LS
{
    /// <summary>
    /// 实现模拟退火控制逻辑的类
    /// </summary>
    public class SimulatedAnnealingController
    {
        private double _initialTemperature;
        private double _finalTemperature;
        private double _coolingRate;
        private double _currentTemperature;
        private int _iteration;
        private int _maxIterations;
        private Random _random = new Random();

        /// <summary>
        /// 创建模拟退火控制器
        /// </summary>
        /// <param name="initialTemp">初始温度</param>
        /// <param name="finalTemp">最终温度</param>
        /// <param name="coolingRate">冷却率</param>
        /// <param name="maxIterations">最大迭代次数</param>
        public SimulatedAnnealingController(
            double initialTemp = 1.0,
            double finalTemp = 0.01,
            double coolingRate = 0.995,
            int maxIterations = 1000)
        {
            _initialTemperature = initialTemp;
            _finalTemperature = finalTemp;
            _coolingRate = coolingRate;
            _maxIterations = maxIterations;
            Reset();
        }

        /// <summary>
        /// 重置到初始状态
        /// </summary>
        public void Reset()
        {
            _currentTemperature = _initialTemperature;
            _iteration = 0;
        }

        /// <summary>
        /// 降低温度(调用一次代表一次迭代)
        /// </summary>
        /// <returns>是否已达到最大迭代次数</returns>
        public bool Cool()
        {
            _currentTemperature *= _coolingRate;
            if (_currentTemperature < _finalTemperature)
            {
                _currentTemperature = _finalTemperature;
            }

            _iteration++;

            return _iteration >= _maxIterations || _currentTemperature <= _finalTemperature;
        }

        /// <summary>
        /// 确定是否应接受新解
        /// </summary>
        /// <param name="currentScore">当前解的评分</param>
        /// <param name="newScore">新解的评分</param>
        /// <returns>是否应接受新解</returns>
        public bool ShouldAccept(double currentScore, double newScore)
        {
            // 如果新解更好，总是接受
            if (newScore >= currentScore)
            {
                return true;
            }

            // 根据温度和评分差异计算接受概率
            double scoreDifference = newScore - currentScore;
            double acceptanceProbability = Math.Exp(scoreDifference / _currentTemperature);

            // 根据概率决定是否接受
            return _random.NextDouble() < acceptanceProbability;
        }

        /// <summary>
        /// 获取当前温度
        /// </summary>
        public double CurrentTemperature => _currentTemperature;

        /// <summary>
        /// 获取当前迭代次数
        /// </summary>
        public int CurrentIteration => _iteration;

        /// <summary>
        /// 获取搜索进度(0-1)
        /// </summary>
        public double Progress => Math.Min(1.0, (double)_iteration / _maxIterations);
    }
}