// 创建TeacherConflictHandler.cs实现冲突处理
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Engine.LS;
using SmartSchedulingSystem.Scheduling.Engine.LS.Moves;
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

            // 获取冲突涉及的课程分配
            var involvedSectionIds = conflict.InvolvedEntities.TryGetValue("Sections", out var sections)
                ? sections
                : new List<int>();

            if (involvedSectionIds.Count < 2)
            {
                _logger.LogWarning("教师冲突信息不完整，无法生成解决方案");
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

            // 为每个分配生成移动
            foreach (var assignment in assignments)
            {
                // 1. 生成时间移动
                var availableTimeSlots = _moveGenerator.GenerateValidMoves(
                    solution, assignment)
                    .OfType<TimeMove>()
                    .ToList();

                foreach (var move in availableTimeSlots)
                {
                    // 创建解决方案选项
                    var option = new ConflictResolutionOption
                    {
                        Id = options.Count + 1,
                        ConflictId = conflict.Id,
                        Description = $"将课程 {assignment.SectionCode} 移动到其他时间段",
                        Compatibility = 80, // 较高兼容性
                        Impacts = new List<string>
                        {
                            "改变课程时间",
                            "可能影响学生上课计划"
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

                    // 限制选项数量
                    if (options.Count >= 5)
                        break;
                }

                if (options.Count >= 5)
                    break;

                // 2. 生成教师移动
                var availableTeachers = _moveGenerator.GenerateValidMoves(
                    solution, assignment)
                    .OfType<TeacherMove>()
                    .ToList();

                foreach (var move in availableTeachers)
                {
                    var teacherMove = (TeacherMove)move;

                    // 获取新教师信息
                    var newTeacher = solution.Problem?.Teachers
                        .FirstOrDefault(t => t.Id == teacherMove.NewTeacherId);

                    if (newTeacher == null)
                        continue;

                    // 创建解决方案选项
                    var option = new ConflictResolutionOption
                    {
                        Id = options.Count + 1,
                        ConflictId = conflict.Id,
                        Description = $"将课程 {assignment.SectionCode} 分配给教师 {newTeacher.Name}",
                        Compatibility = 70, // 中等兼容性
                        Impacts = new List<string>
                        {
                            "改变授课教师",
                            "可能影响教学质量"
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

                    // 限制选项数量
                    if (options.Count >= 8)
                        break;
                }

                if (options.Count >= 8)
                    break;
            }

            // 3. 生成课程交换移动
            if (assignments.Count >= 2)
            {
                var assignment1 = assignments[0];
                var assignment2 = assignments[1];

                // 创建时间交换选项
                var swapOption = new ConflictResolutionOption
                {
                    Id = options.Count + 1,
                    ConflictId = conflict.Id,
                    Description = $"交换课程 {assignment1.SectionCode} 和 {assignment2.SectionCode} 的时间",
                    Compatibility = 90, // 很高兼容性
                    Impacts = new List<string>
                    {
                        "保持教师分配不变",
                        "仅改变课程时间顺序"
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

            // 按优先级排序冲突（严重程度、影响课程数等）
            var sortedConflicts = conflicts
                .OrderByDescending(c => c.Severity)
                .ThenByDescending(c => c.InvolvedEntities?.GetValueOrDefault("Sections")?.Count ?? 0)
                .ToList();

            foreach (var conflict in sortedConflicts)
            {
                // 生成解决选项
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

                // 加上选项兼容性的权重
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

    // 添加交换时间操作
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
                // 交换时间槽
                int tempTimeSlotId = assignment1.TimeSlotId;
                assignment1.TimeSlotId = assignment2.TimeSlotId;
                assignment2.TimeSlotId = tempTimeSlotId;

                // 交换日期和时间信息
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