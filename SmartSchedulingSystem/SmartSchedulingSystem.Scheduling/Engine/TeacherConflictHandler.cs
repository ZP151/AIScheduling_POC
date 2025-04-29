// 创建TeacherConflictHandler.cs实现冲突处理
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Algorithms.LS;
using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Engine
{
    public class TeacherConflictHandler : IConflictHandler
    {
        private readonly ILogger<TeacherConflictHandler> _logger;
        private readonly MoveGenerator _moveGenerator;
        private readonly SolutionEvaluator _evaluator;

        public SchedulingConflictType ConflictType => SchedulingConflictType.TeacherConflict;

        public TeacherConflictHandler(
            ILogger<TeacherConflictHandler> logger,
            MoveGenerator moveGenerator,
            SolutionEvaluator evaluator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _moveGenerator = moveGenerator ?? throw new ArgumentNullException(nameof(moveGenerator));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public async Task<IEnumerable<ConflictResolutionOption>> GetResolutionOptionsAsync(
            SchedulingConflict conflict,
            SchedulingSolution solution,
            CancellationToken cancellationToken = default)
        {
            var options = new List<ConflictResolutionOption>();

            // Get course assignments involved in the conflict
            var involvedSectionIds = conflict.InvolvedEntities.TryGetValue("Sections", out var sections)
                ? sections
                : new List<int>();

            if (involvedSectionIds.Count < 2)
            {
                _logger.LogWarning("Teacher conflict information is incomplete, cannot generate solution");
                return options;
            }

            // Get related assignments
            var assignments = solution.Assignments
                .Where(a => involvedSectionIds.Contains(a.SectionId))
                .ToList();

            if (assignments.Count < 2)
            {
                _logger.LogWarning("No related assignments found");
                return options;
            }

            // Generate moves for each assignment
            foreach (var assignment in assignments)
            {
                // 1. Generate time move
                var availableTimeSlots = _moveGenerator.GenerateValidMoves(
                    solution, assignment)
                    .OfType<TimeMove>()
                    .ToList();

                foreach (var move in availableTimeSlots)
                {
                    // Create solution option
                    var option = new ConflictResolutionOption
                    {
                        Id = options.Count + 1,
                        ConflictId = conflict.Id,
                        Description = $"Move course {assignment.SectionCode} to other time slot",
                        Compatibility = 80, // Higher compatibility
                        Impacts = new List<string>
                        {
                            "Change course time",
                            "May affect student schedule"
                        },
                        Actions = new List<ResolutionAction>
                        {
                            new ReassignTimeSlotAction
                            {
                                AssignmentId = assignment.Id,
                                NewTimeSlotId = ((TimeMove)move).NewTimeSlotId
                            }
                        }
                    };

                    options.Add(option);

                    // Limit option count
                    if (options.Count >= 5)
                        break;
                }

                if (options.Count >= 5)
                    break;

                // 2. Generate teacher move
                var availableTeachers = _moveGenerator.GenerateValidMoves(
                    solution, assignment)
                    .OfType<TeacherMove>()
                    .ToList();

                foreach (var move in availableTeachers)
                {
                    var teacherMove = (TeacherMove)move;

                    // Get new teacher information
                    var newTeacher = solution.Problem?.Teachers
                        .FirstOrDefault(t => t.Id == teacherMove.NewTeacherId);

                    if (newTeacher == null)
                        continue;

                    // Create solution option
                    var option = new ConflictResolutionOption
                    {
                        Id = options.Count + 1,
                        ConflictId = conflict.Id,
                        Description = $"Assign course {assignment.SectionCode} to teacher {newTeacher.Name}",
                        Compatibility = 70, // Medium compatibility
                        Impacts = new List<string>
                        {
                            "Change the teacher",
                            "May affect teaching quality"
                        },
                        Actions = new List<ResolutionAction>
                        {
                            new ReassignTeacherAction
                            {
                                AssignmentId = assignment.Id,
                                NewTeacherId = newTeacher.Id,
                                NewTeacherName = newTeacher.Name
                            }
                        }
                    };

                    options.Add(option);

                    // Limit option count
                    if (options.Count >= 8)
                        break;
                }

                if (options.Count >= 8)
                    break;
            }

            // 3. Generate course swap move
            if (assignments.Count >= 2)
            {
                var assignment1 = assignments[0];
                var assignment2 = assignments[1];

                // Create time swap option
                var swapOption = new ConflictResolutionOption
                {
                    Id = options.Count + 1,
                    ConflictId = conflict.Id,
                    Description = $"Swap the time of course {assignment1.SectionCode} and {assignment2.SectionCode}",
                    Compatibility = 90, // High compatibility
                    Impacts = new List<string>
                    {
                        "Keep teacher assignments unchanged",
                        "Only change course time order"
                    },
                    Actions = new List<ResolutionAction>
                    {
                        new SwapTimeAction
                        {
                            Assignment1Id = assignment1.Id,
                            Assignment2Id = assignment2.Id
                        }
                    }
                };

                options.Add(swapOption);
            }

            return options;
        }

        public async Task<SchedulingSolution> ApplyResolutionAsync(
            ConflictResolutionOption option,
            SchedulingSolution solution,
            CancellationToken cancellationToken = default)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            // Create a copy of the solution
            var resolvedSolution = solution.Clone();

            // Apply all resolution actions
            foreach (var action in option.Actions)
            {
                action.Execute(resolvedSolution);
            }

            return resolvedSolution;
        }

        public async Task<SchedulingSolution> ResolveBatchAsync(
            IEnumerable<SchedulingConflict> conflicts,
            SchedulingSolution solution,
            CancellationToken cancellationToken = default)
        {
            if (conflicts == null)
                throw new ArgumentNullException(nameof(conflicts));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var resolvedSolution = solution.Clone();

            // Sort conflicts by priority (severity, number of affected courses, etc.)
            var sortedConflicts = conflicts
                .OrderByDescending(c => c.Severity)
                .ThenByDescending(c => c.InvolvedEntities?.GetValueOrDefault("Sections")?.Count ?? 0)
                .ToList();

            foreach (var conflict in sortedConflicts)
            {
                // Generate resolution options
                var options = await GetResolutionOptionsAsync(conflict, resolvedSolution, cancellationToken);

                // Select the best option
                var bestOption = SelectBestOption(options, resolvedSolution);

                if (bestOption != null)
                {
                    // Apply solution
                    resolvedSolution = await ApplyResolutionAsync(bestOption, resolvedSolution, cancellationToken);
                }
            }

            return resolvedSolution;
        }

        private ConflictResolutionOption SelectBestOption(
            IEnumerable<ConflictResolutionOption> options,
            SchedulingSolution solution)
        {
            if (options == null || !options.Any())
                return null;

            // Score each option
            var scoredOptions = new List<(ConflictResolutionOption Option, double Score)>();

            foreach (var option in options)
            {
                // Clone solution
                var tempSolution = solution.Clone();

                // Apply option
                foreach (var action in option.Actions)
                {
                    action.Execute(tempSolution);
                }

                // Evaluate solution
                double score = _evaluator.Evaluate(tempSolution).Score;

                // Add option compatibility weight
                score = score * 0.8 + (option.Compatibility / 100.0) * 0.2;

                scoredOptions.Add((option, score));
            }

            // Return the option with the highest score
            return scoredOptions
                .OrderByDescending(so => so.Score)
                .FirstOrDefault()
                .Option;
        }
    }

    // Add swap time operation
    public class SwapTimeAction : ResolutionAction
    {
        public int Assignment1Id { get; set; }
        public int Assignment2Id { get; set; }

        public SwapTimeAction()
        {
            Type = ResolutionActionType.Other;
        }

        public override void Execute(SchedulingSolution solution)
        {
            var assignment1 = solution.Assignments.FirstOrDefault(a => a.Id == Assignment1Id);
            var assignment2 = solution.Assignments.FirstOrDefault(a => a.Id == Assignment2Id);

            if (assignment1 != null && assignment2 != null)
            {
                // Swap time slot
                int tempTimeSlotId = assignment1.TimeSlotId;
                assignment1.TimeSlotId = assignment2.TimeSlotId;
                assignment2.TimeSlotId = tempTimeSlotId;

                // Swap date and time information
                int tempDayOfWeek = assignment1.DayOfWeek;
                TimeSpan tempStartTime = assignment1.StartTime;
                TimeSpan tempEndTime = assignment1.EndTime;

                assignment1.DayOfWeek = assignment2.DayOfWeek;
                assignment1.StartTime = assignment2.StartTime;
                assignment1.EndTime = assignment2.EndTime;

                assignment2.DayOfWeek = tempDayOfWeek;
                assignment2.StartTime = tempStartTime;
                assignment2.EndTime = tempEndTime;
            }
        }
    }
}