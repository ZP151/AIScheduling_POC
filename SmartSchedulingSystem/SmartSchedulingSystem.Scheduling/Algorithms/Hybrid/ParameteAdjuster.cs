using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Utils;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Algorithms.Hybrid
{
    /// <summary>
    /// Parameter Adjuster - Dynamically adjusts parameters based on problem characteristics and intermediate results
    /// </summary>
    public class ParameterAdjuster
    {
        private readonly Utils.SchedulingParameters _parameters;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parameters">Scheduling parameters</param>
        public ParameterAdjuster(Utils.SchedulingParameters parameters)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        /// <summary>
        /// Adjust parameters based on problem characteristics
        /// </summary>
        public void AdjustParameters(SchedulingProblem problem)
        {
            // Calculate basic problem characteristics
            int courseCount = problem.CourseSections.Count;
            int teacherCount = problem.Teachers.Count;
            int classroomCount = problem.Classrooms.Count;
            int timeSlotCount = problem.TimeSlots.Count;

            // Calculate problem size metric (0-1 range)
            double problemSizeMetric = CalculateProblemSizeMetric(courseCount, teacherCount, classroomCount, timeSlotCount);

            // Calculate constraint complexity
            double constraintComplexity = CalculateConstraintComplexity(problem);

            // Adjust parameters
            AdjustCPParameters(problemSizeMetric, constraintComplexity);
            AdjustLSParameters(problemSizeMetric, constraintComplexity);
            AdjustParallelizationParameters(problemSizeMetric);

            // Output adjusted parameter information
            LogParameters();
        }

        /// <summary>
        /// Calculate problem size metric (0-1 range)
        /// </summary>
        private double CalculateProblemSizeMetric(int courseCount, int teacherCount, int classroomCount, int timeSlotCount)
        {
            // Determine thresholds for problem size based on experience
            const int smallCourseCount = 20;
            const int mediumCourseCount = 100;
            const int largeCourseCount = 500;

            // Calculate size metric based on course count
            double sizeMetric;
            if (courseCount <= smallCourseCount)
            {
                sizeMetric = (double)courseCount / smallCourseCount * 0.25; // 0-0.25
            }
            else if (courseCount <= mediumCourseCount)
            {
                sizeMetric = 0.25 + (courseCount - smallCourseCount) / (double)(mediumCourseCount - smallCourseCount) * 0.5; // 0.25-0.75
            }
            else
            {
                sizeMetric = Math.Min(0.75 + (courseCount - mediumCourseCount) / (double)(largeCourseCount - mediumCourseCount) * 0.25, 1.0); // 0.75-1.0
            }

            return sizeMetric;
        }

        /// <summary>
        /// Calculate constraint complexity
        /// </summary>
        private double CalculateConstraintComplexity(SchedulingProblem problem)
        {
            // Simplified version: Estimate constraint complexity based on hard constraint ratio
            // In actual implementation, more factors can be considered, such as interactions between constraints

            const double defaultComplexity = 0.5; // Default medium complexity

            // If constraint information is not available, return default value
            if (problem.Constraints == null || problem.Constraints.Count == 0)
            {
                return defaultComplexity;
            }

            // Calculate hard constraint ratio
            int hardConstraintCount = problem.Constraints.Count(c => c.IsHard);
            double hardConstraintRatio = (double)hardConstraintCount / problem.Constraints.Count;

            // Higher hard constraint ratio means more complex problem
            return 0.3 + hardConstraintRatio * 0.7; // 0.3-1.0 range
        }

        /// <summary>
        /// Adjust CP parameters
        /// </summary>
        private void AdjustCPParameters(double problemSizeMetric, double constraintComplexity)
        {
            // Adjust initial solution count based on problem size
            if (problemSizeMetric < 0.4) // Small problem
            {
                _parameters.InitialSolutionCount = 10;
            }
            else if (problemSizeMetric < 0.7) // Medium problem
            {
                _parameters.InitialSolutionCount = 5;
            }
            else // Large problem
            {
                _parameters.InitialSolutionCount = 3;
            }

            // Adjust CP solving time limit based on constraint complexity
            _parameters.CpTimeLimit = (int)(30 + constraintComplexity * 270); // 30-300 seconds
        }

        /// <summary>
        /// Adjust LS parameters
        /// </summary>
        private void AdjustLSParameters(double problemSizeMetric, double constraintComplexity)
        {
            // Adjust local search iteration count based on problem size and constraint complexity
            _parameters.MaxLsIterations = (int)(500 + 4500 * problemSizeMetric * constraintComplexity); // 500-5000

            // Adjust simulated annealing parameters
            _parameters.InitialTemperature = 0.5 + 0.5 * constraintComplexity; // 0.5-1.0
            _parameters.CoolingRate = 0.999 - 0.001 * problemSizeMetric; // 0.999-0.998
        }

        /// <summary>
        /// Adjust parallelization parameters
        /// </summary>
        private void AdjustParallelizationParameters(double problemSizeMetric)
        {
            // Small problems may not need parallelization
            _parameters.EnableParallelOptimization = problemSizeMetric >= 0.4;

            // Adjust maximum parallelism based on problem size
            // Large problems use more threads
            int availableProcessors = Environment.ProcessorCount;
            _parameters.MaxParallelism = problemSizeMetric < 0.7
                ? Math.Max(2, availableProcessors / 2)
                : Math.Max(4, availableProcessors - 1);
        }

        /// <summary>
        /// Output adjusted parameters
        /// </summary>
        private void LogParameters()
        {
            Console.WriteLine("Adjusted parameters:");
            Console.WriteLine($"Initial solution count: {_parameters.InitialSolutionCount}");
            Console.WriteLine($"CP solving time limit: {_parameters.CpTimeLimit} seconds");
            Console.WriteLine($"LS maximum iterations: {_parameters.MaxLsIterations}");
            Console.WriteLine($"Initial temperature: {_parameters.InitialTemperature}");
            Console.WriteLine($"Cooling rate: {_parameters.CoolingRate}");
            Console.WriteLine($"Enable parallel optimization: {_parameters.EnableParallelOptimization}");
            Console.WriteLine($"Maximum parallelism: {_parameters.MaxParallelism}");
        }
    }
}