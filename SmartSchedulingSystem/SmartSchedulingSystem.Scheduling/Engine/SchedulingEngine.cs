using SmartSchedulingSystem.Scheduling.Algorithms.CP;
using SmartSchedulingSystem.Scheduling.Algorithms.Hybrid;
using SmartSchedulingSystem.Scheduling.Algorithms.LS;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Constraints;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    /// <summary>
    /// Scheduling engine core class, responsible for coordinating and executing the complete scheduling process
    /// </summary>
    public class SchedulingEngine
    {
        private readonly ILogger<SchedulingEngine> _logger;
        private readonly ConstraintManager _constraintManager;
        private readonly CPScheduler _cpScheduler;
        private readonly LocalSearchOptimizer _localSearchOptimizer;
        private readonly CPLSScheduler _cplsScheduler;
        private readonly ProblemAnalyzer _problemAnalyzer;
        private readonly SolutionEvaluator _solutionEvaluator;
        private readonly SolutionDiversifier _solutionDiversifier;

        public SchedulingEngine(
            ILogger<SchedulingEngine> logger,
            ConstraintManager constraintManager,
            CPScheduler cpScheduler,
            LocalSearchOptimizer localSearchOptimizer,
            CPLSScheduler cplsScheduler,
            ProblemAnalyzer problemAnalyzer,
            SolutionEvaluator solutionEvaluator,
            SolutionDiversifier solutionDiversifier)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _cpScheduler = cpScheduler ?? throw new ArgumentNullException(nameof(cpScheduler));
            _localSearchOptimizer = localSearchOptimizer ?? throw new ArgumentNullException(nameof(localSearchOptimizer));
            _cplsScheduler = cplsScheduler ?? throw new ArgumentNullException(nameof(cplsScheduler));
            _problemAnalyzer = problemAnalyzer ?? throw new ArgumentNullException(nameof(problemAnalyzer));
            _solutionEvaluator = solutionEvaluator ?? throw new ArgumentNullException(nameof(solutionEvaluator));
            _solutionDiversifier = solutionDiversifier ?? throw new ArgumentNullException(nameof(solutionDiversifier));
            
            // Register global constraint manager, so it can be accessed in CPScheduler
            GlobalConstraintManager.Initialize(_constraintManager);
            _logger.LogInformation("Constraint manager registered as global instance");
        }

        /// <summary>
        /// Generate scheduling solution
        /// </summary>
        /// <param name="problem">Scheduling problem definition</param>
        /// <param name="parameters">Scheduling parameters</param>
        /// <param name="useSimplifiedMode">Whether to use simplified mode</param>
        /// <returns>Scheduling result</returns>
        public SchedulingResult GenerateSchedule(SchedulingProblem problem, Utils.SchedulingParameters parameters = null, bool useSimplifiedMode = false)
        {
            try
            {
                _logger.LogInformation("Starting to generate scheduling solution, using progressive constraint strategy...");
                
                // Set whether to use simplified constraint mode
                if (_constraintManager is ConstraintManager constraintManager)
                {
                    constraintManager.UseSimplifiedConstraints(useSimplifiedMode);
                    
                    if (useSimplifiedMode)
                    {
                        _logger.LogInformation("Using simplified constraint mode to generate scheduling solution");
                    }
                }

                // Analyze problem
                var features = _problemAnalyzer.AnalyzeProblem(problem);
                _logger.LogInformation("Problem features: CourseCount={CourseCount}, TeacherCount={TeacherCount}, ClassroomCount={ClassroomCount}, Complexity={Complexity}",
                    features.CourseSectionCount, features.TeacherCount, features.ClassroomCount, features.OverallComplexity);

                // If no parameters are provided, use recommended parameters
                parameters ??= _problemAnalyzer.RecommendParameters(features);
                
                var result = new SchedulingResult
                {
                    Problem = problem,
                    Status = SchedulingStatus.NotStarted,
                    CreatedAt = DateTime.Now,
                    Solutions = new List<SchedulingSolution>(),
                    Message = "Using progressive constraint strategy to generate scheduling solution"
                };

                // Default to standard level
                ConstraintApplicationLevel constraintLevel = ConstraintApplicationLevel.Standard;
                
                // Determine which constraint level to use based on the parameters
                if (parameters?.UseBasicConstraints == true)
                {
                    _logger.LogInformation("Based on parameters, only use Basic level constraints (Level1 only)...");
                    constraintLevel = ConstraintApplicationLevel.Basic;
                }
                else if (parameters?.UseEnhancedConstraints == true)
                {
                    _logger.LogInformation("Based on parameters, enable Enhanced level constraints (includes Level3 physical soft constraints)...");
                    constraintLevel = ConstraintApplicationLevel.Enhanced;
                }
                else
                {
                    _logger.LogInformation("Using Standard level constraints (Level1+Level2)...");
                    // Use default Standard level
                }
                
                // Set constraint application level
                _constraintManager.SetConstraintApplicationLevel(constraintLevel);
                
                var startTime = DateTime.Now;
                
                // Generate specified number of solutions, using updated progressive constraint strategy
                int targetSolutionCount = problem.GenerateMultipleSolutions ? 
                    Math.Max(3, problem.SolutionCount) : 1; // Ensure at least 3 solutions are generated
                
                // Generate solutions
                var solutions = _cpScheduler.GenerateRandomSolutions(problem, targetSolutionCount);
                
                if (solutions.Count > 0)
                {
                    _logger.LogInformation($"Successfully generated {solutions.Count} solutions");
                    
                    // Check solution diversity
                    _logger.LogInformation("Checking solution diversity, currently {SolutionsCount} solutions", solutions.Count);
                    if (solutions.Count > 1)
                    {
                        // Ensure solutions are sufficiently diverse, limit to 5 solutions
                        solutions = _solutionDiversifier.DiversifySolutions(problem, solutions, 5).ToList();
                        _logger.LogInformation("Optimized solution diversity, final {SolutionsCount} solutions", solutions.Count);
                    }
                    
                    // Check if there are duplicate assignments in each solution
                    foreach (var solution in solutions)
                    {
                        var sectionIds = new HashSet<int>();
                        var duplicateAssignments = new List<SchedulingAssignment>();
                        
                        foreach (var assignment in solution.Assignments)
                        {
                            if (sectionIds.Contains(assignment.SectionId))
                            {
                                duplicateAssignments.Add(assignment);
                                _logger.LogWarning($"Solution #{solution.Id} has duplicate assignment for course {assignment.SectionId} ({assignment.SectionCode}), removing duplicate");
                            }
                            else
                            {
                                sectionIds.Add(assignment.SectionId);
                            }
                        }
                        
                        // Remove duplicate assignments
                        foreach (var duplicate in duplicateAssignments)
                        {
                            solution.Assignments.Remove(duplicate);
                        }
                        
                        if (duplicateAssignments.Count > 0)
                        {
                            _logger.LogInformation($"Removed {duplicateAssignments.Count} duplicate assignments from solution #{solution.Id}");
                        }
                    }
                    
                    // If solution count is insufficient, try to create variants
                    if (solutions.Count < targetSolutionCount)
                    {
                        _logger.LogInformation($"Generated solutions count is insufficient ({solutions.Count}/{targetSolutionCount}), creating variants...");
                        var existingSolutions = new List<SchedulingSolution>(solutions);
                        
                        // Create variants based on existing solutions
                        for (int i = solutions.Count; i < targetSolutionCount && existingSolutions.Count > 0; i++)
                        {
                            var baseSolution = existingSolutions[i % existingSolutions.Count];
                            var variant = baseSolution.Clone();
                            
                            // Modify some assignments to create variants
                            CreateSolutionVariant(variant, problem);
                            
                            // Give variant a new ID
                            variant.Id = i + 1;
                            variant.Algorithm = $"Variant-{baseSolution.Algorithm}";
                            
                            // Evaluate variant
                            var evaluation = _solutionEvaluator.Evaluate(variant);
                            variant.Evaluation = evaluation;
                            
                            solutions.Add(variant);
                            _logger.LogInformation($"Created variant #{i+1} based on solution #{baseSolution.Id}");
                        }
                    }
                    
                    // Ensure each solution has a unique ID
                    for (int i = 0; i < solutions.Count; i++)
                    {
                        solutions[i].Id = i + 1;
                        
                        // Optimize solution (optional)
                        if (parameters.EnableLocalSearch && parameters.MaxLsIterations > 0)
                        {
                            try
                            {
                                _logger.LogInformation($"Starting local search optimization for solution #{solutions[i].Id}...");
                                
                                var optimizedSolution = _localSearchOptimizer.OptimizeSolution(
                                    solutions[i], 
                                    parameters.MaxLsIterations,
                                    parameters.InitialTemperature,
                                    parameters.CoolingRate);
                                
                                // If the optimized solution is better, replace it
                                if (optimizedSolution.Score > solutions[i].Score)
                                {
                                    _logger.LogInformation($"Solution #{solutions[i].Id} improved from {solutions[i].Score:F2} to {optimizedSolution.Score:F2}");
                                    optimizedSolution.Algorithm = solutions[i].Algorithm + "+LS";
                                    optimizedSolution.Id = solutions[i].Id; // Keep same ID
                                    solutions[i] = optimizedSolution;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error optimizing solution #{solutions[i].Id}");
                            }
                        }
                    }
                    
                    result.Solutions = solutions;
                    result.Status = SchedulingStatus.Success;
                    result.Message = "Successfully generated scheduling solution using progressive constraint strategy";
                }
                else
                {
                    _logger.LogWarning("Failed to generate any solutions using progressive constraint strategy");
                    result.Status = SchedulingStatus.Failure;
                    result.Message = "Failed to generate any solutions using progressive constraint strategy";
                }
                
                // Calculate execution time
                result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
                
                // Calculate statistics
                result.Statistics.TotalSections = problem.CourseSections.Count;
                result.Statistics.ScheduledSections = result.Solutions.Count > 0 ? 
                    result.Solutions[0].Assignments.Count : 0;
                result.Statistics.UnscheduledSections = result.Statistics.TotalSections - result.Statistics.ScheduledSections;
                result.Statistics.TotalTeachers = problem.Teachers.Count;
                result.Statistics.TotalClassrooms = problem.Classrooms.Count;
                
                // Restore constraint manager state
                if (_constraintManager is ConstraintManager constraintManager2)
                {
                    constraintManager2.UseSimplifiedConstraints(false);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scheduling solution");
                
                // Restore constraint manager state
                if (_constraintManager is ConstraintManager constraintManager)
                {
                    constraintManager.UseSimplifiedConstraints(false);
                }
                
                // Generate random solutions as fallback for exceptional cases
                try
                {
                    var randomResult = new SchedulingResult
                    {
                        Status = SchedulingStatus.Error,
                        Message = $"Encountered exception: {ex.Message}, returned random solutions as fallback",
                        Solutions = new List<SchedulingSolution>()
                    };
                    
                    int targetSolutionCount = problem.GenerateMultipleSolutions ? 
                        Math.Max(3, problem.SolutionCount) : 1; // Ensure at least 3 solutions are generated
                    
                    // Use lowest constraint level to force generate solutions
                    _constraintManager.SetConstraintApplicationLevel(ConstraintApplicationLevel.Basic);
                    var randomSolutions = _cpScheduler.GenerateRandomSolutions(problem, targetSolutionCount);
                    
                    // Ensure each solution has a unique ID
                    for (int i = 0; i < randomSolutions.Count; i++)
                    {
                        randomSolutions[i].Id = i + 1;
                        randomSolutions[i].Algorithm = $"Emergency-Random-{i+1}";
                    }
                    
                    if (randomSolutions.Count > 0)
                    {
                        _logger.LogInformation($"Generated {randomSolutions.Count} random solutions as fallback");
                        randomResult.Solutions = randomSolutions;
                        randomResult.Status = SchedulingStatus.PartialSuccess;
                        randomResult.Message = "Encountered exception, returned solutions generated using lowest constraint level";
                        return randomResult;
                    }
                    
                    return randomResult;
                }
                catch
                {
                    // If random solutions also fail, return original error
                    return new SchedulingResult
                    {
                        Status = SchedulingStatus.Error,
                        Message = $"Encountered exception: {ex.Message}",
                        Solutions = new List<SchedulingSolution>()
                    };
                }
            }
        }

        /// <summary>
        /// Create solution variants by randomly modifying some assignments to increase diversity
        /// </summary>
        private void CreateSolutionVariant(SchedulingSolution solution, SchedulingProblem problem)
        {
            if (solution.Assignments.Count == 0)
                return;
                
            var random = new Random();
            
            // Decide the number of assignments to modify (about 20%)
            int modificationCount = Math.Max(1, solution.Assignments.Count / 5);
            
            // Select assignments to modify
            var assignmentsToModify = solution.Assignments
                .OrderBy(x => random.Next())
                .Take(modificationCount)
                .ToList();
                
            foreach (var assignment in assignmentsToModify)
            {
                // Randomly decide which part to modify (time slot, classroom, teacher)
                int modType = random.Next(3);
                
                if (modType == 0 && problem.TimeSlots.Count > 1)
                {
                    // Modify time slot
                    var availableTimeSlots = problem.TimeSlots
                        .Where(ts => ts.Id != assignment.TimeSlotId)
                        .ToList();
                        
                    if (availableTimeSlots.Count > 0)
                    {
                        var newTimeSlot = availableTimeSlots[random.Next(availableTimeSlots.Count)];
                        assignment.TimeSlotId = newTimeSlot.Id;
                        assignment.DayOfWeek = newTimeSlot.DayOfWeek;
                        assignment.StartTime = newTimeSlot.StartTime;
                        assignment.EndTime = newTimeSlot.EndTime;
                    }
                }
                else if (modType == 1 && problem.Classrooms.Count > 1)
                {
                    // Modify classroom
                    var availableClassrooms = problem.Classrooms
                        .Where(c => c.Id != assignment.ClassroomId && c.Capacity >= problem.CourseSections.FirstOrDefault(cs => cs.Id == assignment.SectionId)?.Enrollment)
                        .ToList();
                        
                    if (availableClassrooms.Count > 0)
                    {
                        var newClassroom = availableClassrooms[random.Next(availableClassrooms.Count)];
                        assignment.ClassroomId = newClassroom.Id;
                        assignment.ClassroomName = newClassroom.Name;
                    }
                }
                else if (problem.Teachers.Count > 1)
                {
                    // Modify teacher
                    var availableTeachers = problem.Teachers
                        .Where(t => t.Id != assignment.TeacherId)
                        .ToList();
                        
                    if (availableTeachers.Count > 0)
                    {
                        var newTeacher = availableTeachers[random.Next(availableTeachers.Count)];
                        assignment.TeacherId = newTeacher.Id;
                        assignment.TeacherName = newTeacher.Name;
                    }
                }
            }
        }

        /// <summary>
        /// Evaluate scheduling solution
        /// </summary>
        public SchedulingEvaluation EvaluateSchedule(SchedulingSolution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            try
            {
                _logger.LogInformation("Starting to evaluate scheduling solution...");

                // Use evaluator to evaluate solution
                double score = _solutionEvaluator.Evaluate(solution).Score;
                var hardConstraintSatisfaction = _solutionEvaluator.EvaluateHardConstraints(solution);
                var softConstraintSatisfaction = _solutionEvaluator.EvaluateSoftConstraints(solution);

                return new SchedulingEvaluation
                {
                    SolutionId = solution.Id,
                    Score = score,
                    HardConstraintsSatisfied = hardConstraintSatisfaction >= 1.0,
                    HardConstraintsSatisfactionLevel = hardConstraintSatisfaction,
                    SoftConstraintsSatisfactionLevel = softConstraintSatisfaction
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating scheduling solution");
                throw;
            }
        }

        /// <summary>
        /// Optimize existing scheduling solution
        /// </summary>
        /// <param name="solution">Scheduling solution</param>
        /// <param name="parameters">Scheduling parameters</param>
        /// <returns>Optimized solution</returns>
        public SchedulingSolution OptimizeSchedule(SchedulingSolution solution, Utils.SchedulingParameters parameters = null)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            try
            {
                _logger.LogInformation("Starting to optimize scheduling solution...");

                // Use local search optimizer to optimize solution
                var optimizedSolution = _localSearchOptimizer.OptimizeSolution(solution);

                _logger.LogInformation("Scheduling solution optimized");

                return optimizedSolution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing scheduling solution");
                throw;
            }
        }
    }
}