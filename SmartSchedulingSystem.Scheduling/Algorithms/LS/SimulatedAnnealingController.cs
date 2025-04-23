using System;
using Microsoft.Extensions.Logging;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS
{
    /// <summary>
    /// 实现模拟退火控制逻辑的类，用于指导局部搜索过程
    /// </summary>
    public class SimulatedAnnealingController
    {
        private readonly ILogger<SimulatedAnnealingController> _logger;
        private readonly Random _random = new Random();

        private double _initialTemperature;
        private double _finalTemperature;
        private double _coolingRate;
        private double _currentTemperature;
        private int _iteration;
        private int _maxIterations;
        private int _noImprovementCount;
        private int _maxNoImprovementIterations;
        private double _bestScore;

        /// <summary>
        /// 当前温度
        /// </summary>
        public double CurrentTemperature => _currentTemperature;

        /// <summary>
        /// 当前迭代数
        /// </summary>
        public int CurrentIteration => _iteration;

        /// <summary>
        /// 搜索进度(0-1范围)
        /// </summary>
        public double Progress => Math.Min(1.0, (double)_iteration / _maxIterations);

        /// <summary>
        /// 无改进的迭代次数
        /// </summary>
        public int NoImprovementCount => _noImprovementCount;

        /// <summary>
        /// 最佳得分
        /// </summary>
        public double BestScore => _bestScore;

        /// <summary>
        /// 创建模拟退火控制器
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="initialTemp">初始温度</param>
        /// <param name="finalTemp">最终温度</param>
        /// <param name="coolingRate">冷却率</param>
        /// <param name="maxIterations">最大迭代次数</param>
        /// <param name="maxNoImprovementIterations">允许无改进的最大迭代次数</param>
        public SimulatedAnnealingController(
            ILogger<SimulatedAnnealingController> logger,
            double initialTemp = 1.0,
            double finalTemp = 0.01,
            double coolingRate = 0.995,
            int maxIterations = 1000,
            int maxNoImprovementIterations = 200)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _initialTemperature = initialTemp;
            _finalTemperature = finalTemp;
            _coolingRate = coolingRate;
            _maxIterations = maxIterations;
            _maxNoImprovementIterations = maxNoImprovementIterations;
            Reset();
        }

        /// <summary>
        /// 重置到初始状态
        /// </summary>
        public void Reset()
        {
            _currentTemperature = _initialTemperature;
            _iteration = 0;
            _noImprovementCount = 0;
            _bestScore = double.MinValue;

            _logger.LogInformation($"模拟退火控制器已重置，初始温度: {_initialTemperature}, 冷却率: {_coolingRate}");
        }

        /// <summary>
        /// 使用指定参数重置
        /// </summary>
        /// <param name="initialTemperature">初始温度</param>
        /// <param name="coolingRate">冷却率</param>
        public void Reset(double initialTemperature, double coolingRate)
        {
            _initialTemperature = initialTemperature;
            _coolingRate = coolingRate;
            _currentTemperature = _initialTemperature;
            _iteration = 0;
            _noImprovementCount = 0;
            _bestScore = double.MinValue;

            _logger.LogInformation($"模拟退火控制器已使用新参数重置，初始温度: {_initialTemperature}, 冷却率: {_coolingRate}");
        }

        /// <summary>
        /// 更新最佳得分并计算无改进次数
        /// </summary>
        /// <param name="score">当前得分</param>
        public void UpdateBestScore(double score)
        {
            if (score > _bestScore)
            {
                double improvement = _bestScore > double.MinValue ? (score - _bestScore) : 0;
                _bestScore = score;
                _noImprovementCount = 0;

                _logger.LogDebug($"发现新的最佳解，得分: {_bestScore}, 改进: {improvement:F4}");
            }
            else
            {
                _noImprovementCount++;

                if (_noImprovementCount % 50 == 0)
                {
                    _logger.LogDebug($"已有 {_noImprovementCount} 次迭代无改进");
                }
            }
        }

        /// <summary>
        /// 降低温度(调用一次代表一次迭代)
        /// </summary>
        /// <returns>是否应该停止搜索</returns>
        public bool Cool()
        {
            // 标准退火冷却
            _currentTemperature *= _coolingRate;

            // 处理温度下限
            if (_currentTemperature < _finalTemperature)
            {
                _currentTemperature = _finalTemperature;
            }

            _iteration++;

            // 记录日志（间隔性地）
            if (_iteration % 100 == 0 || _iteration == 1)
            {
                _logger.LogDebug($"迭代: {_iteration}, 温度: {_currentTemperature:F6}, 最佳分数: {_bestScore:F4}");
            }

            // 检查是否应该停止搜索
            bool shouldStop =
                _iteration >= _maxIterations || // 达到最大迭代次数
                _currentTemperature <= _finalTemperature || // 温度降到最低
                _noImprovementCount >= _maxNoImprovementIterations; // 长时间无改进

            if (shouldStop && _iteration % 100 != 0) // 避免重复日志
            {
                _logger.LogInformation($"搜索结束，迭代: {_iteration}, 温度: {_currentTemperature:F6}, 最佳分数: {_bestScore:F4}");

                // 记录停止原因
                if (_iteration >= _maxIterations)
                {
                    _logger.LogInformation("停止原因: 达到最大迭代次数");
                }
                else if (_currentTemperature <= _finalTemperature)
                {
                    _logger.LogInformation("停止原因: 温度达到最低值");
                }
                else if (_noImprovementCount >= _maxNoImprovementIterations)
                {
                    _logger.LogInformation($"停止原因: {_noImprovementCount} 次迭代无改进");
                }
            }

            return shouldStop;
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
            bool shouldAccept = _random.NextDouble() < acceptanceProbability;

            // 记录详细日志
            if (_iteration % 100 == 0 || shouldAccept)
            {
                _logger.LogDebug($"当前分数: {currentScore:F4}, 新分数: {newScore:F4}, " +
                                $"差异: {scoreDifference:F4}, 接受概率: {acceptanceProbability:F4}, " +
                                $"接受: {shouldAccept}");
            }

            return shouldAccept;
        }

        /// <summary>
        /// 调整搜索参数（自适应）
        /// </summary>
        public void AdjustParameters()
        {
            // 根据搜索进展调整参数
            double progress = Progress;

            // 早期搜索：高温阶段
            if (progress < 0.2)
            {
                // 保持较高接受率，鼓励探索
                if (_noImprovementCount > 20)
                {
                    // 如果长时间无改进，提高温度
                    _currentTemperature = Math.Min(_initialTemperature, _currentTemperature / _coolingRate);
                    _noImprovementCount = 0;

                    _logger.LogDebug($"提高温度以增加探索, 新温度: {_currentTemperature:F6}");
                }
            }
            // 中期搜索：逐渐降温
            else if (progress < 0.7)
            {
                // 标准冷却，不做额外调整
            }
            // 后期搜索：低温阶段
            else
            {
                if (_noImprovementCount > 50)
                {
                    // 如果较长时间无改进，可能陷入局部最优
                    // 临时升高温度
                    _currentTemperature = Math.Min(_initialTemperature * 0.5, _currentTemperature / (_coolingRate * _coolingRate));
                    _noImprovementCount = 0;

                    _logger.LogDebug($"临时升高温度以跳出局部最优, 新温度: {_currentTemperature:F6}");
                }
                else if (_noImprovementCount == 0 && _currentTemperature > _finalTemperature * 10)
                {
                    // 如果找到更好解，可以加速冷却
                    _currentTemperature *= _coolingRate;

                    _logger.LogDebug($"加速冷却以集中搜索, 新温度: {_currentTemperature:F6}");
                }
            }
        }
    }
}