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

            // 获取冲突涉及的课程分配
            var involvedSectionIds = conflict.InvolvedEntities.TryGetValue("Sections", out var sections)
                ? sections
                : new List<int>();

            var involvedClassroomIds = conflict.InvolvedEntities.TryGetValue("Classrooms", out var classrooms)
                ? classrooms
                : new List<int>();

            if (involvedSectionIds.Count < 2 || involvedClassroomIds.Count < 1)
            {
                _logger.LogWarning("教室冲突信息不完整，无法生成解决方案");
                return options;
            }

            // 获取相关分配
            var assignments = solution.Assignments
                .Where(a => involvedSectionIds.Contains(a.SectionId))
                .ToList();

            if (assignments.Count < 2)
            {
                _logger.LogWarning("未找到冲突相关的分配");
                return options;
            }

            foreach (var assignment in assignments)
            {
                // 1. 生成替代教室移动
                var availableRooms = _moveGenerator.GenerateValidMoves(
                    solution, assignment)
                    .OfType<RoomMove>()
                    .ToList();

                foreach (var move in availableRooms)
                {
                    var roomMove = (RoomMove)move;

                    // 获取新教室信息
                    var newRoom = solution.Problem?.Classrooms
                        .FirstOrDefault(r => r.Id == roomMove.NewClassroomId);

                    if (newRoom == null)
                        continue;

                    // 创建解决方案选项
                    var option = new ConflictResolutionOption
                    {
                        Id = options.Count + 1,
                        ConflictId = conflict.Id,
                        Description = $"将课程 {assignment.SectionCode} 移动到教室 {newRoom.Name}",
                        Compatibility = 90, // 很高兼容性
                        Impacts = new List<string>
                        {
                            "改变课程所在教室",
                            "可能影响教学设备可用性"
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

                    // 限制选项数量
                    if (options.Count >= 5)
                        break;
                }

                if (options.Count >= 5)
                    break;

                // 2. 生成时间移动
                var timeMovesOptions = _moveGenerator.GenerateValidMoves(
                    solution, assignment)
                    .OfType<TimeMove>()
                    .Take(3)
                    .ToList();

                foreach (var move in timeMovesOptions)
                {
                    var timeMove = (TimeMove)move;

                    // 获取新时间槽信息
                    var newTimeSlot = solution.Problem?.TimeSlots
                        .FirstOrDefault(t => t.Id == timeMove.NewTimeSlotId);

                    if (newTimeSlot == null)
                        continue;

                    // 创建解决方案选项
                    var option = new ConflictResolutionOption
                    {
                        Id = options.Count + 1,
                        ConflictId = conflict.Id,
                        Description = $"将课程 {assignment.SectionCode} 调整到 {newTimeSlot.DayName} {newTimeSlot.StartTime}-{newTimeSlot.EndTime}",
                        Compatibility = 70, // 中等兼容性
                        Impacts = new List<string>
                        {
                            "改变课程时间",
                            "可能影响学生和教师安排"
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

                    // 限制选项数量
                    if (options.Count >= 8)
                        break;
                }

                if (options.Count >= 8)
                    break;
            }

            // 3. 如果有两个相关分配，考虑交换操作
            if (assignments.Count >= 2)
            {
                var assignment1 = assignments[0];
                var assignment2 = assignments[1];

                // 创建教室交换选项
                var swapOption = new ConflictResolutionOption
                {
                    Id = options.Count + 1,
                    ConflictId = conflict.Id,
                    Description = $"交换课程 {assignment1.SectionCode} 和 {assignment2.SectionCode} 的时间",
                    Compatibility = 85,
                    Impacts = new List<string>
                    {
                        "两门课程时间互换",
                        "避免改变教室分配"
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

            // 创建解决方案的副本
            var resolvedSolution = solution.Clone();

            // 应用所有解决操作
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

            // 按冲突严重程度排序
            var sortedConflicts = conflicts
                .OrderByDescending(c => c.Severity)
                .ThenByDescending(c => c.InvolvedEntities?.GetValueOrDefault("Sections")?.Count ?? 0)
                .ToList();

            foreach (var conflict in sortedConflicts)
            {
                // 为每个冲突生成解决选项
                var options = await GetResolutionOptionsAsync(conflict, resolvedSolution, cancellationToken);

                // 选择最佳选项
                var bestOption = SelectBestOption(options, resolvedSolution);

                if (bestOption != null)
                {
                    // 应用解决方案
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

            // 为每个选项评分
            var scoredOptions = new List<(ConflictResolutionOption Option, double Score)>();

            foreach (var option in options)
            {
                // 克隆解决方案
                var tempSolution = solution.Clone();

                // 应用选项
                foreach (var action in option.Actions)
                {
                    action.Execute(tempSolution);
                }

                // 评估解决方案
                double score = _evaluator.Evaluate(tempSolution).Score;

                // 考虑选项兼容性的权重
                score = score * 0.8 + (option.Compatibility / 100.0) * 0.2;

                scoredOptions.Add((option, score));
            }

            // 返回评分最高的选项
            return scoredOptions
                .OrderByDescending(so => so.Score)
                .FirstOrDefault()
                .Option;
        }
    }
}