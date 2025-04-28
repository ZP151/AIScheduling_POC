using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Algorithms.LS.Moves;
using SmartSchedulingSystem.Scheduling.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using SmartSchedulingSystem.Scheduling.Utils;

namespace SmartSchedulingSystem.Scheduling.Algorithms.LS
{
    /// <summary>
    /// Responsible for generating valid optimization moves during local search phase
    /// </summary>
    public class MoveGenerator
    {
        private readonly Random _random = new Random();
        private readonly ILogger<MoveGenerator> _logger;
        private readonly ConstraintManager _constraintManager;
        private readonly Utils.SchedulingParameters _parameters;

        public MoveGenerator(
            ILogger<MoveGenerator> logger,
            ConstraintManager constraintManager,
            Utils.SchedulingParameters parameters = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _parameters = parameters ?? new Utils.SchedulingParameters();
        }

        /// <summary>
        /// Generate valid moves
        /// </summary>
        /// <param name="solution">Current solution</param>
        /// <param name="assignment">Course assignment to optimize</param>
        /// <param name="maxMoves">Maximum number of moves to generate</param>
        /// <returns>List of valid moves</returns>
        public List<IMove> GenerateValidMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            int maxMoves = 10)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));

            try
            {
                _logger.LogDebug($"Generating moves for assignment #{assignment.Id}, maximum count: {maxMoves}");

                var validMoves = new List<IMove>();

                // Add time moves
                AddTimeSlotMoves(solution, assignment, validMoves);

                // Add room moves
                AddRoomMoves(solution, assignment, validMoves);

                // Add teacher moves
                AddTeacherMoves(solution, assignment, validMoves);

                // Add swap moves
                AddSwapMoves(solution, assignment, validMoves);

                _logger.LogDebug($"Generated {validMoves.Count} valid moves");

                // If too many moves generated, randomly select maxMoves
                if (validMoves.Count > maxMoves)
                {
                    validMoves = validMoves
                        .OrderBy(x => _random.Next())
                        .Take(maxMoves)
                        .ToList();

                    _logger.LogDebug($"Randomly selected {maxMoves} moves");
                }

                return validMoves;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating moves: {ex.Message}");
                return new List<IMove>();
            }
        }

        // Add move generation methods for specific constraint types in MoveGenerator.cs
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
                    // For other conflict types, generate generic moves
                    moves.AddRange(GenerateGenericMoves(solution, assignment));
                    break;
            }

            return moves;
        }

        private List<IMove> GenerateMovesForTeacherConflict(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var moves = new List<IMove>();

            // 1. Try moving to other time slots
            var availableTimeSlots = GetAvailableTimeSlots(solution, assignment);
            foreach (var timeSlot in availableTimeSlots)
            {
                moves.Add(new TimeMove(assignment.Id, timeSlot));
            }

            // 2. Try changing teacher
            var qualifiedTeachers = GetQualifiedTeachers(solution, assignment);
            foreach (var teacher in qualifiedTeachers)
            {
                moves.Add(new TeacherMove(assignment.Id, teacher));
            }

            return moves;
        }

        // Implement move generation methods for other conflict types...
        private List<IMove> GenerateMovesForClassroomConflict(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var moves = new List<IMove>();

            // 1. Try changing classroom
            var suitableRooms = GetSuitableRooms(solution, assignment);
            foreach (var roomId in suitableRooms)
            {
                moves.Add(new RoomMove(assignment.Id, roomId));
            }

            // 2. Try changing time slot
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

            // Find classrooms with larger capacity
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

            // 1. Try moving to time slots where teacher is available
            var teacherAvailableTimeSlots = GetTeacherAvailableTimeSlots(solution, assignment);
            foreach (var timeSlotId in teacherAvailableTimeSlots)
            {
                moves.Add(new TimeMove(assignment.Id, timeSlotId));
            }

            // 2. Try changing to available teacher
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

            // 1. Try moving to more suitable time slots (increase travel time)
            var betterTimeSlots = GetTimeSlotWithSufficientTravelTime(solution, assignment);
            foreach (var timeSlotId in betterTimeSlots)
            {
                moves.Add(new TimeMove(assignment.Id, timeSlotId));
            }

            // 2. Try changing to closer classroom
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

            // For prerequisite conflicts, mainly adjust time slots
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

            // 1. Time moves
            var availableTimeSlots = GetAvailableTimeSlots(solution, assignment);
            foreach (var timeSlotId in availableTimeSlots.Take(3)) // Limit number of moves generated
            {
                moves.Add(new TimeMove(assignment.Id, timeSlotId));
            }

            // 2. Room moves
            var suitableRooms = GetSuitableRooms(solution, assignment);
            foreach (var roomId in suitableRooms.Take(3))
            {
                moves.Add(new RoomMove(assignment.Id, roomId));
            }

            // 3. Teacher moves
            var qualifiedTeachers = GetQualifiedTeachers(solution, assignment);
            foreach (var teacherId in qualifiedTeachers.Take(2))
            {
                moves.Add(new TeacherMove(assignment.Id, teacherId));
            }

            // 4. Randomly select some courses for swapping
            var swapCandidates = solution.Assignments
                .Where(a => a.Id != assignment.Id)
                .OrderBy(x => Guid.NewGuid())
                .Take(2);

            foreach (var candidate in swapCandidates)
            {
                moves.Add(new SwapMove(assignment.Id, candidate.Id, true, false, false)); // Swap time
                moves.Add(new SwapMove(assignment.Id, candidate.Id, false, true, false)); // Swap classroom
            }

            return moves;
        }

        /// <summary>
        /// Add time moves
        /// </summary>
        private void AddTimeSlotMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            try
            {
                // Get available time slots
                var availableTimeSlots = GetAvailableTimeSlots(solution, assignment);

                if (availableTimeSlots.Count == 0)
                {
                    _logger.LogDebug("No available time slots found");
                    return;
                }

                _logger.LogDebug($"Found {availableTimeSlots.Count} available time slots");

                // Randomly select some time slots to add as moves
                var selectedTimeSlots = availableTimeSlots
                    .OrderBy(x => _random.Next()) // Randomize
                    .Take(Math.Min(3, availableTimeSlots.Count))
                    .ToList();

                foreach (var timeSlotId in selectedTimeSlots)
                {
                    // Create time slot move
                    var move = new TimeSlotMove(assignment.Id, timeSlotId);
                    moves.Add(move);
                    _logger.LogDebug($"Added time slot move: Assign {assignment.Id} to time slot {timeSlotId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding time slot moves");
            }
        }

        // Add necessary helper methods in MoveGenerator.cs
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
                // Exclude current classroom
                if (room.Id == assignment.ClassroomId)
                    continue;

                // Check if capacity is larger and sufficient
                if (room.Capacity <= currentRoom.Capacity || room.Capacity < courseSection.Enrollment)
                    continue;

                // Check if classroom is already scheduled in this time slot
                if (solution.HasClassroomConflict(room.Id, assignment.TimeSlotId, assignment.SectionId))
                    continue;

                // Check if classroom is available in this time slot
                var roomAvailability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ra => ra.ClassroomId == room.Id && ra.TimeSlotId == assignment.TimeSlotId);

                if (roomAvailability != null && !roomAvailability.IsAvailable)
                    continue;

                // Classroom suitable
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
                // Exclude current time slot
                if (timeSlot.Id == assignment.TimeSlotId)
                    continue;

                // Check if teacher is available in this time slot
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId && ta.TimeSlotId == timeSlot.Id);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // Check if teacher is already scheduled in this time slot
                if (solution.HasTeacherConflict(assignment.TeacherId, timeSlot.Id, assignment.SectionId))
                    continue;

                // Check if classroom is already scheduled in this time slot
                if (solution.HasClassroomConflict(assignment.ClassroomId, timeSlot.Id, assignment.SectionId))
                    continue;

                // Time slot available
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
                // Exclude current teacher
                if (teacher.Id == assignment.TeacherId)
                    continue;

                // Check if teacher is qualified to teach this course
                var preference = solution.Problem.TeacherCoursePreferences
                    .FirstOrDefault(p => p.TeacherId == teacher.Id &&
                                       p.CourseId == courseSection.CourseId);

                if (preference == null || preference.ProficiencyLevel < 3)
                    continue;

                // Check if teacher is already scheduled in this time slot
                if (solution.HasTeacherConflict(teacher.Id, assignment.TimeSlotId, assignment.SectionId))
                    continue;

                // Check if teacher is available in this time slot
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == teacher.Id && ta.TimeSlotId == assignment.TimeSlotId);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // Teacher available
                availableTeachers.Add(teacher.Id);
            }

            return availableTeachers;
        }

        private List<int> GetTimeSlotWithSufficientTravelTime(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var suitableTimeSlots = new List<int>();

            if (solution.Problem == null)
                return suitableTimeSlots;

            // Get all other assignments of the current teacher
            var teacherAssignments = solution.Assignments
                .Where(a => a.TeacherId == assignment.TeacherId && a.Id != assignment.Id)
                .ToList();

            // If teacher has no other courses, any time slot can be used
            if (!teacherAssignments.Any())
            {
                return GetAvailableTimeSlots(solution, assignment);
            }

            // Get current classroom's campus information
            var currentClassroom = solution.Problem.Classrooms
                .FirstOrDefault(c => c.Id == assignment.ClassroomId);

            if (currentClassroom == null)
                return suitableTimeSlots;

            int currentCampusId = currentClassroom.CampusId;

            // Check each time slot
            foreach (var timeSlot in solution.Problem.TimeSlots)
            {
                // Exclude current time slot
                if (timeSlot.Id == assignment.TimeSlotId)
                    continue;

                // Check if teacher is available in this time slot
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId && ta.TimeSlotId == timeSlot.Id);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // Check if classroom is available in this time slot
                var roomAvailability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ra => ra.ClassroomId == assignment.ClassroomId && ra.TimeSlotId == timeSlot.Id);

                if (roomAvailability != null && !roomAvailability.IsAvailable)
                    continue;

                // Check if teacher is already scheduled in this time slot
                if (solution.HasTeacherConflict(assignment.TeacherId, timeSlot.Id, assignment.SectionId))
                    continue;

                // Check if classroom is already scheduled in this time slot
                if (solution.HasClassroomConflict(assignment.ClassroomId, timeSlot.Id, assignment.SectionId))
                    continue;

                // Check if travel time to other teacher's courses is sufficient
                bool hasSufficientTravelTime = true;

                foreach (var otherAssignment in teacherAssignments)
                {
                    var otherTimeSlot = solution.Problem.TimeSlots
                        .FirstOrDefault(t => t.Id == otherAssignment.TimeSlotId);

                    if (otherTimeSlot == null)
                        continue;

                    // If on the same day
                    if (timeSlot.DayOfWeek == otherTimeSlot.DayOfWeek)
                    {
                        // Calculate time difference (minutes)
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
                            // Time overlap
                            hasSufficientTravelTime = false;
                            break;
                        }

                        // Get other assigned classroom's campus information
                        var otherClassroom = solution.Problem.Classrooms
                            .FirstOrDefault(c => c.Id == otherAssignment.ClassroomId);

                        if (otherClassroom == null)
                            continue;

                        int otherCampusId = otherClassroom.CampusId;

                        // If in different campus, more travel time is needed
                        if (currentCampusId != otherCampusId)
                        {
                            // Get campus travel time (assume at least 30 minutes)
                            int travelTime = 30;

                            if (timeDifference < travelTime)
                            {
                                hasSufficientTravelTime = false;
                                break;
                            }
                        }
                        else if (timeDifference < 15) // Same campus at least needs 15 minutes
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

        // Add necessary helper methods (continued) in MoveGenerator.cs
        private List<int> GetNearbyRooms(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            var nearbyRooms = new List<int>();

            if (solution.Problem == null)
                return nearbyRooms;

            // Get current classroom information
            var currentClassroom = solution.Problem.Classrooms
                .FirstOrDefault(c => c.Id == assignment.ClassroomId);

            if (currentClassroom == null)
                return nearbyRooms;

            int currentCampusId = currentClassroom.CampusId;
            string currentBuilding = currentClassroom.Building;

            // Get all other assignments of the current teacher
            var teacherAssignments = solution.Assignments
                .Where(a => a.TeacherId == assignment.TeacherId && a.Id != assignment.Id)
                .ToList();

            // Check each classroom
            foreach (var room in solution.Problem.Classrooms)
            {
                // Exclude current classroom
                if (room.Id == assignment.ClassroomId)
                    continue;

                // Check if capacity is sufficient
                var courseSection = solution.Problem.CourseSections
                    .FirstOrDefault(s => s.Id == assignment.SectionId);

                if (courseSection != null && room.Capacity < courseSection.Enrollment)
                    continue;

                // Check if classroom is already scheduled in this time slot
                if (solution.HasClassroomConflict(room.Id, assignment.TimeSlotId, assignment.SectionId))
                    continue;

                // Check if classroom is available in this time slot
                var roomAvailability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ra => ra.ClassroomId == room.Id && ra.TimeSlotId == assignment.TimeSlotId);

                if (roomAvailability != null && !roomAvailability.IsAvailable)
                    continue;

                // Prefer classrooms in the same building
                if (room.Building == currentBuilding)
                {
                    nearbyRooms.Add(room.Id);
                    continue;
                }

                // If no other assignments, same campus classrooms can also be used
                if (!teacherAssignments.Any() && room.CampusId == currentCampusId)
                {
                    nearbyRooms.Add(room.Id);
                    continue;
                }

                // For teachers with other assignments, need to consider distance to other courses
                bool isSuitable = true;
                foreach (var otherAssignment in teacherAssignments)
                {
                    var otherTimeSlot = solution.Problem.TimeSlots
                        .FirstOrDefault(t => t.Id == otherAssignment.TimeSlotId);

                    if (otherTimeSlot == null)
                        continue;

                    // If on the same day
                    if (assignment.DayOfWeek == otherTimeSlot.DayOfWeek)
                    {
                        // Calculate time difference (minutes)
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
                            // Time overlap, not considered
                            continue;
                        }

                        // Get other assigned classroom
                        var otherClassroom = solution.Problem.Classrooms
                            .FirstOrDefault(c => c.Id == otherAssignment.ClassroomId);

                        if (otherClassroom == null)
                            continue;

                        // If time difference is large enough or in the same campus/building, suitable
                        if (timeDifference >= 30 ||
                            (room.CampusId == otherClassroom.CampusId && timeDifference >= 15) ||
                            (room.Building == otherClassroom.Building && timeDifference >= 10))
                        {
                            // Suitable
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

            // Get current course
            var courseSection = solution.Problem.CourseSections
                .FirstOrDefault(s => s.Id == assignment.SectionId);

            if (courseSection == null)
                return suitableTimeSlots;

            int courseId = courseSection.CourseId;

            // Get courses with prerequisite relationships
            var prerequisites = solution.Problem.Prerequisites
                .Where(p => p.CourseId == courseId || p.PrerequisiteCourseId == courseId)
                .ToList();

            // If no prerequisite relationships, all available time slots can be used
            if (!prerequisites.Any())
            {
                return GetAvailableTimeSlots(solution, assignment);
            }

            // Get involved course IDs
            var relatedCourseIds = new HashSet<int>();
            foreach (var prereq in prerequisites)
            {
                if (prereq.CourseId == courseId)
                {
                    relatedCourseIds.Add(prereq.PrerequisiteCourseId); // Prerequisite course of current course
                }
                else if (prereq.PrerequisiteCourseId == courseId)
                {
                    relatedCourseIds.Add(prereq.CourseId); // Course with current course as prerequisite
                }
            }

            // Get all classes of these courses
            var relatedSectionIds = solution.Problem.CourseSections
                .Where(s => relatedCourseIds.Contains(s.CourseId))
                .Select(s => s.Id)
                .ToList();

            // Get these classes' arrangements
            var relatedAssignments = solution.Assignments
                .Where(a => relatedSectionIds.Contains(a.SectionId))
                .ToList();

            // Check each time slot
            foreach (var timeSlot in solution.Problem.TimeSlots)
            {
                // Exclude current time slot
                if (timeSlot.Id == assignment.TimeSlotId)
                    continue;

                // Check if teacher is available in this time slot
                var teacherAvailability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == assignment.TeacherId && ta.TimeSlotId == timeSlot.Id);

                if (teacherAvailability != null && !teacherAvailability.IsAvailable)
                    continue;

                // Check if classroom is available in this time slot
                var roomAvailability = solution.Problem.ClassroomAvailabilities
                    .FirstOrDefault(ra => ra.ClassroomId == assignment.ClassroomId && ra.TimeSlotId == timeSlot.Id);

                if (roomAvailability != null && !roomAvailability.IsAvailable)
                    continue;

                // Check if teacher is already scheduled in this time slot
                if (solution.HasTeacherConflict(assignment.TeacherId, timeSlot.Id, assignment.SectionId))
                    continue;

                // Check if classroom is already scheduled in this time slot
                if (solution.HasClassroomConflict(assignment.ClassroomId, timeSlot.Id, assignment.SectionId))
                    continue;

                // Check if in the same time slot as courses with prerequisite relationships
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
        /// Add room moves
        /// </summary>
        private void AddRoomMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // Get all suitable classrooms
            var suitableRooms = GetSuitableRooms(solution, assignment);

            _logger.LogDebug($"Found {suitableRooms.Count} suitable classrooms for moves");

            foreach (var roomId in suitableRooms)
            {
                var move = new RoomMove(assignment.Id, roomId);

                // Verify if move satisfies all hard constraints
                if (IsValidMove(solution, move))
                {
                    moves.Add(move);
                }
            }
        }

        /// <summary>
        /// Add teacher moves
        /// </summary>
        private void AddTeacherMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // Get all qualified teachers
            var qualifiedTeachers = GetQualifiedTeachers(solution, assignment);

            _logger.LogDebug($"Found {qualifiedTeachers.Count} qualified teachers for moves");

            foreach (var teacherId in qualifiedTeachers)
            {
                var move = new TeacherMove(assignment.Id, teacherId);

                // Verify if move satisfies all hard constraints
                if (IsValidMove(solution, move))
                {
                    moves.Add(move);
                }
            }
        }

        /// <summary>
        /// Add swap moves
        /// </summary>
        private void AddSwapMoves(
            SchedulingSolution solution,
            SchedulingAssignment assignment,
            List<IMove> moves)
        {
            // Find possible swap partners
            var potentialSwapPartners = FindPotentialSwapPartners(solution, assignment);

            _logger.LogDebug($"Found {potentialSwapPartners.Count} potential swap partners");

            foreach (var partnerId in potentialSwapPartners)
            {
                // Time swap
                var timeSwap = new SwapMove(assignment.Id, partnerId, true, false, false);
                if (IsValidMove(solution, timeSwap))
                {
                    moves.Add(timeSwap);
                }

                // Classroom swap
                var roomSwap = new SwapMove(assignment.Id, partnerId, false, true, false);
                if (IsValidMove(solution, roomSwap))
                {
                    moves.Add(roomSwap);
                }

                // Swap teacher
                var teacherSwap = new SwapMove(assignment.Id, partnerId, false, false, true);
                if (IsValidMove(solution, teacherSwap))
                {
                    moves.Add(teacherSwap);
                }
                // Time and classroom swap
                var timeRoomSwap = new SwapMove(assignment.Id, partnerId, true, true, false);
                if (IsValidMove(solution, timeRoomSwap))
                {
                    moves.Add(timeRoomSwap);
                }
                // Time and teacher swap
                var timeTeacherSwap = new SwapMove(assignment.Id, partnerId, true, false, true);
                if (IsValidMove(solution, timeTeacherSwap))
                {
                    moves.Add(timeTeacherSwap);
                }

                // Classroom and teacher swap
                var roomTeacherSwap = new SwapMove(assignment.Id, partnerId, false, true, true);
                if (IsValidMove(solution, roomTeacherSwap))
                {
                    moves.Add(roomTeacherSwap);
                }


                // Complete swap (time, classroom, teacher)
                var completeSwap = new SwapMove(assignment.Id, partnerId, true, true, true);
                if (IsValidMove(solution, completeSwap))
                {
                    moves.Add(completeSwap);
                }
                
            }
        }

        private List<int> GetAvailableTimeSlots(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            if (solution.Problem == null)
                return new List<int>();

            // Create teacher and classroom unavailable time caches
            var teacherUnavailableTimes = new HashSet<int>();
            var classroomUnavailableTimes = new HashSet<int>();

            // Add teacher already scheduled time
            foreach (var assign in solution.Assignments.Where(a => a.TeacherId == assignment.TeacherId && a.Id != assignment.Id))
            {
                teacherUnavailableTimes.Add(assign.TimeSlotId);
            }

            // Add classroom already scheduled time
            foreach (var assign in solution.Assignments.Where(a => a.ClassroomId == assignment.ClassroomId && a.Id != assignment.Id))
            {
                classroomUnavailableTimes.Add(assign.TimeSlotId);
            }

            // Add teacher unavailable time
            foreach (var avail in solution.Problem.TeacherAvailabilities.Where(ta =>
                         ta.TeacherId == assignment.TeacherId && !ta.IsAvailable))
            {
                teacherUnavailableTimes.Add(avail.TimeSlotId);
            }

            // Add classroom unavailable time
            foreach (var avail in solution.Problem.ClassroomAvailabilities.Where(ca =>
                         ca.ClassroomId == assignment.ClassroomId && !ca.IsAvailable))
            {
                classroomUnavailableTimes.Add(avail.TimeSlotId);
            }

            // Return teacher and classroom available time slots
            return solution.Problem.TimeSlots
                .Select(ts => ts.Id)
                .Where(tsId => tsId != assignment.TimeSlotId && // Exclude current time slot
                       !teacherUnavailableTimes.Contains(tsId) && // Teacher available
                       !classroomUnavailableTimes.Contains(tsId)) // Classroom available
                .ToList();
        }

        // Add class member variable for caching
        private Dictionary<(int sectionId, int classroomId), bool> _roomSuitabilityCache;

        private List<int> GetSuitableRooms(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            if (solution.Problem == null)
                return new List<int>();

            var courseSection = solution.Problem.CourseSections
                .FirstOrDefault(s => s.Id == assignment.SectionId);

            if (courseSection == null)
                return new List<int>();

            // Initialize or update classroom suitability cache
            if (_roomSuitabilityCache == null)
            {
                InitializeRoomSuitabilityCache(solution.Problem);
            }

            // Use cache to quickly filter suitable classrooms
            return solution.Problem.Classrooms
                .Where(classroom =>
                    // 1. Use cache to check if classroom is suitable
                    _roomSuitabilityCache.TryGetValue((courseSection.Id, classroom.Id), out bool isSuitable) &&
                    isSuitable &&
                    // 2. Check if classroom is already scheduled in this time slot
                    !solution.HasClassroomConflict(classroom.Id, assignment.TimeSlotId, assignment.SectionId))
                .Select(c => c.Id)
                .ToList();
        }

        // Initialize classroom suitability cache
        private void InitializeRoomSuitabilityCache(SchedulingProblem problem)
        {
            _roomSuitabilityCache = new Dictionary<(int sectionId, int classroomId), bool>();

            foreach (var section in problem.CourseSections)
            {
                foreach (var classroom in problem.Classrooms)
                {
                    bool isSuitable = classroom.Capacity >= section.Enrollment;

                    // If need to consider classroom type matching, can add more conditions
                    if (!string.IsNullOrEmpty(section.RequiredRoomType) &&
                        !string.IsNullOrEmpty(classroom.Type))
                    {
                        isSuitable = isSuitable && IsCompatibleRoomType(section.RequiredRoomType, classroom.Type);
                    }

                    _roomSuitabilityCache[(section.Id, classroom.Id)] = isSuitable;
                }
            }
        }

        // Check if two classroom types are compatible
        private bool IsCompatibleRoomType(string requiredType, string actualType)
        {
            // Same type, completely compatible
            if (requiredType.Equals(actualType, StringComparison.OrdinalIgnoreCase))
                return true;

            // Lab course must be in lab
            if (requiredType.Contains("Lab", StringComparison.OrdinalIgnoreCase))
                return actualType.Contains("Lab", StringComparison.OrdinalIgnoreCase);

            // Computer course must be in computer room
            if (requiredType.Contains("Computer", StringComparison.OrdinalIgnoreCase))
                return actualType.Contains("Computer", StringComparison.OrdinalIgnoreCase);

            // Regular course can be in multiple type classrooms
            if (requiredType.Contains("Regular", StringComparison.OrdinalIgnoreCase))
                return true;

            // Default not compatible
            return false;
        }

        private Dictionary<(int teacherId, int courseId), bool> _teacherQualificationCache;

        private List<int> GetQualifiedTeachers(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            if (solution.Problem == null)
                return new List<int>();

            var courseSection = solution.Problem.CourseSections
                .FirstOrDefault(s => s.Id == assignment.SectionId);

            if (courseSection == null)
                return new List<int>();

            // Initialize or update qualification cache
            if (_teacherQualificationCache == null)
            {
                InitializeQualificationCache(solution.Problem);
            }

            // Use cache to quickly filter qualified teachers
            return solution.Problem.Teachers
                .Where(teacher =>
                    // 1. Use cache to check if teacher is qualified
                    _teacherQualificationCache.TryGetValue((teacher.Id, courseSection.CourseId), out bool isQualified) &&
                    isQualified &&
                    // 2. Check if teacher is already scheduled in this time slot
                    !solution.HasTeacherConflict(teacher.Id, assignment.TimeSlotId, assignment.SectionId))
                .Select(t => t.Id)
                .ToList();
        }

        // Initialize teacher qualification cache
        private void InitializeQualificationCache(SchedulingProblem problem)
        {
            _teacherQualificationCache = new Dictionary<(int teacherId, int courseId), bool>();

            foreach (var pref in problem.TeacherCoursePreferences)
            {
                _teacherQualificationCache[(pref.TeacherId, pref.CourseId)] = pref.ProficiencyLevel >= 3;
            }

            // Ensure all teacher-course combinations are in cache
            foreach (var teacher in problem.Teachers)
            {
                foreach (var section in problem.CourseSections)
                {
                    var key = (teacher.Id, section.CourseId);
                    if (!_teacherQualificationCache.ContainsKey(key))
                    {
                        _teacherQualificationCache[key] = false; // Default not qualified
                    }
                }
            }
        }

        /// <summary>
        /// Find potential swap partner ID list
        /// </summary>
        private List<int> FindPotentialSwapPartners(SchedulingSolution solution, SchedulingAssignment assignment)
        {
            // Simple implementation: randomly select several other assignments as potential swap partners
            return solution.Assignments
                .Where(a => a.Id != assignment.Id)
                .OrderBy(x => _random.Next())
                .Take(3)
                .Select(a => a.Id)
                .ToList();
        }

        /// <summary>
        /// Verify if move satisfies all hard constraints
        /// </summary>
        private bool IsValidMove(SchedulingSolution solution, IMove move)
        {
            // Apply move to temporary solution
            var tempSolution = move.Apply(solution);

            // Verify all hard constraints
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