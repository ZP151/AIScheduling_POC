using System;
using Microsoft.Extensions.Logging;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS
{
    /// <summary>
    /// Class implementing simulated annealing control logic, used to guide the local search process
    /// </summary>
    public class SimulatedAnnealingController
    {
        private readonly ILogger<SimulatedAnnealingController> _logger;
        private readonly Random _random = new Random();

        private double _initialTemperature;
        private readonly double _finalTemperature;
        private double _coolingRate;
        private readonly int _maxIterations;
        private readonly int _maxNoImprovementIterations;

        /// <summary>
        /// Current temperature
        /// </summary>
        private double _currentTemperature;

        /// <summary>
        /// Current iteration count
        /// </summary>
        private int _currentIteration;

        /// <summary>
        /// Search progress (0-1 range)
        /// </summary>
        private double _progress;

        /// <summary>
        /// Number of iterations without improvement
        /// </summary>
        private int _noImprovementCount;

        /// <summary>
        /// Best score
        /// </summary>
        private double _bestScore;

        /// <summary>
        /// Current temperature
        /// </summary>
        public double CurrentTemperature => _currentTemperature;

        /// <summary>
        /// Current iteration count
        /// </summary>
        public int CurrentIteration => _currentIteration;

        /// <summary>
        /// Search progress (0-1 range)
        /// </summary>
        public double Progress => _progress;

        /// <summary>
        /// Number of iterations without improvement
        /// </summary>
        public int NoImprovementCount => _noImprovementCount;

        /// <summary>
        /// Best score
        /// </summary>
        public double BestScore => _bestScore;

        /// <summary>
        /// Create simulated annealing controller with default parameters
        /// </summary>
        /// <param name="logger">Logger</param>
        public SimulatedAnnealingController(ILogger<SimulatedAnnealingController> logger)
        {
            _logger = logger;
            _initialTemperature = 100.0;
            _finalTemperature = 0.1;
            _coolingRate = 0.95;
            _maxIterations = 1000;
            _maxNoImprovementIterations = 100;

            Reset();
        }

        /// <summary>
        /// Create simulated annealing controller
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="initialTemp">Initial temperature</param>
        /// <param name="finalTemp">Final temperature</param>
        /// <param name="coolingRate">Cooling rate</param>
        /// <param name="maxIterations">Maximum iterations</param>
        /// <param name="maxNoImprovementIterations">Maximum iterations allowed without improvement</param>
        public SimulatedAnnealingController(
            ILogger<SimulatedAnnealingController> logger,
            double initialTemp,
            double finalTemp,
            double coolingRate,
            int maxIterations,
            int maxNoImprovementIterations)
        {
            _logger = logger;
            _initialTemperature = initialTemp;
            _finalTemperature = finalTemp;
            _coolingRate = coolingRate;
            _maxIterations = maxIterations;
            _maxNoImprovementIterations = maxNoImprovementIterations;

            Reset();
        }

        /// <summary>
        /// Reset to initial state
        /// </summary>
        public void Reset()
        {
            _currentTemperature = _initialTemperature;
            _currentIteration = 0;
            _progress = 0;
            _noImprovementCount = 0;
            _bestScore = double.MinValue;

            _logger.LogInformation($"Simulated annealing controller reset, initial temperature: {_initialTemperature}, cooling rate: {_coolingRate}");
        }

        /// <summary>
        /// Reset with specified parameters
        /// </summary>
        /// <param name="initialTemperature">Initial temperature</param>
        /// <param name="coolingRate">Cooling rate</param>
        public void Reset(double initialTemperature, double coolingRate)
        {
            _initialTemperature = initialTemperature;
            _coolingRate = coolingRate;
            _currentTemperature = _initialTemperature;
            _currentIteration = 0;
            _progress = 0;
            _noImprovementCount = 0;
            _bestScore = double.MinValue;

            _logger.LogInformation($"Simulated annealing controller reset with new parameters, initial temperature: {_initialTemperature}, cooling rate: {_coolingRate}");
        }

        /// <summary>
        /// Update best score and calculate no improvement count
        /// </summary>
        /// <param name="score">Current score</param>
        public void UpdateBestScore(double score)
        {
            if (score > _bestScore)
            {
                double improvement = _bestScore > double.MinValue ? (score - _bestScore) : 0;
                _bestScore = score;
                _noImprovementCount = 0;

                _logger.LogDebug($"Found new best solution, score: {_bestScore}, improvement: {improvement:F4}");
            }
            else
            {
                _noImprovementCount++;

                if (_noImprovementCount % 50 == 0)
                {
                    _logger.LogDebug($"No improvement for {_noImprovementCount} iterations");
                }
            }
        }

        /// <summary>
        /// Cool down temperature (each call represents one iteration)
        /// </summary>
        /// <returns>Whether to stop search</returns>
        public bool Cool()
        {
            // Standard annealing cooling
            _currentTemperature *= _coolingRate;

            // Handle temperature lower limit
            if (_currentTemperature < _finalTemperature)
            {
                _currentTemperature = _finalTemperature;
            }

            _currentIteration++;

            // Log progress (intermittently)
            if (_currentIteration % 100 == 0 || _currentIteration == 1)
            {
                _logger.LogDebug($"Iteration: {_currentIteration}, temperature: {_currentTemperature:F6}, best score: {_bestScore:F4}");
            }

            // Check if should stop search
            bool shouldStop =
                _currentIteration >= _maxIterations || // Reached maximum iterations
                _currentTemperature <= _finalTemperature || // Temperature reached minimum
                _noImprovementCount >= _maxNoImprovementIterations; // No improvement for too long

            if (shouldStop && _currentIteration % 100 != 0) // Avoid duplicate logs
            {
                _logger.LogInformation($"Search ended, iteration: {_currentIteration}, temperature: {_currentTemperature:F6}, best score: {_bestScore:F4}");

                // Log stop reason
                if (_currentIteration >= _maxIterations)
                {
                    _logger.LogInformation("Stop reason: Reached maximum iterations");
                }
                else if (_currentTemperature <= _finalTemperature)
                {
                    _logger.LogInformation("Stop reason: Temperature reached minimum value");
                }
                else if (_noImprovementCount >= _maxNoImprovementIterations)
                {
                    _logger.LogInformation($"Stop reason: No improvement for {_noImprovementCount} iterations");
                }
            }

            return shouldStop;
        }

        /// <summary>
        /// Determine whether to accept new solution
        /// </summary>
        /// <param name="currentScore">Current solution score</param>
        /// <param name="newScore">New solution score</param>
        /// <returns>Whether to accept new solution</returns>
        public bool ShouldAccept(double currentScore, double newScore)
        {
            // Always accept better solutions
            if (newScore >= currentScore)
            {
                return true;
            }

            // Calculate acceptance probability based on temperature and score difference
            double scoreDifference = newScore - currentScore;
            double acceptanceProbability = Math.Exp(scoreDifference / _currentTemperature);

            // Decide whether to accept based on probability
            bool shouldAccept = _random.NextDouble() < acceptanceProbability;

            // Log detailed information
            if (_currentIteration % 100 == 0 || shouldAccept)
            {
                _logger.LogDebug($"Current score: {currentScore:F4}, new score: {newScore:F4}, " +
                                $"difference: {scoreDifference:F4}, acceptance probability: {acceptanceProbability:F4}, " +
                                $"accepted: {shouldAccept}");
            }

            return shouldAccept;
        }

        /// <summary>
        /// Adjust search parameters (adaptive)
        /// </summary>
        public void AdjustParameters()
        {
            // Adjust parameters based on search progress
            _progress = _currentIteration / (double)_maxIterations;

            // Early search: High temperature phase
            if (_progress < 0.2)
            {
                // Maintain high acceptance rate to encourage exploration
                if (_noImprovementCount > 20)
                {
                    // If no improvement for long time, increase temperature
                    _currentTemperature = Math.Min(_initialTemperature, _currentTemperature / _coolingRate);
                    _noImprovementCount = 0;

                    _logger.LogDebug($"Increased temperature to encourage exploration, new temperature: {_currentTemperature:F6}");
                }
            }
            // Mid search: Gradual cooling
            else if (_progress < 0.7)
            {
                // Standard cooling, no additional adjustments
            }
            // Late search: Low temperature phase
            else
            {
                if (_noImprovementCount > 50)
                {
                    // If no improvement for long time, might be stuck in local optimum
                    // Temporarily increase temperature
                    _currentTemperature = Math.Min(_initialTemperature * 0.5, _currentTemperature / (_coolingRate * _coolingRate));
                    _noImprovementCount = 0;

                    _logger.LogDebug($"Temporarily increased temperature to escape local optimum, new temperature: {_currentTemperature:F6}");
                }
                else if (_noImprovementCount == 0 && _currentTemperature > _finalTemperature * 10)
                {
                    // If found better solution, can accelerate cooling
                    _currentTemperature *= _coolingRate;

                    _logger.LogDebug($"Accelerated cooling to focus search, new temperature: {_currentTemperature:F6}");
                }
            }
        }
    }
}