// SmartSchedulingSystem.Scheduling/Engine/ClassroomConflictHandler.cs
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
    public class ClassroomConflictHandler : IConflictHandler
    {
        private readonly ILogger<ClassroomConflictHandler> _logger;
        private readonly MoveGenerator _moveGenerator;
        private readonly SolutionEvaluator _evaluator;

        public SchedulingConflictType ConflictType => SchedulingConflictType.ClassroomConflict;

        public ClassroomConflictHandler(
            ILogger<ClassroomConflictHandler> logger,
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

            // Access to course assignments involved in the conflict
            var involvedSectionIds = conflict.InvolvedEntities.TryGetValue("Sections", out var sections)
                ? sections
                : new List<int>();

            var involvedClassroomIds = conflict.InvolvedEntities.TryGetValue("Classrooms", out var classrooms)
                ? classrooms
                : new List<int>();

            if (involvedSectionIds.Count < 2 || involvedClassroomIds.Count < 1)
            {
                _logger.LogWarning("Incomplete classroom conflict information, cannot generate solution");
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

            foreach (var assignment in assignments)
            {
                // 1. Generate alternative classroom moves
                var availableRooms = _moveGenerator.GenerateValidMoves(
                    solution, assignment)
                    .OfType<RoomMove>()
                    .ToList();

                foreach (var move in availableRooms)
                {
                    var roomMove = (RoomMove)move;

                    // Get new classroom information
                    var newRoom = solution.Problem?.Classrooms
                        .FirstOrDefault(r => r.Id == roomMove.NewClassroomId);

                    if (newRoom == null)
                        continue;

                    // Create solution option
                    var option = new ConflictResolutionOption
                    {
                        Id = options.Count + 1,
                        ConflictId = conflict.Id,
                        Description = $"Move course {assignment.SectionCode} to classroom {newRoom.Name}",
                        Compatibility = 90, // Very high compatibility
                        Impacts = new List<string>
                        {
                            "Change the classroom where the course is held",
                            "May affect the availability of teaching equipment"
                        },
                        Actions = new List<ResolutionAction>
                        {
                            new ReassignClassroomAction
                            {
                                AssignmentId = assignment.Id,
                                NewClassroomId = newRoom.Id,
                                NewClassroomName = newRoom.Name
                            }
                        }
                    };

                    options.Add(option);

                    // Limit the number of options
                    if (options.Count >= 5)
                        break;
                }

                if (options.Count >= 5)
                    break;

                // 2. Generate time moves
                var timeMovesOptions = _moveGenerator.GenerateValidMoves(
                    solution, assignment)
                    .OfType<TimeMove>()
                    .Take(3)
                    .ToList();

                foreach (var move in timeMovesOptions)
                {
                    var timeMove = (TimeMove)move;

                    // Get new time slot information
                    var newTimeSlot = solution.Problem?.TimeSlots
                        .FirstOrDefault(t => t.Id == timeMove.NewTimeSlotId);

                    if (newTimeSlot == null)
                        continue;

                    // Create solution option
                    var option = new ConflictResolutionOption
                    {
                        Id = options.Count + 1,
                        ConflictId = conflict.Id,
                        Description = $"Adjust course {assignment.SectionCode} to {newTimeSlot.DayName} {newTimeSlot.StartTime}-{newTimeSlot.EndTime}",
                        Compatibility = 70, // Medium compatibility
                        Impacts = new List<string>
                        {
                            "Change the course time",
                            "May affect student and teacher arrangements"
                        },
                        Actions = new List<ResolutionAction>
                        {
                            new ReassignTimeSlotAction
                            {
                                AssignmentId = assignment.Id,
                                NewTimeSlotId = newTimeSlot.Id,
                                NewDayOfWeek = newTimeSlot.DayOfWeek,
                                NewStartTime = newTimeSlot.StartTime,
                                NewEndTime = newTimeSlot.EndTime
                            }
                        }
                    };

                    options.Add(option);

                    // Limit the number of options
                    if (options.Count >= 8)
                        break;
                }

                if (options.Count >= 8)
                    break;
            }

            // 3. If there are two related assignments, consider swap operations
            if (assignments.Count >= 2)
            {
                var assignment1 = assignments[0];
                var assignment2 = assignments[1];

                // Create classroom swap options
                var swapOption = new ConflictResolutionOption
                {
                    Id = options.Count + 1,
                    ConflictId = conflict.Id,
                    Description = $"Swap the time of courses {assignment1.SectionCode} and {assignment2.SectionCode}",
                    Compatibility = 85,
                    Impacts = new List<string>
                    {
                        "Swap the time of two courses",
                        "Avoid changing classroom assignments"
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

            // Sort conflicts by severity
            var sortedConflicts = conflicts
                .OrderByDescending(c => c.Severity)
                .ThenByDescending(c => c.InvolvedEntities?.GetValueOrDefault("Sections")?.Count ?? 0)
                .ToList();

            foreach (var conflict in sortedConflicts)
            {
                // Generate resolution options for each conflict
                var options = await GetResolutionOptionsAsync(conflict, resolvedSolution, cancellationToken);

                // Select the best option
                var bestOption = SelectBestOption(options, resolvedSolution);

                if (bestOption != null)
                {
                    // Apply the solution
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
                // Clone the solution
                var tempSolution = solution.Clone();

                // Apply the option
                foreach (var action in option.Actions)
                {
                    action.Execute(tempSolution);
                }

                // Evaluate the solution
                double score = _evaluator.Evaluate(tempSolution).Score;

                // Consider the weight of option compatibility
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
}