using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS
{
    /// <summary>
    /// Intelligent move selector to choose the appropriate move action for different types of constraint conflicts
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
        /// Select appropriate move actions based on constraint conflict types
        /// </summary>
        public List<IMove> SelectMovesForConflict(
            SchedulingSolution solution,
            SchedulingConflict conflict,
            int maxMoves = 5)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            if (conflict == null) throw new ArgumentNullException(nameof(conflict));

            _logger.LogDebug($"Selecting moves for conflict type {conflict.Type}");

            // Extract involved entity IDs
            var sectionIds = GetInvolvedIds(conflict, "Sections");
            var teacherIds = GetInvolvedIds(conflict, "Teachers");
            var classroomIds = GetInvolvedIds(conflict, "Classrooms");
            var timeSlotIds = conflict.InvolvedTimeSlots ?? new List<int>();

            // Find assignments related to conflict
            var relevantAssignments = FindRelevantAssignments(
                solution, sectionIds, teacherIds, classroomIds, timeSlotIds);

            if (relevantAssignments.Count == 0)
            {
                _logger.LogWarning($"No assignments found related to conflict {conflict.Type}");
                return new List<IMove>();
            }

            // Generate specific moves based on conflict type
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
                    // For other types of conflicts, use the generic method to generate the move
                    moves.AddRange(GenerateGenericMoves(solution, relevantAssignments));
                    break;
            }

            // If the generated moves are too many, randomly select some moves
            if (moves.Count > maxMoves)
            {
                moves = moves.OrderBy(x => _random.Next()).Take(maxMoves).ToList();
            }

            _logger.LogDebug($"Generated {moves.Count} moves for conflict type {conflict.Type}.");
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

            // For teacher conflicts, prioritize:
            // 1. Time move (move one course to other time)
            // 2. Teacher swap (swap teachers between courses)

            // Get all assignment pairs in conflict
            for (int i = 0; i < assignments.Count - 1; i++)
            {
                for (int j = i + 1; j < assignments.Count; j++)
                {
                    var a1 = assignments[i];
                    var a2 = assignments[j];

                    // If two assignments use the same teacher and same time, find the conflict pair
                    if (a1.TeacherId == a2.TeacherId && a1.TimeSlotId == a2.TimeSlotId)
                    {
                        // Generate time moves for each assignment
                        var timeMoves1 = _moveGenerator.GenerateValidMoves(solution, a1, 3)
                            .Where(m => m is TimeMove).ToList();
                        var timeMoves2 = _moveGenerator.GenerateValidMoves(solution, a2, 3)
                            .Where(m => m is TimeMove).ToList();

                        moves.AddRange(timeMoves1);
                        moves.AddRange(timeMoves2);

                        // Generate a move for swapping teachers between the two assignments
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

            // For classroom conflicts, prioritize:
            // 1. Room move (move one course to another classroom)
            // 2. Time move (move one course to another time)

            // Get all assignment pairs in conflict
            for (int i = 0; i < assignments.Count - 1; i++)
            {
                for (int j = i + 1; j < assignments.Count; j++)
                {
                    var a1 = assignments[i];
                    var a2 = assignments[j];

                    // If two assignments use the same classroom and same time, find the conflict pair
                    if (a1.ClassroomId == a2.ClassroomId && a1.TimeSlotId == a2.TimeSlotId)
                    {
                        // Generate room moves for each assignment
                        var roomMoves1 = _moveGenerator.GenerateValidMoves(solution, a1, 3)
                            .Where(m => m is RoomMove).ToList();
                        var roomMoves2 = _moveGenerator.GenerateValidMoves(solution, a2, 3)
                            .Where(m => m is RoomMove).ToList();

                        moves.AddRange(roomMoves1);
                        moves.AddRange(roomMoves2);

                        // If not enough room moves found, add some time moves
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

            // For classroom capacity conflicts, mainly consider changing classroom
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

            // For teacher availability conflicts, mainly consider changing time or teacher
            foreach (var assignment in assignments)
            {
                // First try changing time
                var timeMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 3)
                    .Where(m => m is TimeMove).ToList();
                moves.AddRange(timeMoves);

                // If not enough time moves found, try changing teacher
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

            // For travel time conflicts, consider:
            // 1. Changing time (increase interval between two classes)
            // 2. Changing classroom (choose closer classroom)
            // 3. Swapping courses (make courses in same area adjacent)

            foreach (var assignment in assignments)
            {
                // Time moves
                var timeMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 3)
                    .Where(m => m is TimeMove).ToList();
                moves.AddRange(timeMoves);

                // Room moves
                var roomMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 3)
                    .Where(m => m is RoomMove).ToList();
                moves.AddRange(roomMoves);

                // Add some swap moves
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

            // For generic conflicts, generate various types of moves
            foreach (var assignment in assignments)
            {
                var genericMoves = _moveGenerator.GenerateValidMoves(solution, assignment, 5);
                moves.AddRange(genericMoves);
            }

            return moves;
        }
    }
}