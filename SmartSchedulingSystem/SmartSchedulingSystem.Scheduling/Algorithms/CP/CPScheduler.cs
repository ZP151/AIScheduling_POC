using Google.OrTools.Sat;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Algorithms.CP.Converters;

namespace SmartSchedulingSystem.Scheduling.Algorithms.CP
{
    /// <summary>
    /// Constraint Programming scheduling engine
    /// </summary>
    public class CPScheduler
    {
        private readonly CPModelBuilder _modelBuilder;
        private readonly SolutionConverter _solutionConverter;
        private readonly ILogger<CPScheduler> _logger;
        private readonly SmartSchedulingSystem.Scheduling.Utils.SchedulingParameters _parameters;
        private readonly Random _random;
        private readonly Dictionary<string, ICPConstraintConverter> _constraintConverters;

        public CPScheduler(
            CPModelBuilder modelBuilder,
            SolutionConverter solutionConverter,
            ILogger<CPScheduler> logger,
            SchedulingParameters parameters = null)
        {
            _modelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
            _solutionConverter = solutionConverter ?? throw new ArgumentNullException(nameof(solutionConverter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parameters = parameters ?? new SchedulingParameters();
            _random = new Random();
            _constraintConverters = new Dictionary<string, ICPConstraintConverter>();
            InitializeConstraintConverters();
        }

        /// <summary>
        /// Initialize constraint converters
        /// </summary>
        private void InitializeConstraintConverters()
        {
            // Add constraint converter initialization code here
            // For example:
            // _constraintConverters["TeacherConflict"] = new TeacherConflictConverter();
            // _constraintConverters["ClassroomConflict"] = new ClassroomConflictConverter();
            
            _logger.LogInformation("Constraint converter initialization completed");
        }

        /// <summary>
        /// Generate initial solution set using constraint programming
        /// </summary>
        public List<SchedulingSolution> GenerateInitialSolutions(SchedulingProblem problem, int solutionCount = 5)
        {
            try
            {
                _logger.LogInformation($"Starting to generate initial solutions using CP solver, target count: {solutionCount}");
                Console.WriteLine("============ CP Solving Started ============");
                Console.WriteLine($"Problem ID: {problem.Id}, Name: {problem.Name}");
                Console.WriteLine($"Requested solution count: {solutionCount}");
                
                // Check problem data integrity
                ValidateProblemData(problem);
                DebugProblemData(problem); // Add this line to debug problem data

                var sw = Stopwatch.StartNew();

                // Use progressive constraint application approach
                List<SchedulingSolution> solutions = null;
                
                // First try to generate solutions with minimum level constraints
                _logger.LogInformation("Attempting to generate initial solutions using Basic level constraints...");
                solutions = TryGenerateWithConstraintLevel(problem, solutionCount, ConstraintApplicationLevel.Basic);
                
                // If no solutions found, try to relax constraints further
                if (solutions.Count == 0)
                {
                    _logger.LogWarning("No solutions found with Basic level constraints, attempting to generate random solutions with relaxed constraints...");
                    solutions = GenerateRandomSolutions(problem, solutionCount);
                }
                
                _logger.LogInformation($"CP phase completed, generated {solutions.Count} initial solutions");
                
                return solutions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while generating initial solutions");
                return new List<SchedulingSolution>();
            }
        }
        
        /// <summary>
        /// Try to generate solutions with specified constraint level
        /// </summary>
        private List<SchedulingSolution> TryGenerateWithConstraintLevel(
            SchedulingProblem problem, 
            int solutionCount,
            ConstraintApplicationLevel level)
        {
            try
            {
                var sw = Stopwatch.StartNew();
            
                // Create CP model with specified constraint level
                var model = _modelBuilder.BuildModel(problem, level);

                _logger.LogInformation($"Built CP model with {level} level constraints, time taken: {sw.ElapsedMilliseconds}ms");

                // Create CP solver
                var solver = new CpSolver();
                
                // Configure solver
                int numThreads = Math.Max(1, Environment.ProcessorCount / 2);
                int timeLimit = _parameters.CpTimeLimit > 0 ? _parameters.CpTimeLimit : 60;
                
                solver.StringParameters = $"num_search_workers:{numThreads};max_time_in_seconds:{timeLimit}";
                solver.StringParameters += ";log_search_progress:true;collect_all_solutions_as_last_solution:true";
                
                _logger.LogInformation($"Set CP solver parameters: {solver.StringParameters}");
                
                // Create variable dictionary
                var variableDict = ExtractVariablesDictionary(model);
                
                // Create solution callback
                var callback = new CPSolutionCallback(variableDict, solutionCount);

                // Start solving, but in a cancellable way
                sw.Restart();
                
                // Set timeout monitoring
                var tokenSource = new CancellationTokenSource();
                Task timeoutTask = Task.Run(() => {
                    try 
                    {
                        // Use additional 1 minute as safety margin
                        int timeoutMs = (timeLimit + 60) * 1000;
                        Thread.Sleep(timeoutMs);
                        
                        // If execution reaches here, it means timeout
                        _logger.LogWarning($"CP solving exceeded preset time {timeLimit} seconds, forcing termination");
                        tokenSource.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Timeout monitoring task exception");
                    }
                });
                
                Task<CpSolverStatus> solveTask = Task.Run(() => {
                    try
                    {
                        return solver.Solve(model, callback);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception occurred during CP solving");
                        return CpSolverStatus.Unknown;
                    }
                });

                CpSolverStatus status;
                try
                {
                    // Wait for solving to complete or be cancelled
                    if (Task.WaitAny(new Task[] { solveTask }, timeLimit * 1000 + 10000) == 0)
                    {
                        // Normal completion
                        status = solveTask.Result;
                        _logger.LogInformation($"CP solving completed normally, status: {status}");
                    }
                    else
                    {
                        // Timeout
                        tokenSource.Cancel();
                        status = CpSolverStatus.Unknown;
                        _logger.LogWarning("CP solving timeout, forcing interruption");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while waiting for CP solving results");
                    status = CpSolverStatus.Unknown;
                }
                
                // Cancel timeout monitoring task
                tokenSource.Cancel();
                
                sw.Stop();
                _logger.LogInformation($"CP solving with {level} level constraints took: {sw.ElapsedMilliseconds}ms, status: {status}, solutions found: {callback.SolutionCount}");

                // If not enough solutions found but computation was interrupted, try to use intermediate solutions collected by solver
                if (callback.SolutionCount == 0 && status == CpSolverStatus.Unknown)
                {
                    _logger.LogWarning("CP solving interrupted with no valid solutions returned, attempting to get intermediate solutions");
                    
                    // Create a basic solution
                    try
                    {
                        var partialSolution = CollectPartialSolution(problem, model, solver);
                        if (partialSolution != null)
                        {
                            _logger.LogInformation("Successfully built partial solution");
                            return new List<SchedulingSolution> { partialSolution };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while attempting to build partial solution");
                    }
                }

                // Convert to scheduling system solutions
                var solutions = new List<SchedulingSolution>();

                _logger.LogInformation($"Starting to convert CP solutions to scheduling system solutions...");
                sw.Restart();

                foreach (var cpSolution in callback.Solutions)
                {
                    try
                    {
                        var solution = _solutionConverter.ConvertToDomainSolution(problem, cpSolution);
                        solution.ConstraintLevel = level; // Mark which constraint level the solution was generated under
                        
                        // Calculate solution score
                        var evaluation = EvaluateSolutionQuality(solution, problem);
                        solution.Score = evaluation.Score;
                        solution.Evaluation = evaluation;
                        _logger.LogDebug($"Solution score: {evaluation.Score}");
                        
                        solutions.Add(solution);
                        
                        _logger.LogInformation($"Successfully generated solution #{solution.Id}, using {level} level constraints, score: {solution.Score:F3}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error converting CP solution to scheduling system solution");
                    }
                }

                sw.Stop();
                _logger.LogInformation($"Conversion completed, took: {sw.ElapsedMilliseconds}ms, successfully converted {solutions.Count} solutions");

                return solutions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TryGenerateWithConstraintLevel");
                return new List<SchedulingSolution>();
            }
        }
        
        /// <summary>
        /// Use appropriate constraints to generate random solutions
        /// </summary>
        public List<SchedulingSolution> GenerateRandomSolutions(SchedulingProblem problem, int solutionCount)
        {
            try
            {
                _logger.LogInformation($"Starting to generate {solutionCount} random solutions...");
                
                // Get current constraint application level
                var constraintLevel = GlobalConstraintManager.Current?.GetCurrentApplicationLevel() ?? 
                                     ConstraintApplicationLevel.Basic;
                
                _logger.LogInformation($"Using constraint level {constraintLevel} to generate random solutions");
                
                var solutions = new List<SchedulingSolution>();
                var random = new Random();
                
                // Use different constraint level strategies to generate solutions
                switch (constraintLevel)
                {
                    case ConstraintApplicationLevel.Basic:
                        solutions = TryGenerateSolutionsWithConstraintLevel(problem, solutionCount, ConstraintApplicationLevel.Basic, random);
                        break;
                        
                    case ConstraintApplicationLevel.Standard:
                        // First try to generate solutions with standard level constraints
                        solutions = TryGenerateSolutionsWithConstraintLevel(problem, solutionCount, ConstraintApplicationLevel.Standard, random);
                        
                        // If not enough, downgrade to basic constraints
                        if (solutions.Count < solutionCount)
                        {
                            _logger.LogWarning($"Using Standard level constraints only generated {solutions.Count}/{solutionCount} solutions, attempting to use Basic level constraints...");
                            var basicSolutions = TryGenerateSolutionsWithConstraintLevel(
                                problem, solutionCount - solutions.Count, ConstraintApplicationLevel.Basic, random);
                            solutions.AddRange(basicSolutions);
                        }
                        break;
                        
                    case ConstraintApplicationLevel.Enhanced:
                        // First try to generate solutions with enhanced level constraints
                        solutions = TryGenerateSolutionsWithConstraintLevel(problem, solutionCount, ConstraintApplicationLevel.Enhanced, random);
                        
                        // If not enough, downgrade to standard constraints
                        if (solutions.Count < solutionCount)
                        {
                            _logger.LogWarning($"Using Enhanced level constraints only generated {solutions.Count}/{solutionCount} solutions, attempting to use Standard level constraints...");
                            var standardSolutions = TryGenerateSolutionsWithConstraintLevel(
                                problem, solutionCount - solutions.Count, ConstraintApplicationLevel.Standard, random);
                            solutions.AddRange(standardSolutions);
                            
                            // If still not enough, further downgrade to basic constraints
                            if (solutions.Count < solutionCount)
                            {
                                _logger.LogWarning($"Using Standard level constraints after generating {solutions.Count}/{solutionCount} solutions, attempting to use Basic level constraints...");
                                var basicSolutions = TryGenerateSolutionsWithConstraintLevel(
                                    problem, solutionCount - solutions.Count, ConstraintApplicationLevel.Basic, random);
                                solutions.AddRange(basicSolutions);
                            }
                        }
                        break;
                        
                    case ConstraintApplicationLevel.Complete:
                        // For Complete level (including Level4), we use layered strategy
                        // Here we only consider Level3 for now
                        solutions = TryGenerateSolutionsWithConstraintLevel(problem, solutionCount, ConstraintApplicationLevel.Enhanced, random);
                        
                        if (solutions.Count < solutionCount)
                        {
                            _logger.LogWarning($"Using Complete level constraints only generated {solutions.Count}/{solutionCount} solutions, downgrading to Enhanced level...");
                            var enhancedSolutions = TryGenerateSolutionsWithConstraintLevel(
                                problem, solutionCount - solutions.Count, ConstraintApplicationLevel.Enhanced, random);
                            solutions.AddRange(enhancedSolutions);
                            
                            if (solutions.Count < solutionCount)
                            {
                                _logger.LogWarning($"Using Enhanced level constraints after generating {solutions.Count}/{solutionCount} solutions, downgrading to Standard level...");
                                var standardSolutions = TryGenerateSolutionsWithConstraintLevel(
                                    problem, solutionCount - solutions.Count, ConstraintApplicationLevel.Standard, random);
                                solutions.AddRange(standardSolutions);
                                
                                if (solutions.Count < solutionCount)
                                {
                                    _logger.LogWarning($"Using Standard level constraints after generating {solutions.Count}/{solutionCount} solutions, downgrading to Basic level...");
                                    var basicSolutions = TryGenerateSolutionsWithConstraintLevel(
                                        problem, solutionCount - solutions.Count, ConstraintApplicationLevel.Basic, random);
                                    solutions.AddRange(basicSolutions);
                                }
                            }
                        }
                        break;
                }
                
                // Ensure all solutions have unique IDs
                for (int i = 0; i < solutions.Count; i++)
                {
                    solutions[i].Id = i + 1;
                }
                
                // Handle possible duplicate assignments
                EnsureNoDuplicateAssignments(solutions);
                
                _logger.LogInformation($"Successfully generated {solutions.Count} random solutions");
                return solutions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while generating random solutions");
                return new List<SchedulingSolution>();
            }
        }
        
        /// <summary>
        /// Ensure no duplicate assignments in each solution
        /// </summary>
        private void EnsureNoDuplicateAssignments(List<SchedulingSolution> solutions)
        {
            foreach (var solution in solutions)
            {
                var sectionIds = new HashSet<int>();
                var duplicates = new List<SchedulingAssignment>();
                
                foreach (var assignment in solution.Assignments)
                {
                    if (sectionIds.Contains(assignment.SectionId))
                    {
                        duplicates.Add(assignment);
                        _logger.LogWarning($"Duplicate assignment found in solution #{solution.Id}: Course {assignment.SectionId} is assigned more than once, will remove duplicate assignment");
                    }
                    else
                    {
                        sectionIds.Add(assignment.SectionId);
                    }
                }
                
                // Remove duplicates
                foreach (var duplicate in duplicates)
                {
                    solution.Assignments.Remove(duplicate);
                }
                
                if (duplicates.Count > 0)
                {
                    _logger.LogInformation($"Removed {duplicates.Count} duplicate assignments from solution #{solution.Id}");
                }
            }
        }
        
        /// <summary>
        /// Try to generate specified number of solutions with specific constraint level
        /// </summary>
        private List<SchedulingSolution> TryGenerateSolutionsWithConstraintLevel(
            SchedulingProblem problem, 
            int targetCount,
            ConstraintApplicationLevel level,
            Random random)
        {
            _logger.LogInformation($"Attempting to generate {targetCount} solutions with {level} level constraints");
            
            var solutions = new List<SchedulingSolution>();
            int maxAttempts = targetCount * 3; // Each target solution allows up to 3 attempts
            int attempts = 0;
            
            // Save the original constraint level
            var originalLevel = GlobalConstraintManager.Current?.GetCurrentApplicationLevel() ?? ConstraintApplicationLevel.Basic;
            
            try
            {
                // Set the current constraint level
                GlobalConstraintManager.Current?.SetConstraintApplicationLevel(level);
                
                while (solutions.Count < targetCount && attempts < maxAttempts)
                {
                    attempts++;
                    
                    try
                    {
                        // Select different generation strategies based on constraint levels
                        SchedulingSolution solution = null;
                        
                        switch (level)
                        {
                            case ConstraintApplicationLevel.Basic:
                                // Basic level only uses the most basic hard constraints
                                solution = GenerateConstraintAwareRandomSolution(problem, level, random);
                                break;
                                
                            case ConstraintApplicationLevel.Standard:
                                // Standard level uses all hard constraints
                                solution = GenerateConstraintAwareRandomSolution(problem, level, random);
                                break;
                                
                            case ConstraintApplicationLevel.Enhanced:
                                // Enhanced level considers physical soft constraints
                                solution = GenerateConstraintAwareRandomSolution(problem, level, random);
                                break;
                                
                            case ConstraintApplicationLevel.Complete:
                                // Complete level considers all constraints, including quality soft constraints
                                solution = GenerateConstraintAwareRandomSolution(problem, level, random);
                                break;
                                
                            default:
                                // Default to basic strategy
                                solution = GenerateConstraintAwareRandomSolution(problem, ConstraintApplicationLevel.Basic, random);
                                break;
                        }
                        
                        if (solution != null && solution.Assignments.Count > 0)
                        {
                            // Use unique ID
                            solution.Id = solutions.Count + 1;
                            
                            // Set the source algorithm of the solution
                            solution.Algorithm = $"CP-{level}-{solution.Id}";
                            
                            // Evaluation of solutions
                            var evaluation = EvaluateSolutionQuality(solution, problem);
                            solution.Score = evaluation.Score;
                            solution.Evaluation = evaluation;
                            
                            solutions.Add(solution);
                            
                            _logger.LogInformation($"Successfully generated solution #{solution.Id}, using {level} level constraints, score: {solution.Score:F3}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to generate solution #{attempts}");
                    }
                }
                
                _logger.LogInformation($"Completed generating solutions using {level} level constraints, successfully generated {solutions.Count}/{targetCount} solutions, attempted {attempts} times");
                
                return solutions;
            }
            finally
            {
                // Restore the original constraint level
                if (GlobalConstraintManager.Current != null)
                {
                    GlobalConstraintManager.Current.SetConstraintApplicationLevel(originalLevel);
                }
            }
        }
        
        /// <summary>
        /// Generate a single random solution considering constraints
        /// </summary>
        private SchedulingSolution GenerateConstraintAwareRandomSolution(
            SchedulingProblem problem,
            ConstraintApplicationLevel constraintLevel,
            Random random)
        {
            _logger.LogDebug($"Generating a random solution using {constraintLevel} level constraints");
            var solution = new SchedulingSolution
            {
                Problem = problem,
                Assignments = new List<SchedulingAssignment>(),
                CreatedAt = DateTime.Now,
                Algorithm = $"RandomCP-{constraintLevel}",
                Status = SolutionStatus.Feasible,
                GenerationData = new Dictionary<string, string>
                {
                    { "ConstraintLevel", constraintLevel.ToString() }
                }
            };

            // The constraint state tracking used
            var usedRoomTimes = new HashSet<(int roomId, int timeSlotId)>();
            var usedTeacherTimes = new HashSet<(int teacherId, int timeSlotId)>();
            var courseTeacherMap = new Dictionary<string, int>();

            // Try to assign resources to each course
            foreach (var section in problem.CourseSections)
            {
                bool assigned = false;

                // Use different allocation strategies based on constraint levels
                switch (constraintLevel)
                {
                    case ConstraintApplicationLevel.Basic:
                        // The most basic constraints
                        assigned = TryAssignWithBasicConstraints(
                            problem,
                            section,
                            usedRoomTimes,
                            usedTeacherTimes,
                            courseTeacherMap,
                            solution,
                            random);
                        break;

                    case ConstraintApplicationLevel.Standard:
                        // Core constraints and configurable hard constraints
                        assigned = TryAssignWithStandardConstraints(
                            problem,
                            section,
                            usedRoomTimes,
                            usedTeacherTimes,
                            courseTeacherMap,
                            solution,
                            random);
                        break;

                    case ConstraintApplicationLevel.Enhanced:
                        // Consider physical soft constraints
                        assigned = TryAssignWithEnhancedConstraints(
                            problem,
                            section,
                            usedRoomTimes,
                            usedTeacherTimes,
                            courseTeacherMap,
                            solution,
                            random);
                        break;

                    case ConstraintApplicationLevel.Complete:
                        // All constraints, including quality soft constraints
                        assigned = TryAssignWithAllConstraints(
                            problem,
                            section,
                            usedRoomTimes,
                            usedTeacherTimes,
                            courseTeacherMap,
                            solution,
                            random);
                        break;

                    default:
                        // Default to basic constraints
                        assigned = TryAssignWithBasicConstraints(
                            problem,
                            section,
                            usedRoomTimes,
                            usedTeacherTimes,
                            courseTeacherMap,
                            solution,
                            random);
                        break;
                }

                // If it is not possible to assign, you can choose to force assignment
                if (!assigned && constraintLevel != ConstraintApplicationLevel.Complete)
                {
                    // Only non-Complete levels attempt forced assignment
                    ForceAssignmentWithMinimalConstraints(problem, section, solution, random);
                }
            }

            // Check and clean invalid assignments
            var validAssignments = solution.Assignments
                .Where(a => a.TimeSlotId > 0 && a.ClassroomId > 0 && a.TeacherId > 0)
                .ToList();

            solution.Assignments = validAssignments;
            
            // Evaluation of solutions
            var evaluation = new SchedulingEvaluation();
            var constraintManager = GetConstraintManager();
            
            if (constraintManager != null)
            {
                // Temporarily save the current constraint level
                var originalLevel = constraintManager.GetCurrentApplicationLevel();
                
                try
                {
                    // First evaluate using the Complete level, get the full score
                    constraintManager.SetConstraintApplicationLevel(ConstraintApplicationLevel.Complete);
                    var completeEval = constraintManager.EvaluateConstraints(solution);
                    evaluation.Score = completeEval.Score;
                    evaluation.HardConstraintsSatisfactionLevel = completeEval.HardConstraintsSatisfactionLevel;
                    evaluation.SoftConstraintsSatisfactionLevel = completeEval.SoftConstraintsSatisfactionLevel;
                    evaluation.Conflicts = completeEval.Conflicts;
                }
                finally
                {
                    // Restore the original constraint level
                    constraintManager.SetConstraintApplicationLevel(originalLevel);
                }
            }
            
            solution.Evaluation = evaluation;

            _logger.LogDebug($"The randomly generated solution contains {solution.Assignments.Count} assignments, constraint level: {constraintLevel}");
            return solution;
        }
        
        /// <summary>
        /// Try to assign using enhanced level constraints (considering physical soft constraints)
        /// </summary>
        private bool TryAssignWithEnhancedConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            HashSet<(int roomId, int timeSlotId)> usedRoomTimes,
            HashSet<(int teacherId, int timeSlotId)> usedTeacherTimes,
            Dictionary<string, int> courseTeacherMap,
            SchedulingSolution solution,
            Random random)
        {
            // First check if using standard constraints is feasible
            bool standardAssigned = TryAssignWithStandardConstraints(
                problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                
            if (!standardAssigned)
            {
                return false;
            }
            
            // Get the last added assignment
            var assignment = solution.Assignments.LastOrDefault();
            if (assignment == null)
            {
                return false;
            }
            
            // Check additional physical soft constraints
            
            // 1. Check if the classroom type matches the course requirements
            bool roomTypeMatch = CheckRoomTypeMatch(problem, section, assignment);
            
            // 2. Check if the equipment requirements are met
            bool equipmentMatch = CheckEquipmentRequirements(problem, section, assignment);
            
            // If the physical soft constraints are not met, try to reassign to a more suitable classroom
            if (!roomTypeMatch || !equipmentMatch)
            {
                // Remove the current assignment
                solution.Assignments.Remove(assignment);
                usedRoomTimes.Remove((assignment.ClassroomId, assignment.TimeSlotId));
                
                // Keep the same teacher and time slot, but try to find a more suitable classroom
                var suitableRooms = FindSuitableRooms(problem, section, assignment.TeacherId, assignment.TimeSlotId);
                
                if (suitableRooms.Any())
                {
                    // Randomly select a suitable classroom
                    var room = suitableRooms[random.Next(suitableRooms.Count)];
                    
                    // Create a new assignment
                    var newAssignment = new SchedulingAssignment
                    {
                        SectionId = section.Id,
                        SectionCode = section.SectionCode,
                        TeacherId = assignment.TeacherId,
                        TeacherName = assignment.TeacherName,
                        TimeSlotId = assignment.TimeSlotId,
                        DayOfWeek = assignment.DayOfWeek,
                        StartTime = assignment.StartTime,
                        EndTime = assignment.EndTime,
                        ClassroomId = room.Id,
                        ClassroomName = room.Name,
                        Building = room.Building
                    };
                    
                    // Add the new assignment
                    solution.Assignments.Add(newAssignment);
                    usedRoomTimes.Add((room.Id, assignment.TimeSlotId));
                    
                    return true;
                }
                
                // If no suitable classroom is found, restore the original assignment
                solution.Assignments.Add(assignment);
                usedRoomTimes.Add((assignment.ClassroomId, assignment.TimeSlotId));
            }
            
            return true;
        }
        
        /// <summary>
        /// Try to assign using all constraints (including quality soft constraints)
        /// </summary>
        private bool TryAssignWithAllConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            HashSet<(int roomId, int timeSlotId)> usedRoomTimes,
            HashSet<(int teacherId, int timeSlotId)> usedTeacherTimes,
            Dictionary<string, int> courseTeacherMap,
            SchedulingSolution solution,
            Random random)
        {
            // First ensure that the physical soft constraints can be met
            bool enhancedAssigned = TryAssignWithEnhancedConstraints(
                problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                
            if (!enhancedAssigned)
            {
                return false;
            }
            
            // Get the last added assignment
            var assignment = solution.Assignments.LastOrDefault();
            if (assignment == null)
            {
                return false;
            }
            
            // Add additional quality soft constraint checks here
            // TODO: Implement quality soft constraint checks
            
            return true;
        }
        
        /// <summary>
        /// Check if the classroom type matches the course requirements
        /// </summary>
        private bool CheckRoomTypeMatch(SchedulingProblem problem, CourseSectionInfo section, SchedulingAssignment assignment)
        {
            // Check additional resource requirements
            if (problem.CourseResourceRequirements != null && problem.ClassroomResources != null)
            {
                var courseRequirement = problem.CourseResourceRequirements
                    .FirstOrDefault(r => r.CourseSectionId == section.Id);
                var classroomResource = problem.ClassroomResources
                    .FirstOrDefault(r => r.ClassroomId == assignment.ClassroomId);
                
                if (courseRequirement != null && classroomResource != null && 
                    courseRequirement.PreferredRoomTypes.Any())
                {
                    return courseRequirement.PreferredRoomTypes.Contains(classroomResource.RoomType);
                }
            }
            
            // Check traditional classroom type matching
            if (!string.IsNullOrEmpty(section.RequiredClassroomType))
            {
                var classroom = problem.Classrooms.FirstOrDefault(c => c.Id == assignment.ClassroomId);
                if (classroom != null)
                {
                    return classroom.Type == section.RequiredClassroomType;
                }
            }
            
            // No specific requirements, default match
            return true;
        }
        
        /// <summary>
        /// Check if the classroom equipment meets the course requirements
        /// </summary>
        private bool CheckEquipmentRequirements(SchedulingProblem problem, CourseSectionInfo section, SchedulingAssignment assignment)
        {
            // Check additional resource requirements
            if (problem.CourseResourceRequirements != null && problem.ClassroomResources != null)
            {
                var courseRequirement = problem.CourseResourceRequirements
                    .FirstOrDefault(r => r.CourseSectionId == section.Id);
                var classroomResource = problem.ClassroomResources
                    .FirstOrDefault(r => r.ClassroomId == assignment.ClassroomId);
                
                if (courseRequirement != null && classroomResource != null && 
                    courseRequirement.ResourceTypes.Any())
                {
                    // Check if all equipment requirements are met
                    var missingResources = courseRequirement.ResourceTypes
                        .Where(r => !classroomResource.ResourceTypes.Contains(r))
                        .ToList();
                    
                    return !missingResources.Any(); // Return true if there are no missing resources
                }
            }
            
            // Check traditional equipment requirements
            if (!string.IsNullOrEmpty(section.RequiredEquipment))
            {
                var classroom = problem.Classrooms.FirstOrDefault(c => c.Id == assignment.ClassroomId);
                if (classroom != null && !string.IsNullOrEmpty(classroom.Equipment))
                {
                    // Convert the comma-separated equipment list to a collection
                    var requiredEquipment = section.RequiredEquipment.Split(',')
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrEmpty(e))
                        .ToList();
                    
                    // Check if the classroom contains all the required equipment
                    foreach (var equipment in requiredEquipment)
                    {
                        if (!classroom.Equipment.Contains(equipment))
                        {
                            return false;
                        }
                    }
                    
                    return true;
                }
                
                return false; // The classroom has no equipment information but the course requires equipment
            }
            
            // No specific requirements, default satisfied
            return true;
        }
        
        /// <summary>
        /// Find classrooms that meet the classroom type and equipment requirements
        /// </summary>
        private List<ClassroomInfo> FindSuitableRooms(SchedulingProblem problem, CourseSectionInfo section, int teacherId, int timeSlotId)
        {
            // First find all unused classrooms (no longer use problem.Assignments, because SchedulingProblem does not have this property)
            var availableRooms = problem.Classrooms.ToList();
            
            // Check classroom availability
            availableRooms = availableRooms
                .Where(room => !problem.ClassroomAvailabilities.Any(ca => 
                    ca.ClassroomId == room.Id && ca.TimeSlotId == timeSlotId && !ca.IsAvailable))
                .ToList();
            
            // Check capacity constraints
            availableRooms = availableRooms
                .Where(room => room.Capacity >= section.Enrollment)
                .ToList();
            
            // Sort classrooms, first consider classrooms that meet the classroom type and equipment requirements
            var suitableRooms = new List<ClassroomInfo>();
            
            foreach (var room in availableRooms)
            {
                var tempAssignment = new SchedulingAssignment { ClassroomId = room.Id };
                
                bool roomTypeMatches = CheckRoomTypeMatch(problem, section, tempAssignment);
                bool equipmentMatches = CheckEquipmentRequirements(problem, section, tempAssignment);
                
                // Sort priority: 1. Meet both classroom type and equipment requirements 2. Meet classroom type 3. Meet equipment requirements 4. Do not meet any requirements
                if (roomTypeMatches && equipmentMatches)
                {
                    suitableRooms.Insert(0, room); // Most preferred
                }
                else if (roomTypeMatches)
                {
                    suitableRooms.Insert(suitableRooms.Count / 3, room); // Second preferred
                }
                else if (equipmentMatches)
                {
                    suitableRooms.Insert(suitableRooms.Count * 2 / 3, room); // Third preferred
                }
                else
                {
                    suitableRooms.Add(room); // Last preferred
                }
            }
            
            return suitableRooms;
        }
        
        /// <summary>
        /// Get the constraint manager instance
        /// </summary>
        private IConstraintManager GetConstraintManager()
        {
            // Here directly use the constraint manager injected in the constructor
            // First try to get it through SchedulingEngine, if not available then try using the service locator
            try
            {
                // If there is a global access point in the system, try to get it from there
                // Below uses a simplified global access method, which needs to be replaced with dependency injection in actual projects
                var constraintManager = GlobalConstraintManager.Current;
                if (constraintManager != null)
                {
                    _logger.LogDebug("Got the constraint manager from the global instance");
                    return constraintManager;
                }
                
                // As a backup, create a temporary manager with basic constraints
                _logger.LogWarning("Failed to get the global constraint manager, creating a temporary constraint manager instance");
                
                var tempConstraints = new List<IConstraint>();
                // Add basic constraints, such as teacher/classroom conflict constraints
                return new SmartSchedulingSystem.Scheduling.Engine.ConstraintManager(
                    tempConstraints, 
                    LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning)).CreateLogger<ConstraintManager>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get the constraint manager");
                return null;
            }
        }
        
        /// <summary>
        /// Evaluate the quality of the solution, calculate various constraint violations
        /// </summary>
        public SchedulingEvaluation EvaluateSolutionQuality(SchedulingSolution solution, SchedulingProblem problem)
        {
            try
            {
                _logger.LogInformation($"Begin evaluating solution quality with {solution.Assignments.Count} assignments");
                
                // Create the evaluation result object
                var evaluation = new SchedulingEvaluation
                {
                    SolutionId = solution.Id,
                    IsFeasible = true, // Default set to feasible, will be modified if there are hard constraint violations
                    HardConstraintsSatisfied = true
                };
                
                // Initialize total score and violation count
                double totalScore = 100.0; // Start from满分，扣除违反项
                int violationCount = 0;
                
                // Record used resources, used to detect conflicts
                var usedRoomTimes = new HashSet<(int roomId, int timeSlotId)>();
                var usedTeacherTimes = new HashSet<(int teacherId, int timeSlotId)>();
                var courseSectionTeachers = new Dictionary<string, int>(); // Record each course code corresponding to a teacher
                
                // Count of various violations
                int capacityViolations = 0;
                int teacherQualificationViolations = 0;
                int sameCourseTeacherViolations = 0;
                int teacherAvailabilityViolations = 0;
                int roomAvailabilityViolations = 0;
                int roomConflictViolations = 0;
                int teacherConflictViolations = 0;
                
                // List of conflicts
                var conflicts = new List<SchedulingConflict>();
                
                // Check each assignment
                foreach (var assignment in solution.Assignments)
                {
                    // 1. Check if the classroom capacity is sufficient
                    if (assignment.Classroom.Capacity < assignment.CourseSection.Enrollment)
                    {
                        capacityViolations++;
                        var conflict = new SchedulingConflict
                        {
                            Id = conflicts.Count + 1,
                            Type = SchedulingConflictType.ClassroomCapacityExceeded,
                            Description = $"Classroom capacity violation: Classroom {assignment.Classroom.Name} (capacity: {assignment.Classroom.Capacity}) is insufficient to accommodate course {assignment.CourseSection.CourseName} (enrollment: {assignment.CourseSection.Enrollment})",
                            Severity = ConflictSeverity.Moderate
                        };
                        conflicts.Add(conflict);
                        _logger.LogWarning(conflict.Description);
                    }
                    
                    // 2. Check teacher qualification
                    if (!IsTeacherQualified(problem, assignment.Teacher.Id, assignment.CourseSection.CourseId))
                    {
                        teacherQualificationViolations++;
                        var conflict = new SchedulingConflict
                        {
                            Id = conflicts.Count + 1,
                            Type = SchedulingConflictType.Other,
                            Description = $"Teacher qualification violation: Teacher {assignment.Teacher.Name} may not have sufficient qualifications to teach course {assignment.CourseSection.CourseName}",
                            Severity = ConflictSeverity.Minor
                        };
                        conflicts.Add(conflict);
                        _logger.LogWarning(conflict.Description);
                    }
                    
                    // 3. Ensure that different classes of the same course are taught by the same teacher
                    if (courseSectionTeachers.TryGetValue(assignment.CourseSection.CourseCode, out int existingTeacherId))
                    {
                        if (existingTeacherId != assignment.Teacher.Id)
                        {
                            sameCourseTeacherViolations++;
                            var conflict = new SchedulingConflict
                            {
                                Id = conflicts.Count + 1,
                                Type = SchedulingConflictType.Other,
                                Description = $"Same course different teacher violation: Course {assignment.CourseSection.CourseCode} is taught by different teachers in different classes",
                                Severity = ConflictSeverity.Minor
                            };
                            conflicts.Add(conflict);
                            _logger.LogWarning(conflict.Description);
                        }
                    }
                    else
                    {
                        courseSectionTeachers[assignment.CourseSection.CourseCode] = assignment.Teacher.Id;
                    }
                    
                    // 4. Check if the teacher is available at this time slot
                    var teacherUnavailable = problem.TeacherAvailabilities
                        .Any(ta => ta.TeacherId == assignment.Teacher.Id && 
                                 ta.TimeSlotId == assignment.TimeSlot.Id && 
                                 !ta.IsAvailable);
                                 
                    if (teacherUnavailable)
                    {
                        teacherAvailabilityViolations++;
                        var conflict = new SchedulingConflict
                        {
                            Id = conflicts.Count + 1,
                            Type = SchedulingConflictType.TeacherAvailabilityConflict,
                            Description = $"教师时间冲突: 教师 {assignment.Teacher.Name} 在时间段 {assignment.TimeSlot.DayName} {assignment.TimeSlot.StartTime} 不可用",
                            Severity = ConflictSeverity.Severe
                        };
                        conflicts.Add(conflict);
                        _logger.LogWarning(conflict.Description);
                    }
                    
                    // 5. Check if the classroom is available at this time slot
                    var roomUnavailable = problem.ClassroomAvailabilities
                        .Any(ca => ca.ClassroomId == assignment.Classroom.Id && 
                                ca.TimeSlotId == assignment.TimeSlot.Id && 
                                !ca.IsAvailable);
                                
                    if (roomUnavailable)
                    {
                        roomAvailabilityViolations++;
                        var conflict = new SchedulingConflict
                        {
                            Id = conflicts.Count + 1,
                            Type = SchedulingConflictType.ClassroomAvailabilityConflict,
                            Description = $"Classroom time conflict: Classroom {assignment.Classroom.Name} is unavailable at time slot {assignment.TimeSlot.DayName} {assignment.TimeSlot.StartTime}",
                            Severity = ConflictSeverity.Severe
                        };
                        conflicts.Add(conflict);
                        _logger.LogWarning(conflict.Description);
                    }
                    
                    // 6. Check classroom conflict (only one course can be assigned to one classroom at the same time)
                    var roomTimeKey = (assignment.Classroom.Id, assignment.TimeSlot.Id);
                    if (usedRoomTimes.Contains(roomTimeKey))
                    {
                        roomConflictViolations++;
                        var conflict = new SchedulingConflict
                        {
                            Id = conflicts.Count + 1,
                            Type = SchedulingConflictType.ClassroomConflict,
                            Description = $"教室冲突: 教室 {assignment.Classroom.Name} 在时间段 {assignment.TimeSlot.DayName} {assignment.TimeSlot.StartTime} 已被其他课程使用",
                            Severity = ConflictSeverity.Critical
                        };
                        conflicts.Add(conflict);
                        _logger.LogWarning(conflict.Description);
                    }
                    else
                    {
                        usedRoomTimes.Add(roomTimeKey);
                    }
                    
                    // 7. Check teacher conflict (only one course can be assigned to one teacher at the same time)
                    var teacherTimeKey = (assignment.Teacher.Id, assignment.TimeSlot.Id);
                    if (usedTeacherTimes.Contains(teacherTimeKey))
                    {
                        teacherConflictViolations++;
                        var conflict = new SchedulingConflict
                        {
                            Id = conflicts.Count + 1,
                            Type = SchedulingConflictType.TeacherConflict,
                            Description = $"教师冲突: 教师 {assignment.Teacher.Name} 在时间段 {assignment.TimeSlot.DayName} {assignment.TimeSlot.StartTime} 已被安排教授其他课程",
                            Severity = ConflictSeverity.Critical
                        };
                        conflicts.Add(conflict);
                        _logger.LogWarning(conflict.Description);
                    }
                    else
                    {
                        usedTeacherTimes.Add(teacherTimeKey);
                    }
                }
                
                // Calculate total violations
                violationCount = capacityViolations + teacherQualificationViolations + sameCourseTeacherViolations
                                + teacherAvailabilityViolations + roomAvailabilityViolations 
                                + roomConflictViolations + teacherConflictViolations;
                
                // 根据违反数扣减分数
                // Hard constraint violations each扣10分
                double hardConstraintPenalty = (roomConflictViolations + teacherConflictViolations) * 10.0;
                // Soft constraint violations each扣5分
                double softConstraintPenalty = (capacityViolations + teacherQualificationViolations + 
                                              sameCourseTeacherViolations + teacherAvailabilityViolations + 
                                              roomAvailabilityViolations) * 5.0;
                
                totalScore = Math.Max(0, totalScore - hardConstraintPenalty - softConstraintPenalty);
                
                // If there are hard constraint violations, the solution is not feasible
                if (roomConflictViolations > 0 || teacherConflictViolations > 0)
                {
                    evaluation.IsFeasible = false;
                    evaluation.HardConstraintsSatisfied = false;
                }
                
                // Calculate the satisfaction of hard and soft constraints
                double hardConstraintSatisfaction = (roomConflictViolations + teacherConflictViolations) > 0 ? 
                    0.0 : 1.0;
                    
                double softConstraintTotal = capacityViolations + teacherQualificationViolations + 
                                           sameCourseTeacherViolations + teacherAvailabilityViolations + 
                                           roomAvailabilityViolations;
                double softConstraintSatisfaction = solution.Assignments.Count > 0 ? 
                    Math.Max(0, 1.0 - (softConstraintTotal / (solution.Assignments.Count * 5.0))) : 1.0;
                
                // Set the evaluation result
                evaluation.Score = totalScore / 100.0; // Convert to 0-1 range
                evaluation.HardConstraintsSatisfactionLevel = hardConstraintSatisfaction;
                evaluation.SoftConstraintsSatisfactionLevel = softConstraintSatisfaction;
                evaluation.Conflicts = conflicts;
                
                // Log the evaluation result
                _logger.LogInformation($"Solution quality evaluation result:");
                _logger.LogInformation($"- Classroom capacity violations: {capacityViolations}");
                _logger.LogInformation($"- Teacher qualification violations: {teacherQualificationViolations}");
                _logger.LogInformation($"- Same course different teacher violations: {sameCourseTeacherViolations}");
                _logger.LogInformation($"- Teacher availability violations: {teacherAvailabilityViolations}");
                _logger.LogInformation($"- Classroom availability violations: {roomAvailabilityViolations}");
                _logger.LogInformation($"- Classroom conflict violations: {roomConflictViolations}");
                _logger.LogInformation($"- Teacher conflict violations: {teacherConflictViolations}");
                _logger.LogInformation($"- Total violations: {violationCount}");
                _logger.LogInformation($"- Final score: {totalScore}/100");
                _logger.LogInformation($"- Solution feasibility: {evaluation.IsFeasible}");
                
                return evaluation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred when evaluating solution quality");
                // Return an infeasible evaluation result when an exception occurs
                return new SchedulingEvaluation
                {
                    IsFeasible = false,
                    Score = 0.0,
                    SolutionId = solution.Id,
                    HardConstraintsSatisfied = false,
                    HardConstraintsSatisfactionLevel = 0.0,
                    SoftConstraintsSatisfactionLevel = 0.0,
                    Conflicts = new List<SchedulingConflict>
                    {
                        new SchedulingConflict
                        {
                            Id = 1,
                            Type = SchedulingConflictType.ConstraintEvaluationError,
                            Description = $"Error occurred when evaluating solution: {ex.Message}",
                            Severity = ConflictSeverity.Critical
                        }
                    }
                };
            }
        }
        
        /// <summary>
        /// Check if the teacher has the qualification to teach a specific course
        /// </summary>
        private bool IsTeacherQualified(SchedulingProblem problem, int teacherId, int courseId)
        {
            // Find the teacher's qualification preference for this course
            var preference = problem.TeacherCoursePreferences
                .FirstOrDefault(tcp => tcp.TeacherId == teacherId && tcp.CourseId == courseId);
                
            // If there is no specific preference record, assume the teacher does not have the qualification
            if (preference == null)
            {
                return false;
            }
            
            // Find the difficulty level of the course (if any)
            var courseDifficulty = 1; // Default difficulty level
            var courseInfo = problem.CourseSections
                .FirstOrDefault(cs => cs.CourseId == courseId);
                
            if (courseInfo != null)
            {
                courseDifficulty = courseInfo.DifficultyLevel;
            }
            
            // Check if the teacher's qualification meets or exceeds the course difficulty
            return preference.ProficiencyLevel >= courseDifficulty;
        }
        
        /// <summary>
        /// Basic level assignment, only consider resource conflict avoidance (teacher/classroom conflict, classroom capacity)
        /// </summary>
        private bool TryAssignWithBasicConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            HashSet<(int roomId, int timeSlotId)> usedRoomTimes,
            HashSet<(int teacherId, int timeSlotId)> usedTeacherTimes,
            Dictionary<string, int> courseTeacherMap,
            SchedulingSolution solution,
            Random random)
        {
            try
            {
                _logger.LogDebug($"Using basic constraints to assign resources for course {section.Id} ({section.CourseName})");
                
                // Filter classrooms with sufficient capacity
                var suitableRooms = problem.Classrooms
                    .Where(room => room.Capacity >= section.Enrollment)
                    .ToList();
                
                // If there are no classrooms with sufficient capacity, select the classroom with the largest capacity
                if (suitableRooms.Count == 0)
                {
                    suitableRooms = problem.Classrooms
                        .OrderByDescending(room => room.Capacity)
                        .Take(1)
                        .ToList();
                    
                    _logger.LogWarning($"Course {section.Id} cannot find a classroom with sufficient capacity, selected the classroom with the largest capacity");
                }
                
                // Prepare the list of all teachers
                var teachers = problem.Teachers.ToList();
                
                // Randomly try multiple times to find a feasible assignment
                int maxAttempts = 15;
                
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // Randomly select a time slot
                    var timeSlots = problem.TimeSlots.ToList();
                    var selectedTimeSlot = timeSlots[random.Next(timeSlots.Count)];
                    
                    // Filter out classrooms that are already used in this time slot
                    var availableRooms = suitableRooms
                        .Where(room => !usedRoomTimes.Contains((room.Id, selectedTimeSlot.Id)))
                        .ToList();
                    
                    if (availableRooms.Count == 0) 
                    {
                        // No available classrooms, try the next time slot
                        continue;
                    }
                    
                    // Randomly select an available classroom
                    var selectedRoom = availableRooms[random.Next(availableRooms.Count)];
                    
                    // Filter out teachers that are already used in this time slot
                    var availableTeachers = teachers
                        .Where(teacher => !usedTeacherTimes.Contains((teacher.Id, selectedTimeSlot.Id)))
                        .ToList();
                    
                    if (availableTeachers.Count == 0) 
                    {
                        // No available teachers, try the next time slot
                        continue;
                    }
                    
                    // Randomly select an available teacher
                    var selectedTeacher = availableTeachers[random.Next(availableTeachers.Count)];
                    
                    // Create an assignment
                    var courseAssignment = new CourseAssignment(section, selectedTeacher, selectedRoom, selectedTimeSlot);
                    
                    // Convert to SchedulingAssignment and add to the solution
                    solution.Assignments.Add(courseAssignment.ToSchedulingAssignment(problem));
                    
                    // Update the used resources
                    usedRoomTimes.Add((selectedRoom.Id, selectedTimeSlot.Id));
                    usedTeacherTimes.Add((selectedTeacher.Id, selectedTimeSlot.Id));
                    
                    _logger.LogDebug($"Successfully assigned resources for course {section.Id}: Time={selectedTimeSlot.DayName} {selectedTimeSlot.StartTime}, Classroom={selectedRoom.Name}, Teacher={selectedTeacher.Name}");
                    
                    return true;
                }
                
                _logger.LogWarning($"After {maxAttempts} attempts, no feasible assignment found for course {section.Id}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred when assigning resources for course {section.Id}");
                return false;
            }
        }
        
        /// <summary>
        /// Consider all hard constraints to assign a course
        /// </summary>
        private bool TryAssignWithAllHardConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            HashSet<(int roomId, int timeSlotId)> usedRoomTimes,
            HashSet<(int teacherId, int timeSlotId)> usedTeacherTimes,
            Dictionary<string, int> courseTeacherMap,
            SchedulingSolution solution,
            Random random)
        {
            try
            {
                // Basic constraint check
                var coreConstraintsSatisfied = TryAssignWithCoreConstraints(problem, section, usedRoomTimes, usedTeacherTimes, courseTeacherMap, solution, random);
                
                if (!coreConstraintsSatisfied)
                {
                    return false;
                }
                
                // Get the latest assigned course
                var assignment = solution.Assignments.Last();
                
                // Check additional variable hard constraints
                
                // 1. Teacher availability
                bool teacherAvailable = true;
                if (assignment.Teacher != null && assignment.TimeSlot != null)
                {
                    teacherAvailable = !problem.TeacherAvailabilities.Any(ta => 
                    ta.TeacherId == assignment.Teacher.Id && 
                    ta.TimeSlotId == assignment.TimeSlot.Id && 
                    !ta.IsAvailable);
                }
                
                if (!teacherAvailable)
                {
                    // Remove the assignment, mark as failed
                    solution.Assignments.RemoveAt(solution.Assignments.Count - 1);
                    if (assignment.Classroom != null && assignment.TimeSlot != null)
                    usedRoomTimes.Remove((assignment.Classroom.Id, assignment.TimeSlot.Id));
                    if (assignment.Teacher != null && assignment.TimeSlot != null)
                    usedTeacherTimes.Remove((assignment.Teacher.Id, assignment.TimeSlot.Id));
                    return false;
                }
                
                // 2. Classroom availability
                bool roomAvailable = true;
                if (assignment.Classroom != null && assignment.TimeSlot != null)
                {
                    roomAvailable = !problem.ClassroomAvailabilities.Any(ca => 
                    ca.ClassroomId == assignment.Classroom.Id && 
                    ca.TimeSlotId == assignment.TimeSlot.Id && 
                    !ca.IsAvailable);
                }
                
                if (!roomAvailable)
                {
                    // Remove the assignment, mark as failed
                    solution.Assignments.RemoveAt(solution.Assignments.Count - 1);
                    if (assignment.Classroom != null && assignment.TimeSlot != null)
                    usedRoomTimes.Remove((assignment.Classroom.Id, assignment.TimeSlot.Id));
                    if (assignment.Teacher != null && assignment.TimeSlot != null)
                    usedTeacherTimes.Remove((assignment.Teacher.Id, assignment.TimeSlot.Id));
                    return false;
                }
                
                // 3. Teacher qualification constraint
                bool teacherQualified = true;
                if (assignment.Teacher != null && section != null)
                {
                    teacherQualified = problem.TeacherCoursePreferences.Any(tcp => 
                    tcp.TeacherId == assignment.Teacher.Id && 
                    tcp.CourseId == section.CourseId && 
                    tcp.ProficiencyLevel >= 2);
                }
                
                if (!teacherQualified)
                {
                    // Remove the assignment, mark as failed
                    solution.Assignments.RemoveAt(solution.Assignments.Count - 1);
                    if (assignment.Classroom != null && assignment.TimeSlot != null)
                    usedRoomTimes.Remove((assignment.Classroom.Id, assignment.TimeSlot.Id));
                    if (assignment.Teacher != null && assignment.TimeSlot != null)
                    usedTeacherTimes.Remove((assignment.Teacher.Id, assignment.TimeSlot.Id));
                    return false;
                }
                
                // All hard constraints are satisfied
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred when assigning course {section.Id}");
                return false;
            }
        }
        
        /// <summary>
        /// Try to assign a course using basic scheduling rules
        /// </summary>
        private bool TryAssignWithCoreConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            HashSet<(int roomId, int timeSlotId)> usedRoomTimes,
            HashSet<(int teacherId, int timeSlotId)> usedTeacherTimes,
            Dictionary<string, int> courseTeacherMap,
            SchedulingSolution solution,
            Random random)
        {
            // First filter classrooms with sufficient capacity (satisfy classroom capacity constraint)
            var suitableRooms = problem.Classrooms
                .Where(room => room.Capacity >= section.Enrollment)
                .ToList();
            
            if (suitableRooms.Count == 0)
            {
                _logger.LogWarning($"Course {section.Id} ({section.CourseName}) has no classrooms with sufficient capacity");
                return false;
            }
            
            // Filter teachers with the qualification to teach the course
            var qualifiedTeachers = problem.Teachers.ToList();
            var teacherPreferences = problem.TeacherCoursePreferences
                .Where(tcp => tcp.CourseId == section.CourseId && tcp.ProficiencyLevel >= 2)
                .ToList();
            
            if (teacherPreferences.Count > 0)
            {
                var preferredTeacherIds = teacherPreferences.Select(tp => tp.TeacherId).ToHashSet();
                qualifiedTeachers = problem.Teachers
                    .Where(t => preferredTeacherIds.Contains(t.Id))
                    .ToList();
            }
            
            // Check if different classes of the same course should be taught by the same teacher
            if (courseTeacherMap.TryGetValue(section.CourseCode, out int existingTeacherId))
            {
                qualifiedTeachers = qualifiedTeachers.Where(t => t.Id == existingTeacherId).ToList();
                if (qualifiedTeachers.Count == 0)
                {
                    _logger.LogWarning($"Course {section.CourseCode} has previously assigned teacher {existingTeacherId}, but current class has no this teacher available");
                    return false;
                }
            }
            
            // Try 20 times randomly to find a feasible assignment
            int maxAttempts = 20;
            
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Calculate the probability weight of each time slot being selected
                var timeSlotCandidates = problem.TimeSlots.ToList();
                var timeSlotWeights = CalculateTimeSlotWeights(timeSlotCandidates);
                
                // Select a time slot randomly based on the weight
                TimeSlotInfo selectedTimeSlot = SelectRandomByWeight(timeSlotCandidates, timeSlotWeights, random);
                
                // Check if the classroom is in conflict (whether it has been used in this time slot)
                var availableRooms = suitableRooms
                    .Where(room => !usedRoomTimes.Contains((room.Id, selectedTimeSlot.Id)))
                    .ToList();
                
                if (availableRooms.Count == 0) continue; // No available classrooms, try the next time slot
                
                var selectedRoom = availableRooms[random.Next(availableRooms.Count)];
                
                // Check if the teacher is in conflict (whether it has been assigned in this time slot)
                var availableTeachers = qualifiedTeachers
                    .Where(teacher => !usedTeacherTimes.Contains((teacher.Id, selectedTimeSlot.Id)))
                    .ToList();
                
                if (availableTeachers.Count == 0) continue; // No available teachers, try the next time slot
                
                var selectedTeacher = availableTeachers[random.Next(availableTeachers.Count)];
                
                // Create an assignment
                var courseAssignment = new CourseAssignment(section, selectedTeacher, selectedRoom, selectedTimeSlot);
                
                // Convert to SchedulingAssignment and add to the solution
                solution.Assignments.Add(courseAssignment.ToSchedulingAssignment(problem));
                
                // Update the used resources
                usedRoomTimes.Add((selectedRoom.Id, selectedTimeSlot.Id));
                usedTeacherTimes.Add((selectedTeacher.Id, selectedTimeSlot.Id));
                
                // Record the course-teacher mapping
                if (!courseTeacherMap.ContainsKey(section.CourseCode))
                {
                    courseTeacherMap[section.CourseCode] = selectedTeacher.Id;
                }
                
                return true;
            }
            
            return false; // Try failed
        }
        
        /// <summary>
        /// Standard level constraint assignment, consider teacher qualification, classroom capacity, teacher preference, etc.
        /// </summary>
        private bool TryAssignWithStandardConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            HashSet<(int roomId, int timeSlotId)> usedRoomTimes,
            HashSet<(int teacherId, int timeSlotId)> usedTeacherTimes,
            Dictionary<string, int> courseTeacherMap,
            SchedulingSolution solution,
            Random random)
        {
            try
            {
                _logger.LogDebug($"Using standard constraints to assign resources for course {section.Id} ({section.CourseName})");
                
                // Filter classrooms with sufficient capacity
            var suitableRooms = problem.Classrooms
                .Where(room => room.Capacity >= section.Enrollment)
                .ToList();
                
            if (suitableRooms.Count == 0)
            {
                    _logger.LogWarning($"Course {section.Id} has no classrooms with sufficient capacity");
                    return false;
                }
                
                // Filter teachers with the qualification to teach the course
            var qualifiedTeachers = problem.Teachers.ToList();
            var teacherPreferences = problem.TeacherCoursePreferences
                .Where(tcp => tcp.CourseId == section.CourseId && tcp.ProficiencyLevel >= 1)
                .ToList();
            
            if (teacherPreferences.Count > 0)
            {
                var preferredTeacherIds = teacherPreferences.Select(tp => tp.TeacherId).ToHashSet();
                    qualifiedTeachers = problem.Teachers
                    .Where(t => preferredTeacherIds.Contains(t.Id))
                    .ToList();
                    
                    if (qualifiedTeachers.Count == 0)
                    {
                        _logger.LogWarning($"Course {section.Id} has no qualified teachers");
                        qualifiedTeachers = problem.Teachers.ToList(); // Fallback to all teachers
                    }
                }
                
                // Check if different classes of the same course should be taught by the same teacher
                if (courseTeacherMap.TryGetValue(section.CourseCode, out int existingTeacherId))
                {
                    var sameTeachers = qualifiedTeachers.Where(t => t.Id == existingTeacherId).ToList();
                    if (sameTeachers.Count > 0)
                    {
                        qualifiedTeachers = sameTeachers;
                        _logger.LogDebug($"Course {section.CourseCode} has previously assigned teacher {existingTeacherId}, will use the same teacher");
                    }
                }
                
                // Try multiple times randomly to find a feasible assignment
                int maxAttempts = 30;
                
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // Randomly select a time slot
                    var timeSlots = problem.TimeSlots.ToList();
                    var selectedTimeSlot = timeSlots[random.Next(timeSlots.Count)];
                    
                    // Filter out classrooms that have been used
                    var availableRooms = suitableRooms
                        .Where(room => !usedRoomTimes.Contains((room.Id, selectedTimeSlot.Id)))
                        .ToList();
                    
                    if (availableRooms.Count == 0) 
                    {
                        // No available classrooms, try the next time slot
                        continue;
                    }
                    
                    // Filter out classrooms that are not available
                    availableRooms = availableRooms
                        .Where(room => !problem.ClassroomAvailabilities.Any(ca => 
                            ca.ClassroomId == room.Id && 
                            ca.TimeSlotId == selectedTimeSlot.Id && 
                            !ca.IsAvailable))
                        .ToList();
                        
                    if (availableRooms.Count == 0) 
                    {
                        // No available classrooms, try the next time slot
                        continue;
                    }
                    
                    // Randomly select an available classroom
                    var selectedRoom = availableRooms[random.Next(availableRooms.Count)];
                    
                    // Filter out teachers that have been assigned
                    var availableTeachers = qualifiedTeachers
                        .Where(teacher => !usedTeacherTimes.Contains((teacher.Id, selectedTimeSlot.Id)))
                        .ToList();
                        
                    if (availableTeachers.Count == 0) 
                    {
                        // No available teachers, try the next time slot
                        continue;
                    }
                    
                    // Filter out teachers that are not available
                    availableTeachers = availableTeachers
                        .Where(teacher => !problem.TeacherAvailabilities.Any(ta => 
                            ta.TeacherId == teacher.Id && 
                            ta.TimeSlotId == selectedTimeSlot.Id && 
                            !ta.IsAvailable))
                    .ToList();

                    if (availableTeachers.Count == 0) 
                    {
                        // No available teachers, try the next time slot
                        continue;
                    }
                    
                    // Prioritize teachers with higher course preferences
                    var preferredAvailableTeachers = availableTeachers
                        .Where(teacher => teacherPreferences.Any(tp => 
                            tp.TeacherId == teacher.Id && 
                            tp.CourseId == section.CourseId && 
                            tp.ProficiencyLevel >= 2))
                    .ToList();

                    if (preferredAvailableTeachers.Count > 0)
                    {
                        availableTeachers = preferredAvailableTeachers;
                    }
                    
                    // Randomly select an available teacher
                    var selectedTeacher = availableTeachers[random.Next(availableTeachers.Count)];
                    
                    // Create an assignment
                    var courseAssignment = new CourseAssignment(section, selectedTeacher, selectedRoom, selectedTimeSlot);
                    
                    // Convert to SchedulingAssignment and add to the solution
                    solution.Assignments.Add(courseAssignment.ToSchedulingAssignment(problem));
                    
                    // Update the used resources
                    usedRoomTimes.Add((selectedRoom.Id, selectedTimeSlot.Id));
                    usedTeacherTimes.Add((selectedTeacher.Id, selectedTimeSlot.Id));
                    
                    // Update the course-teacher mapping
                    if (!courseTeacherMap.ContainsKey(section.CourseCode))
                    {
                        courseTeacherMap[section.CourseCode] = selectedTeacher.Id;
                    }
                    
                    _logger.LogDebug($"Successfully assigned resources for course {section.Id}: time={selectedTimeSlot.DayName} {selectedTimeSlot.StartTime}, classroom={selectedRoom.Name}, teacher={selectedTeacher.Name}");
                    
                    return true;
                }
                
                _logger.LogWarning($"After trying {maxAttempts} times, failed to find a feasible assignment for course {section.Id}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred when assigning resources for course {section.Id}");
                return false;
            }
        }

        /// <summary>
        /// Ignore most constraints, force assignment course (only keep the minimum constraints)
        /// </summary>
        private void ForceAssignmentWithMinimalConstraints(
            SchedulingProblem problem, 
            CourseSectionInfo section,
            SchedulingSolution solution,
            Random random)
        {
            try
            {
                // Randomly select a time slot (no bias)
                var timeSlotCandidates = problem.TimeSlots.ToList();
                var selectedTimeSlot = timeSlotCandidates[random.Next(timeSlotCandidates.Count)];
                
                // Try to select a classroom with sufficient capacity
                var suitableRooms = problem.Classrooms
                    .Where(room => room.Capacity >= section.Enrollment)
                    .ToList();
                    
                if (suitableRooms.Count == 0)
                {
                    suitableRooms = problem.Classrooms
                        .OrderByDescending(room => room.Capacity)
                        .Take(1)
                        .ToList();
                }
                
                var selectedRoom = suitableRooms[random.Next(suitableRooms.Count)];
                
                // Try to select a teacher with qualification
                var qualifiedTeachers = problem.Teachers.ToList();
                var teacherPreferences = problem.TeacherCoursePreferences
                    .Where(tcp => tcp.CourseId == section.CourseId && tcp.ProficiencyLevel >= 1)
                    .ToList();
                
                if (teacherPreferences.Count > 0)
                {
                    var preferredTeacherIds = teacherPreferences.Select(tp => tp.TeacherId).ToHashSet();
                    var preferredTeachers = problem.Teachers
                        .Where(t => preferredTeacherIds.Contains(t.Id))
                        .ToList();
                        
                    if (preferredTeachers.Count > 0)
                    {
                        qualifiedTeachers = preferredTeachers;
                    }
                }
                
                // Prioritize teachers that are available in the current time slot
                var availableTeachers = qualifiedTeachers
                    .Where(teacher => !problem.TeacherAvailabilities.Any(ta => 
                        ta.TeacherId == teacher.Id && 
                        ta.TimeSlotId == selectedTimeSlot.Id && 
                        !ta.IsAvailable))
                    .ToList();

                TeacherInfo selectedTeacher;
                if (availableTeachers.Count > 0)
                {
                    selectedTeacher = availableTeachers[random.Next(availableTeachers.Count)];
                    _logger.LogInformation($"In minimal constraint mode, found a teacher {selectedTeacher.Id} available at time slot {selectedTimeSlot.Id}");
                }
                else
                {
                    // If no teacher satisfies the availability constraint, fallback to all teachers
                    selectedTeacher = qualifiedTeachers[random.Next(qualifiedTeachers.Count)];
                    _logger.LogWarning($"In minimal constraint mode, no teacher available at time slot {selectedTimeSlot.Id}, randomly selected teacher {selectedTeacher.Id}, which may violate the availability constraint");
                }
                
                // Create an assignment
                var courseAssignment = new CourseAssignment(section, selectedTeacher, selectedRoom, selectedTimeSlot);
                
                _logger.LogWarning($"Force assign course {section.Id} to time slot {selectedTimeSlot.Id}, classroom {selectedRoom.Id}, teacher {selectedTeacher.Id}");
                
                // Convert to SchedulingAssignment and add to the solution
                solution.Assignments.Add(courseAssignment.ToSchedulingAssignment(problem));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred when force assigning course {section.Id}");
                
                // Even if an exception occurs, try to create a simple assignment
                try
                {
                    var timeSlot = problem.TimeSlots.ElementAt(random.Next(problem.TimeSlots.Count()));
                    var room = problem.Classrooms.ElementAt(random.Next(problem.Classrooms.Count()));
                    var teacher = problem.Teachers.ElementAt(random.Next(problem.Teachers.Count()));
                    
                    var courseAssignment = new CourseAssignment(section, teacher, room, timeSlot);
                    
                    solution.Assignments.Add(courseAssignment.ToSchedulingAssignment(problem));
                    _logger.LogWarning($"Exception recovery: created a fallback assignment for course {section.Id}");
                }
                catch
                {
                    _logger.LogError($"Failed to create any assignment for course {section.Id}, this course will be ignored");
                }
            }
        }
        
        /// <summary>
        /// Calculate the selection weights for each time slot
        /// </summary>
        private List<double> CalculateTimeSlotWeights(List<TimeSlotInfo> timeSlots)
        {
            // Simple implementation: all time slots have equal weights
            return timeSlots.Select(_ => 1.0).ToList();
        }

        /// <summary>
        /// Select an element randomly based on weights
        /// </summary>
        private T SelectRandomByWeight<T>(List<T> items, List<double> weights, Random random)
        {
            if (items.Count == 0) 
                throw new ArgumentException("The item list cannot be empty");
            
            if (items.Count != weights.Count)
                throw new ArgumentException("The number of items and weights must be the same");
            
            // Simple implementation: if all weights are equal, select randomly
            if (weights.All(w => Math.Abs(w - weights[0]) < 0.0001))
            {
                return items[random.Next(items.Count)];
            }
            
            // Calculate the sum of weights
            double totalWeight = weights.Sum();
            // Select a random weight position
            double randomValue = random.NextDouble() * totalWeight;
            
            // Find the corresponding item
            double cumulativeWeight = 0;
            for (int i = 0; i < items.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue <= cumulativeWeight)
                    return items[i];
            }
            
            // If there is a floating point error, return the last item
            return items[items.Count - 1];
        }

        /// <summary>
        /// Validate the completeness and validity of problem data
        /// </summary>
        private void ValidateProblemData(SchedulingProblem problem)
        {
            if (problem == null)
                throw new ArgumentNullException(nameof(problem), "The scheduling problem data cannot be empty");
                
            if (problem.CourseSections == null || !problem.CourseSections.Any())
                throw new ArgumentException("The scheduling problem must contain at least one course section", nameof(problem));
                
            if (problem.Teachers == null || !problem.Teachers.Any())
                throw new ArgumentException("The scheduling problem must contain at least one teacher", nameof(problem));
                
            if (problem.Classrooms == null || !problem.Classrooms.Any())
                throw new ArgumentException("The scheduling problem must contain at least one classroom", nameof(problem));
                
            if (problem.TimeSlots == null || !problem.TimeSlots.Any())
                throw new ArgumentException("The scheduling problem must contain at least one time slot", nameof(problem));
                
            _logger.LogInformation($"Problem data validation passed: {problem.CourseSections.Count()} course sections, " +
                                 $"{problem.Teachers.Count()} teachers, " +
                                 $"{problem.Classrooms.Count()} classrooms, " +
                                 $"{problem.TimeSlots.Count()} time slots");
        }
        
        /// <summary>
        /// Debug output problem data
        /// </summary>
        private void DebugProblemData(SchedulingProblem problem)
        {
            _logger.LogDebug($"Problem details: {problem.Name}");
            _logger.LogDebug($"Course section count: {problem.CourseSections.Count()}");
            _logger.LogDebug($"Teacher count: {problem.Teachers.Count()}");
            _logger.LogDebug($"Classroom count: {problem.Classrooms.Count()}");
            _logger.LogDebug($"Time slot count: {problem.TimeSlots.Count()}");
            _logger.LogDebug($"Teacher course preference count: {problem.TeacherCoursePreferences.Count()}");
            _logger.LogDebug($"Teacher availability count: {problem.TeacherAvailabilities.Count()}");
            _logger.LogDebug($"Classroom availability count: {problem.ClassroomAvailabilities.Count()}");
        }
        
        /// <summary>
        /// Extract the variable dictionary from the CP model
        /// </summary>
        private Dictionary<string, IntVar> ExtractVariablesDictionary(CpModel model)
        {
            // Simplified implementation, actually should extract variables from the model
            return new Dictionary<string, IntVar>();
        }
        
        /// <summary>
        /// Collect partial solution
        /// </summary>
        private SchedulingSolution CollectPartialSolution(SchedulingProblem problem, CpModel model, CpSolver solver)
        {
            // Create an empty solution
            var solution = new SchedulingSolution
            {
                Id = new Random().Next(1, 1000000),  // Use a random number as ID, not GUID string
                ProblemId = problem.Id,
                Name = $"Partial solution_{DateTime.Now:yyyyMMdd_HHmmss}",
                ConstraintLevel = ConstraintApplicationLevel.Basic,
                Algorithm = "CP_Partial"
            };
            
            try
            {
                // Use random assignment as a backup
                var random = new Random();
                
                // Try to create a basic assignment for each course
                foreach (var section in problem.CourseSections)
                {
                    try
                    {
                        // Randomly select resources
                        var timeSlot = problem.TimeSlots.ElementAt(random.Next(problem.TimeSlots.Count()));
                        var room = problem.Classrooms.ElementAt(random.Next(problem.Classrooms.Count()));
                        var teacher = problem.Teachers.ElementAt(random.Next(problem.Teachers.Count()));
                        
                        // Create a simple assignment
                        var assignment = new SchedulingAssignment
                        {
                            SectionId = section.Id,
                            CourseSection = section,
                            TeacherId = teacher.Id,
                            Teacher = teacher,
                            ClassroomId = room.Id,
                            Classroom = room,
                            TimeSlotId = timeSlot.Id,
                            TimeSlot = timeSlot
                        };
                        
                        solution.Assignments.Add(assignment);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error occurred when creating a partial solution for course {section.Id}");
                    }
                }
                
                _logger.LogInformation($"Created a partial solution, containing {solution.Assignments.Count} assignments");
                return solution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred when creating a partial solution");
                return null;
            }
        }

        /// <summary>
        /// Check the feasibility of the solution
        /// </summary>
        public bool CheckFeasibility(SchedulingSolution solution, SchedulingProblem problem)
        {
            if (solution == null || problem == null)
                return false;
                
            try
            {
                // Record used resources, used to detect conflicts
                var usedRoomTimes = new HashSet<(int roomId, int timeSlotId)>();
                var usedTeacherTimes = new HashSet<(int teacherId, int timeSlotId)>();
                
                // Check each assignment
                foreach (var assignment in solution.Assignments)
                {
                    // Check classroom conflicts
                    var roomTimeKey = (assignment.ClassroomId, assignment.TimeSlotId);
                    if (usedRoomTimes.Contains(roomTimeKey))
                    {
                        _logger.LogWarning($"Classroom conflict: classroom {assignment.ClassroomId} is used multiple times at time slot {assignment.TimeSlotId}");
                        return false;
                    }
                    
                    // Check teacher conflicts
                    var teacherTimeKey = (assignment.TeacherId, assignment.TimeSlotId);
                    if (usedTeacherTimes.Contains(teacherTimeKey))
                    {
                        _logger.LogWarning($"Teacher conflict: teacher {assignment.TeacherId} is assigned to time slot {assignment.TimeSlotId} multiple times");
                        return false;
                    }
                    
                    // Record used resources
                    usedRoomTimes.Add(roomTimeKey);
                    usedTeacherTimes.Add(teacherTimeKey);
                }
                
                _logger.LogInformation("Solution feasibility check passed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred when checking the feasibility of the solution");
                return false;
            }
        }
    }
}