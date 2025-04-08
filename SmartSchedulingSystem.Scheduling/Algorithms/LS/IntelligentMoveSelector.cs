using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS
{
    /// <summary>
    /// 智能移动选择器，为不同类型的约束冲突选择合适的移动操作
    /// </summary>
    public class IntelligentMoveSelector
    {
        private readonly ILogger<IntelligentMoveSelector> _logger;
        private readonly MoveGenerator _moveGenerator;
        private readonly Random _random = new Random();

        public IntelligentMoveSelector(
            ILogger<IntelligentMoveSelector> logger,
            MoveGenerator moveGenerator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _moveGenerator = moveGenerator ?? throw new ArgumentNullException(nameof(moveGenerator));
        }

        /// <summary>
        /// 根据约束冲突类型选择合适的移动操作
        /// </summary>
        public List<IMove> SelectMovesForConflict(
            SchedulingSolution solution,
            SchedulingConflict conflict,
            int maxMoves = 5)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            if (conflict == null) throw new ArgumentNullException(nameof(conflict));

            _logger.LogDebug($"为冲突类型 {conflict.Type} 选择移动操作");

            // 提取涉及的实体ID
            var sectionIds = GetInvolvedIds(conflict, "Sections");
            var teacherIds = GetInvolvedIds(conflict, "Teachers");
            var classroomIds = GetInvolvedIds(conflict, "Classrooms");
            var timeSlotIds = conflict.InvolvedTimeSlots ?? new List<int>();

            // 找出与冲突相关的分配
            var relevantAssignments = FindRelevantAssignments(
                solution, sectionIds, teacherIds, classroomIds, timeSlotIds);

            if (relevantAssignments.Count == 0)
            {
                _logger.LogWarning($"未找到与冲突 {conflict.Type} 相关的分配");
                return new List<IMove>();
            }

            // 根据冲突类型生成特定的移动
            var moves = new List<IMove>();

            switch (conflict.Type)
            {
                case SchedulingConflictType.TeacherConflict:
                    moves.AddRange(GenerateMovesForTeacherConflict(solution, relevantAssignments));
                    break;

                case SchedulingConflictType.ClassroomConflict:
                    moves.AddRange(GenerateMovesForClassroomConflict(solution, relevantAssignments));
                    break;

                case SchedulingConflictType.ClassroomCapacityExceeded:
                    moves.AddRange(GenerateMovesForCapacityConflict(solution, relevantAssignments));
                    break;

                case SchedulingConflictType.TeacherAvailabilityConflict:
                    moves.AddRange(GenerateMovesForTeacherAvailabilityConflict(solution, relevantAssignments));
                    break;

                case SchedulingConflictType.CampusTravelTimeConflict:
                    moves.AddRange(GenerateMovesForTravelTimeConflict(solution, relevantAssignments));
                    break;

                default:
                    // 对于其他类型的冲突，使用通用方法生成移动
                    moves.AddRange(GenerateGenericMoves(solution, relevantAssignments));
                    break;
            }

            // 如果生成的移动太多，随机选择部分移动
            if (moves.Count > maxMoves)
            {
                moves = moves.OrderBy(x => _random.Next()).Take(maxMoves).ToList();
            }

            _logger.LogDebug($"为冲突类型 {conflict.Type} 生成了 {moves.Count} 个移动操作");
            return moves;
        }

        private List<int> GetInvolvedIds(SchedulingConflict conflict, string entityType)
        {
            if (conflict.InvolvedEntities != null &&
                conflict.InvolvedEntities.TryGetValue(entityType, out var ids))
            {
                return ids;
            }
            return new List<int>();
        }

        private List<SchedulingAssignment> FindRelevantAssignments(
            SchedulingSolution solution,
            List<int> sectionIds,
            List<int> teacherIds,
            List<int> classroomIds,
            List<int> timeSlotIds)
        {
            return solution.Assignments.Where(a =>
                (sectionIds.Count == 0 || sectionIds.Contains(a.SectionId)) ||
                (teacherIds.Count == 0 || teacherIds.Contains(a.TeacherId)) ||
                (classroomIds.Count == 0 || classroomIds.Contains(a.ClassroomId)) ||
                (timeSlotIds.Count == 0 || timeSlotIds.Contains(a.TimeSlotId))
            ).ToList();
        }

        private List<IMove> GenerateMovesForTeacherConflict(
            SchedulingSolution solution,
            List<SchedulingAssignment> assignments)
        {
            var moves = new List<IMove>();

            // 对于教师冲突，优先考虑：
            // 1. 时间移动（把其中一门课移到其他时间）
            // 2. 教师交换（在课程间交换教师）

            // 获取冲突中的所有分配对
            for (int i = 0; i < assignments.Count - 1; i++)
            {
                for (int j = i + 1; j < assignments.Count; j++)
                {
                    var a1 = assignments[i];
                    var a2 = assignments[j];

                    // 如果两个分配使用同一教师和同一时间，则找到冲突对
                    if (a1.TeacherId == a2.TeacherId && a1.TimeSlotId == a2.TimeSlotId)
                    {
                        // 为每个分配生成时间移动
                        var timeMoves1 = _moveGenerator.GenerateValidMoves(solution, a1, 3)
                            .Where(m => m is TimeMove).ToList();
                        var timeMoves2 = _moveGenerator.GenerateValidMoves(solution, a2, 3)
                            .Where(m => m is TimeMove).ToList();

                        moves.AddRange(timeMoves1);
                        moves.AddRange(timeMoves2);

                        // 为互相交换教师生成一个移动
                        var potentialSwapPartners = solution.Assignments
                            .Where(a => a.TimeSlotId != a1.TimeSlotId && a.Id != a1.Id && a.Id != a2.Id)
                            .Take(3);

                        foreach (var partner in potentialSwapPartners)
                        {
                            moves.Add(new TeacherMove(a1.Id, partner.TeacherId));
                            moves.Add(new TeacherMove(a2.Id, partner.TeacherId));
                        }
                    }
                }
            }

            return moves;
        }

        private List<IMove> GenerateMovesForClassroomConflict(
            SchedulingSolution solution,
            List<SchedulingAssignment> assignments)
        {
            var moves = new List<IMove>();

            // 对于教室冲突，优先考虑：
            // 1. 教室移动（把其中一门课移到其他教室）
            // 2. 时间移动（把其中一门课移到其他时间）

            // 获取冲突中的所有分配对
            for (int i = 0; i < assignments.Count - 1; i++)
            {
                for (int j = i + 1; j < assignments.Count; j++)
                {
                    var a1 = assignments[i];
                    var a2 = assignments[j];

                    // 如果两个分配使用同一教室和同一时间，则找到冲突对
                    if (a1.ClassroomId == a2.ClassroomId && a1.TimeSlotId == a2.TimeSlotId)
                    {
                        // 为每个分配生成教室移动
                        var roomMoves1 = _moveGenerator.GenerateValidMoves(solution, a1, 3)
                            .Where(m => m is RoomMove).ToList();
                        var roomMoves2 = _moveGenerator.GenerateValidMoves(solution, a2, 3)
                            .Where(m => m is RoomMove).ToList();

                        moves.AddRange(roomMoves1);
                        moves.AddRange(roomMoves2);

                        // 如果找不到足够的教室移动，添加一些时间移动
                        if (roomMoves1.Count + roomMoves2.Count < 4)
                        {
                            var timeMoves1 = _moveGenerator.GenerateValidMoves(solution, a1, 2)
                                .Where(m => m is TimeMove).ToList();
                            var timeMoves2 = _moveGenerator.GenerateValidMoves(solution, a2, 2)
                                .Where(m => m is TimeMove).ToList();

                            moves.AddRange(timeMoves1);
                            moves.AddRange(timeMoves2);
                        }
                    }
                }
            }

            return moves;
        }

        private List<IMove> GenerateMovesForCapacityConflict(
            SchedulingSolution solution,
            List<SchedulingAssignment> assignments)
        {
            var moves = new List<IMove>();

            // 对于教室容量冲突，主要考虑更换教室
            foreach (var assignment in assignments)
            {
                var roomMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 5)
                    .Where(m => m is RoomMove).ToList();
                moves.AddRange(roomMoves);
            }

            return moves;
        }

        private List<IMove> GenerateMovesForTeacherAvailabilityConflict(
            SchedulingSolution solution,
            List<SchedulingAssignment> assignments)
        {
            var moves = new List<IMove>();

            // 对于教师可用性冲突，主要考虑更换时间或教师
            foreach (var assignment in assignments)
            {
                // 首先尝试更换时间
                var timeMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 3)
                    .Where(m => m is TimeMove).ToList();
                moves.AddRange(timeMoves);

                // 如果找不到足够的时间移动，尝试更换教师
                if (timeMoves.Count < 2)
                {
                    var teacherMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 3)
                        .Where(m => m is TeacherMove).ToList();
                    moves.AddRange(teacherMoves);
                }
            }

            return moves;
        }

        private List<IMove> GenerateMovesForTravelTimeConflict(
            SchedulingSolution solution,
            List<SchedulingAssignment> assignments)
        {
            var moves = new List<IMove>();

            // 对于旅行时间冲突，可以考虑：
            // 1. 更换时间（增加两节课之间的间隔）
            // 2. 更换教室（选择更接近的教室）
            // 3. 交换课程（使同一区域的课程相邻）

            foreach (var assignment in assignments)
            {
                // 时间移动
                var timeMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 3)
                    .Where(m => m is TimeMove).ToList();
                moves.AddRange(timeMoves);

                // 教室移动
                var roomMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 3)
                    .Where(m => m is RoomMove).ToList();
                moves.AddRange(roomMoves);

                // 添加一些交换移动
                var swapMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 2)
                    .Where(m => m is SwapMove).ToList();
                moves.AddRange(swapMoves);
            }

            return moves;
        }

        // 由于GenderRestrictionConstraint不需要实现，此方法移除
        private List<IMove> GenerateGenericMovesForUnknownConflict(
            SchedulingSolution solution,
            List<SchedulingAssignment> assignments)
        {
            var moves = new List<IMove>();

            // 对于未知类型的冲突，尝试各种类型的移动
            foreach (var assignment in assignments)
            {
                // 添加时间移动
                var timeMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 2)
                    .Where(m => m is TimeMove).ToList();
                moves.AddRange(timeMoves);

                // 添加教室移动
                var roomMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 2)
                    .Where(m => m is RoomMove).ToList();
                moves.AddRange(roomMoves);

                // 添加教师移动
                var teacherMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 1)
                    .Where(m => m is TeacherMove).ToList();
                moves.AddRange(teacherMoves);
            }

            return moves;
        }

        private List<IMove> GenerateGenericMoves(
            SchedulingSolution solution,
            List<SchedulingAssignment> assignments)
        {
            var moves = new List<IMove>();

            // 对于泛用的冲突，生成各种类型的移动
            foreach (var assignment in assignments)
            {
                var genericMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 5);
                moves.AddRange(genericMoves);
            }

            return moves;
        }
    }
}