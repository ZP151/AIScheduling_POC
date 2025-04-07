using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Engine.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Engine.LS
{
    /// <summary>
    /// 负责在局部搜索阶段生成有效的优化移动
    /// </summary>
    public class MoveGenerator
    {
        private readonly Random _random = new Random();
        private readonly ILogger<MoveGenerator> _logger;
        private readonly ConstraintManager _constraintManager;
        private readonly SchedulingParameters _parameters;

        public MoveGenerator(
            ILogger<MoveGenerator> logger,
            ConstraintManager constraintManager,
            SchedulingParameters parameters = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _parameters = parameters ?? new SchedulingParameters();
        }

        /// <summary>
        /// 生成有效的移动操作
        /// </summary>
        /// <param name="solution">当前解决方案</param>
        /// <param name="assignment">要优化的课程分配</param>
        /// <param name="maxMoves">最多生成的移动数量</param>
        /// <returns>有效移动列表</returns>
        public List<IMove> GenerateValidMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            int maxMoves = 10)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));

            try
            {
                _logger.LogDebug($"为分配 #{assignment.Id} 生成移动操作，最大数量: {maxMoves}");

                var validMoves = new List<IMove>();

                // 添加时间移动
                AddTimeSlotMoves(solution, assignment, validMoves);

                // 添加教室移动
                AddRoomMoves(solution, assignment, validMoves);

                // 添加教师移动
                AddTeacherMoves(solution, assignment, validMoves);

                // 添加交换移动
                AddSwapMoves(solution, assignment, validMoves);

                _logger.LogDebug($"生成了 {validMoves.Count} 个有效移动操作");

                // 如果生成的移动太多，随机选择maxMoves个
                if (validMoves.Count > maxMoves)
                {
                    validMoves = validMoves
                        .OrderBy(x => _random.Next())
                        .Take(maxMoves)
                        .ToList();

                    _logger.LogDebug($"随机选择了 {maxMoves} 个移动操作");
                }

                return validMoves;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"生成移动操作时出错: {ex.Message}");
                return new List<IMove>();
            }
        }
        // 在MoveGenerator.cs中添加针对特定约束类型的移动生成方法
        public List<IMove> GenerateMovesForConstraintType(
            SchedulingSolution solution,
            SchedulingConflict conflict,
            SchedulingAssignment assignment)
        {
            var moves = new List<IMove>();

            switch (conflict.Type)
            {
                case SchedulingConflictType.TeacherConflict:
                    moves.AddRange(GenerateMovesForTeacherConflict(solution, assignment));
                    break;
                case SchedulingConflictType.ClassroomConflict:
                    moves.AddRange(GenerateMovesForClassroomConflict(solution, assignment));
                    break;
                case SchedulingConflictType.ClassroomCapacityExceeded:
                    moves.AddRange(GenerateMovesForCapacityConflict(solution, assignment));
                    break;
                case SchedulingConflictType.TeacherAvailabilityConflict:
                    moves.AddRange(GenerateMovesForTeacherAvailabilityConflict(solution, assignment));
                    break;
                case SchedulingConflictType.CampusTravelTimeConflict:
                    moves.AddRange(GenerateMovesForTravelTimeConflict(solution, assignment));
                    break;
                case SchedulingConflictType.PrerequisiteConflict:
                    moves.AddRange(GenerateMovesForPrerequisiteConflict(solution, assignment));
                    break;
                default:
                    // 对于其他类型的冲突，生成通用移动
                    moves.AddRange(GenerateGenericMoves(solution, assignment));
                    break;
            }

            return moves;
        }

        private List<IMove> GenerateMovesForTeacherConflict(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var moves = new List<IMove>();

            // 1. 尝试移动到其他时间槽
            var availableTimeSlots = GetAvailableTimeSlots(solution, assignment);
            foreach (var timeSlot in availableTimeSlots)
            {
                moves.Add(new TimeMove(assignment.Id, timeSlot));
            }

            // 2. 尝试更换教师
            var qualifiedTeachers = GetQualifiedTeachers(solution, assignment);
            foreach (var teacher in qualifiedTeachers)
            {
                moves.Add(new TeacherMove(assignment.Id, teacher));
            }

            return moves;
        }

        // 实现其他冲突类型的移动生成方法...
        private List<IMove> GenerateMovesForClassroomConflict(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var moves = new List<IMove>();

            // 1. 尝试更换教室
            var suitableRooms = GetSuitableRooms(solution, assignment);
            foreach (var roomId in suitableRooms)
            {
                moves.Add(new RoomMove(assignment.Id, roomId));
            }

            // 2. 尝试更换时间槽
            var availableTimeSlots = GetAvailableTimeSlots(solution, assignment);
            foreach (var timeSlotId in availableTimeSlots)
            {
                moves.Add(new TimeMove(assignment.Id, timeSlotId));
            }

            return moves;
        }

        private List<IMove> GenerateMovesForCapacityConflict(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var moves = new List<IMove>();

            // 查找容量更大的教室
            var largerRooms = GetLargerRooms(solution, assignment);
            foreach (var roomId in largerRooms)
            {
                moves.Add(new RoomMove(assignment.Id, roomId));
            }

            return moves;
        }

        private List<IMove> GenerateMovesForTeacherAvailabilityConflict(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var moves = new List<IMove>();

            // 1. 尝试移动到教师可用的时间槽
            var teacherAvailableTimeSlots = GetTeacherAvailableTimeSlots(solution, assignment);
            foreach (var timeSlotId in teacherAvailableTimeSlots)
            {
                moves.Add(new TimeMove(assignment.Id, timeSlotId));
            }

            // 2. 尝试更换为可用的教师
            var availableTeachers = GetAvailableTeachers(solution, assignment);
            foreach (var teacherId in availableTeachers)
            {
                moves.Add(new TeacherMove(assignment.Id, teacherId));
            }

            return moves;
        }

        private List<IMove> GenerateMovesForTravelTimeConflict(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var moves = new List<IMove>();

            // 1. 尝试移动到更合适的时间槽(增加旅行时间)
            var betterTimeSlots = GetTimeSlotWithSufficientTravelTime(solution, assignment);
            foreach (var timeSlotId in betterTimeSlots)
            {
                moves.Add(new TimeMove(assignment.Id, timeSlotId));
            }

            // 2. 尝试更换更近的教室
            var nearbyRooms = GetNearbyRooms(solution, assignment);
            foreach (var roomId in nearbyRooms)
            {
                moves.Add(new RoomMove(assignment.Id, roomId));
            }

            return moves;
        }

        private List<IMove> GenerateMovesForPrerequisiteConflict(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var moves = new List<IMove>();

            // 对于先修课程冲突，主要是调整时间槽
            var nonConflictingTimeSlots = GetNonPrerequisiteConflictingTimeSlots(solution, assignment);
            foreach (var timeSlotId in nonConflictingTimeSlots)
            {
                moves.Add(new TimeMove(assignment.Id, timeSlotId));
            }

            return moves;
        }
        private List<IMove> GenerateGenericMoves(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var moves = new List<IMove>();

            // 1. 时间移动
            var availableTimeSlots = GetAvailableTimeSlots(solution, assignment);
            foreach (var timeSlotId in availableTimeSlots.Take(3)) // 限制生成的移动数量
            {
                moves.Add(new TimeMove(assignment.Id, timeSlotId));
            }

            // 2. 教室移动
            var suitableRooms = GetSuitableRooms(solution, assignment);
            foreach (var roomId in suitableRooms.Take(3))
            {
                moves.Add(new RoomMove(assignment.Id, roomId));
            }

            // 3. 教师移动
            var qualifiedTeachers = GetQualifiedTeachers(solution, assignment);
            foreach (var teacherId in qualifiedTeachers.Take(2))
            {
                moves.Add(new TeacherMove(assignment.Id, teacherId));
            }

            // 4. 随机选择一些课程进行交换
            var swapCandidates = solution.Assignments
                .Where(a => a.Id != assignment.Id)
                .OrderBy(x => Guid.NewGuid())
                .Take(2);

            foreach (var candidate in swapCandidates)
            {
                moves.Add(new SwapMove(assignment.Id, candidate.Id, true, false, false)); // 交换时间
                moves.Add(new SwapMove(assignment.Id, candidate.Id, false, true, false)); // 交换教室
            }

            return moves;
        }

        /// <summary>
        /// 添加时间移动
        /// </summary>
        private void AddTimeSlotMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // 获取所有可用的时间槽
            var availableTimeSlots = GetAvailableTimeSlots(solution, assignment);

            _logger.LogDebug($"找到 {availableTimeSlots.Count} 个可用时间槽供移动");

            foreach (var timeSlotId in availableTimeSlots)
            {
                var move = new TimeMove(assignment.Id, timeSlotId);

                // 验证移动是否满足所有硬约束
                if (IsValidMove(solution, move))
                {
                    moves.Add(move);
                }
            }
        }
        // 在MoveGenerator.cs中添加必要的辅助方法
        private List<int> GetLargerRooms(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var largerRooms = new List<int>();

            if (solution.Problem == null)
                return largerRooms;

            var courseSection = solution.Problem.CourseSections
                .FirstOrDefault(s => s.Id == assignment.SectionId);

            if (courseSection == null)
                return largerRooms;

            var currentRoom = solution.Problem.Classrooms
                .FirstOrDefault(r => r.Id == assignment.ClassroomId);

            if (currentRoom == null)
                return largerRooms;

            foreach (var room in solution.Problem.Classrooms)
            {
                // 排除当前教室
                if (room.Id == assignment.ClassroomId)
                    continue;

                // 检查容量是否更大且足够
                if (room.Capacity <= currentRoom.Capacity || room.Capacity < courseSection.Enrollment)
                    continue;

                // 检查教室在此时间段是否已有其他课程
                if (solution.HasClassroomConflict(room.Id, assignment.TimeSlotId, assignment.SectionId))
                    continue;

                // 检查教室在此时间段是否可用
                var roomAvailability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ra => ra.ClassroomId == room.Id && ra.TimeSlotId == assignment.TimeSlotId);

                if (roomAvailability != null && !roomAvailability.IsAvailable)
                    continue;

                // 教室合适
                largerRooms.Add(room.Id);
            }

            return largerRooms;
        }

        private List<int> GetTeacherAvailableTimeSlots(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var availableSlots = new List<int>();

            if (solution.Problem == null)
                return availableSlots;

            foreach (var timeSlot in solution.Problem.TimeSlots)
            {
                // 排除当前时间槽
                if (timeSlot.Id == assignment.TimeSlotId)
                    continue;

                // 检查教师在此时间段是否可用
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId && ta.TimeSlotId == timeSlot.Id);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // 检查教师在此时间段是否已有其他课程
                if (solution.HasTeacherConflict(assignment.TeacherId, timeSlot.Id, assignment.SectionId))
                    continue;

                // 检查教室在此时间段是否已有其他课程
                if (solution.HasClassroomConflict(assignment.ClassroomId, timeSlot.Id, assignment.SectionId))
                    continue;

                // 时间槽可用
                availableSlots.Add(timeSlot.Id);
            }

            return availableSlots;
        }

        private List<int> GetAvailableTeachers(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var availableTeachers = new List<int>();

            if (solution.Problem == null)
                return availableTeachers;

            var courseSection = solution.Problem.CourseSections
                .FirstOrDefault(s => s.Id == assignment.SectionId);

            if (courseSection == null)
                return availableTeachers;

            foreach (var teacher in solution.Problem.Teachers)
            {
                // 排除当前教师
                if (teacher.Id == assignment.TeacherId)
                    continue;

                // 检查教师是否有资格教授此课程
                var preference = solution.Problem.TeacherCoursePreferences
                    .FirstOrDefault(p => p.TeacherId == teacher.Id &&
                                       p.CourseId == courseSection.CourseId);

                if (preference == null || preference.ProficiencyLevel < 3)
                    continue;

                // 检查教师在此时间段是否已有其他课程
                if (solution.HasTeacherConflict(teacher.Id, assignment.TimeSlotId, assignment.SectionId))
                    continue;

                // 检查教师在此时间段是否可用
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == teacher.Id && ta.TimeSlotId == assignment.TimeSlotId);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // 教师可用
                availableTeachers.Add(teacher.Id);
            }

            return availableTeachers;
        }

        private List<int> GetTimeSlotWithSufficientTravelTime(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var suitableTimeSlots = new List<int>();

            if (solution.Problem == null)
                return suitableTimeSlots;

            // 获取当前教师的所有其他分配
            var teacherAssignments = solution.Assignments
                .Where(a => a.TeacherId == assignment.TeacherId && a.Id != assignment.Id)
                .ToList();

            // 如果教师没有其他课程，任何时间槽都可以
            if (!teacherAssignments.Any())
            {
                return GetAvailableTimeSlots(solution, assignment);
            }

            // 获取当前教室的校区信息
            var currentClassroom = solution.Problem.Classrooms
                .FirstOrDefault(c => c.Id == assignment.ClassroomId);

            if (currentClassroom == null)
                return suitableTimeSlots;

            int currentCampusId = currentClassroom.CampusId;

            // 检查每个时间槽
            foreach (var timeSlot in solution.Problem.TimeSlots)
            {
                // 排除当前时间槽
                if (timeSlot.Id == assignment.TimeSlotId)
                    continue;

                // 检查教师在此时间段是否可用
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId && ta.TimeSlotId == timeSlot.Id);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // 检查教室在此时间段是否可用
                var roomAvailability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ra => ra.ClassroomId == assignment.ClassroomId && ra.TimeSlotId == timeSlot.Id);

                if (roomAvailability != null && !roomAvailability.IsAvailable)
                    continue;

                // 检查教师在此时间段是否已有其他课程
                if (solution.HasTeacherConflict(assignment.TeacherId, timeSlot.Id, assignment.SectionId))
                    continue;

                // 检查教室在此时间段是否已有其他课程
                if (solution.HasClassroomConflict(assignment.ClassroomId, timeSlot.Id, assignment.SectionId))
                    continue;

                // 检查与教师其他课程的旅行时间是否足够
                bool hasSufficientTravelTime = true;

                foreach (var otherAssignment in teacherAssignments)
                {
                    var otherTimeSlot = solution.Problem.TimeSlots
                        .FirstOrDefault(t => t.Id == otherAssignment.TimeSlotId);

                    if (otherTimeSlot == null)
                        continue;

                    // 如果是同一天
                    if (timeSlot.DayOfWeek == otherTimeSlot.DayOfWeek)
                    {
                        // 计算时间差(分钟)
                        double timeDifference;
                        if (timeSlot.StartTime > otherTimeSlot.EndTime)
                        {
                            timeDifference = (timeSlot.StartTime - otherTimeSlot.EndTime).TotalMinutes;
                        }
                        else if (otherTimeSlot.StartTime > timeSlot.EndTime)
                        {
                            timeDifference = (otherTimeSlot.StartTime - timeSlot.EndTime).TotalMinutes;
                        }
                        else
                        {
                            // 时间重叠
                            hasSufficientTravelTime = false;
                            break;
                        }

                        // 获取其他分配的教室的校区信息
                        var otherClassroom = solution.Problem.Classrooms
                            .FirstOrDefault(c => c.Id == otherAssignment.ClassroomId);

                        if (otherClassroom == null)
                            continue;

                        int otherCampusId = otherClassroom.CampusId;

                        // 如果在不同校区，需要更多旅行时间
                        if (currentCampusId != otherCampusId)
                        {
                            // 获取校区间旅行时间(假设至少需要30分钟)
                            int travelTime = 30;

                            if (timeDifference < travelTime)
                            {
                                hasSufficientTravelTime = false;
                                break;
                            }
                        }
                        else if (timeDifference < 15) // 同一校区至少需要15分钟
                        {
                            hasSufficientTravelTime = false;
                            break;
                        }
                    }
                }

                if (hasSufficientTravelTime)
                {
                    suitableTimeSlots.Add(timeSlot.Id);
                }
            }

            return suitableTimeSlots;
        }
        // 在MoveGenerator.cs中添加必要的辅助方法（续）
        private List<int> GetNearbyRooms(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var nearbyRooms = new List<int>();

            if (solution.Problem == null)
                return nearbyRooms;

            // 获取当前教室信息
            var currentClassroom = solution.Problem.Classrooms
                .FirstOrDefault(c => c.Id == assignment.ClassroomId);

            if (currentClassroom == null)
                return nearbyRooms;

            int currentCampusId = currentClassroom.CampusId;
            string currentBuilding = currentClassroom.Building;

            // 获取当前教师的所有其他分配
            var teacherAssignments = solution.Assignments
                .Where(a => a.TeacherId == assignment.TeacherId && a.Id != assignment.Id)
                .ToList();

            // 检查每个教室
            foreach (var room in solution.Problem.Classrooms)
            {
                // 排除当前教室
                if (room.Id == assignment.ClassroomId)
                    continue;

                // 检查容量是否足够
                var courseSection = solution.Problem.CourseSections
                    .FirstOrDefault(s => s.Id == assignment.SectionId);

                if (courseSection != null && room.Capacity < courseSection.Enrollment)
                    continue;

                // 检查教室在此时间段是否已有其他课程
                if (solution.HasClassroomConflict(room.Id, assignment.TimeSlotId, assignment.SectionId))
                    continue;

                // 检查教室在此时间段是否可用
                var roomAvailability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ra => ra.ClassroomId == room.Id && ra.TimeSlotId == assignment.TimeSlotId);

                if (roomAvailability != null && !roomAvailability.IsAvailable)
                    continue;

                // 优先选择同一建筑物的教室
                if (room.Building == currentBuilding)
                {
                    nearbyRooms.Add(room.Id);
                    continue;
                }

                // 如果没有其他分配，同一校区的教室也可以
                if (!teacherAssignments.Any() && room.CampusId == currentCampusId)
                {
                    nearbyRooms.Add(room.Id);
                    continue;
                }

                // 对于有其他分配的教师，需要考虑与其他课程的距离
                bool isSuitable = true;
                foreach (var otherAssignment in teacherAssignments)
                {
                    var otherTimeSlot = solution.Problem.TimeSlots
                        .FirstOrDefault(t => t.Id == otherAssignment.TimeSlotId);

                    if (otherTimeSlot == null)
                        continue;

                    // 如果是同一天
                    if (assignment.DayOfWeek == otherTimeSlot.DayOfWeek)
                    {
                        // 计算时间差(分钟)
                        double timeDifference;
                        if (assignment.StartTime > otherTimeSlot.EndTime)
                        {
                            timeDifference = (assignment.StartTime - otherTimeSlot.EndTime).TotalMinutes;
                        }
                        else if (otherTimeSlot.StartTime > assignment.EndTime)
                        {
                            timeDifference = (otherTimeSlot.StartTime - assignment.EndTime).TotalMinutes;
                        }
                        else
                        {
                            // 时间重叠，不考虑
                            continue;
                        }

                        // 获取其他分配的教室
                        var otherClassroom = solution.Problem.Classrooms
                            .FirstOrDefault(c => c.Id == otherAssignment.ClassroomId);

                        if (otherClassroom == null)
                            continue;

                        // 如果时间差足够大，或者教室在同一校区/建筑物，则适合
                        if (timeDifference >= 30 ||
                            (room.CampusId == otherClassroom.CampusId && timeDifference >= 15) ||
                            (room.Building == otherClassroom.Building && timeDifference >= 10))
                        {
                            // 适合
                        }
                        else
                        {
                            isSuitable = false;
                            break;
                        }
                    }
                }

                if (isSuitable)
                {
                    nearbyRooms.Add(room.Id);
                }
            }

            return nearbyRooms;
        }

        private List<int> GetNonPrerequisiteConflictingTimeSlots(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var suitableTimeSlots = new List<int>();

            if (solution.Problem == null)
                return suitableTimeSlots;

            // 获取当前课程
            var courseSection = solution.Problem.CourseSections
                .FirstOrDefault(s => s.Id == assignment.SectionId);

            if (courseSection == null)
                return suitableTimeSlots;

            int courseId = courseSection.CourseId;

            // 获取与当前课程有先修关系的课程
            var prerequisites = solution.Problem.Prerequisites
                .Where(p => p.CourseId == courseId || p.PrerequisiteCourseId == courseId)
                .ToList();

            // 如果没有先修关系，所有可用时间槽都可以
            if (!prerequisites.Any())
            {
                return GetAvailableTimeSlots(solution, assignment);
            }

            // 获取涉及先修关系的课程ID
            var relatedCourseIds = new HashSet<int>();
            foreach (var prereq in prerequisites)
            {
                if (prereq.CourseId == courseId)
                {
                    relatedCourseIds.Add(prereq.PrerequisiteCourseId); // 当前课程的先修课程
                }
                else if (prereq.PrerequisiteCourseId == courseId)
                {
                    relatedCourseIds.Add(prereq.CourseId); // 以当前课程为先修的课程
                }
            }

            // 获取这些课程的所有班级
            var relatedSectionIds = solution.Problem.CourseSections
                .Where(s => relatedCourseIds.Contains(s.CourseId))
                .Select(s => s.Id)
                .ToList();

            // 获取这些班级的安排
            var relatedAssignments = solution.Assignments
                .Where(a => relatedSectionIds.Contains(a.SectionId))
                .ToList();

            // 检查每个时间槽
            foreach (var timeSlot in solution.Problem.TimeSlots)
            {
                // 排除当前时间槽
                if (timeSlot.Id == assignment.TimeSlotId)
                    continue;

                // 检查教师在此时间段是否可用
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId && ta.TimeSlotId == timeSlot.Id);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // 检查教室在此时间段是否可用
                var roomAvailability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ra => ra.ClassroomId == assignment.ClassroomId && ra.TimeSlotId == timeSlot.Id);

                if (roomAvailability != null && !roomAvailability.IsAvailable)
                    continue;

                // 检查教师在此时间段是否已有其他课程
                if (solution.HasTeacherConflict(assignment.TeacherId, timeSlot.Id, assignment.SectionId))
                    continue;

                // 检查教室在此时间段是否已有其他课程
                if (solution.HasClassroomConflict(assignment.ClassroomId, timeSlot.Id, assignment.SectionId))
                    continue;

                // 检查是否与有先修关系的课程在同一时间段
                bool hasPrerequisiteConflict = false;
                foreach (var relatedAssignment in relatedAssignments)
                {
                    if (relatedAssignment.TimeSlotId == timeSlot.Id)
                    {
                        hasPrerequisiteConflict = true;
                        break;
                    }
                }

                if (!hasPrerequisiteConflict)
                {
                    suitableTimeSlots.Add(timeSlot.Id);
                }
            }

            return suitableTimeSlots;
        }
        /// <summary>
        /// 添加教室移动
        /// </summary>
        private void AddRoomMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // 获取所有合适的教室
            var suitableRooms = GetSuitableRooms(solution, assignment);

            _logger.LogDebug($"找到 {suitableRooms.Count} 个合适的教室供移动");

            foreach (var roomId in suitableRooms)
            {
                var move = new RoomMove(assignment.Id, roomId);

                // 验证移动是否满足所有硬约束
                if (IsValidMove(solution, move))
                {
                    moves.Add(move);
                }
            }
        }

        /// <summary>
        /// 添加教师移动
        /// </summary>
        private void AddTeacherMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // 获取所有有资格的教师
            var qualifiedTeachers = GetQualifiedTeachers(solution, assignment);

            _logger.LogDebug($"找到 {qualifiedTeachers.Count} 个有资格的教师供移动");

            foreach (var teacherId in qualifiedTeachers)
            {
                var move = new TeacherMove(assignment.Id, teacherId);

                // 验证移动是否满足所有硬约束
                if (IsValidMove(solution, move))
                {
                    moves.Add(move);
                }
            }
        }

        /// <summary>
        /// 添加交换移动
        /// </summary>
        private void AddSwapMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // 找出可能的交换对象
            var potentialSwapPartners = FindPotentialSwapPartners(solution, assignment);

            _logger.LogDebug($"找到 {potentialSwapPartners.Count} 个潜在交换伙伴");

            foreach (var partnerId in potentialSwapPartners)
            {
                // 时间交换
                var timeSwap = new SwapMove(assignment.Id, partnerId, true, false, false);
                if (IsValidMove(solution, timeSwap))
                {
                    moves.Add(timeSwap);
                }

                // 教室交换
                var roomSwap = new SwapMove(assignment.Id, partnerId, false, true, false);
                if (IsValidMove(solution, roomSwap))
                {
                    moves.Add(roomSwap);
                }

                // 交换教师
                var teacherSwap = new SwapMove(assignment.Id, partnerId, false, false, true);
                if (IsValidMove(solution, teacherSwap))
                {
                    moves.Add(teacherSwap);
                }
                // 时间和教室都交换
                var timeRoomSwap = new SwapMove(assignment.Id, partnerId, true, true, false);
                if (IsValidMove(solution, timeRoomSwap))
                {
                    moves.Add(timeRoomSwap);
                }
                // 时间和教师交换
                var timeTeacherSwap = new SwapMove(assignment.Id, partnerId, true, false, true);
                if (IsValidMove(solution, timeTeacherSwap))
                {
                    moves.Add(timeTeacherSwap);
                }

                // 教室和教师交换
                var roomTeacherSwap = new SwapMove(assignment.Id, partnerId, false, true, true);
                if (IsValidMove(solution, roomTeacherSwap))
                {
                    moves.Add(roomTeacherSwap);
                }


                // 全部交换（时间、教室、教师）
                var completeSwap = new SwapMove(assignment.Id, partnerId, true, true, true);
                if (IsValidMove(solution, completeSwap))
                {
                    moves.Add(completeSwap);
                }
                
            }
        }

        private List<int> GetAvailableTimeSlots(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            // 实现逻辑：找出所有不与当前分配冲突的时间槽
            var availableSlots = new List<int>();

            if (solution.Problem == null)
                return availableSlots;

            foreach (var timeSlot in solution.Problem.TimeSlots)
            {
                // 排除当前时间槽
                if (timeSlot.Id == assignment.TimeSlotId)
                    continue;

                // 检查教师在此时间段是否已有其他课程
                if (solution.HasTeacherConflict(assignment.TeacherId, timeSlot.Id, assignment.SectionId))
                    continue;

                // 检查教室在此时间段是否已有其他课程
                if (solution.HasClassroomConflict(assignment.ClassroomId, timeSlot.Id, assignment.SectionId))
                    continue;

                // 检查教师在此时间段是否可用
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId && ta.TimeSlotId == timeSlot.Id);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // 时间槽可用
                availableSlots.Add(timeSlot.Id);
            }

            return availableSlots;
        }

        private List<int> GetSuitableRooms(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            // 实现逻辑：找出所有适合当前课程的教室
            var suitableRooms = new List<int>();

            if (solution.Problem == null)
                return suitableRooms;

            var courseSection = solution.Problem.CourseSections
                .FirstOrDefault(s => s.Id == assignment.SectionId);

            if (courseSection == null)
                return suitableRooms;

            foreach (var room in solution.Problem.Classrooms)
            {
                // 排除当前教室
                if (room.Id == assignment.ClassroomId)
                    continue;

                // 检查容量是否足够
                if (room.Capacity < courseSection.Enrollment)
                    continue;

                // 检查教室在此时间段是否已有其他课程
                if (solution.HasClassroomConflict(room.Id, assignment.TimeSlotId, assignment.SectionId))
                    continue;

                // 检查教室在此时间段是否可用
                var roomAvailability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ra => ra.ClassroomId == room.Id && ra.TimeSlotId == assignment.TimeSlotId);

                if (roomAvailability != null && !roomAvailability.IsAvailable)
                    continue;

                // 教室适合
                suitableRooms.Add(room.Id);
            }

            return suitableRooms;
        }

        private List<int> GetQualifiedTeachers(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            // 实现逻辑：找出所有有资格教授当前课程的教师
            var qualifiedTeachers = new List<int>();

            if (solution.Problem == null)
                return qualifiedTeachers;

            var courseSection = solution.Problem.CourseSections
                .FirstOrDefault(s => s.Id == assignment.SectionId);

            if (courseSection == null)
                return qualifiedTeachers;

            foreach (var teacher in solution.Problem.Teachers)
            {
                // 排除当前教师
                if (teacher.Id == assignment.TeacherId)
                    continue;

                // 检查教师是否有资格教授此课程
                var preference = solution.Problem.TeacherCoursePreferences
                    .FirstOrDefault(p => p.TeacherId == teacher.Id &&
                                       p.CourseId == courseSection.CourseId &&
                                       p.ProficiencyLevel >= 3);

                if (preference == null)
                    continue;

                // 检查教师在此时间段是否已有其他课程
                if (solution.HasTeacherConflict(teacher.Id, assignment.TimeSlotId, assignment.SectionId))
                    continue;

                // 检查教师在此时间段是否可用
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == teacher.Id && ta.TimeSlotId == assignment.TimeSlotId);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // 教师合格
                qualifiedTeachers.Add(teacher.Id);
            }

            return qualifiedTeachers;
        }

        /// <summary>
        /// 查找潜在的交换伙伴ID列表
        /// </summary>
        private List<int> FindPotentialSwapPartners(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            // 简单实现：随机选择几个其他分配作为潜在交换伙伴
            return solution.Assignments
                .Where(a => a.Id != assignment.Id)
                .OrderBy(x => _random.Next())
                .Take(3)
                .Select(a => a.Id)
                .ToList();
        }

        /// <summary>
        /// 验证移动是否满足所有硬约束
        /// </summary>
        private bool IsValidMove(SchedulingSolution solution, IMove move)
        {
            // 应用移动到临时解
            var tempSolution = move.Apply(solution);

            // 验证所有硬约束
            foreach (var constraint in _constraintManager.GetHardConstraints())
            {
                if (!constraint.IsSatisfied(tempSolution))
                {
                    return false;
                }
            }

            return true;
        }
    }
}