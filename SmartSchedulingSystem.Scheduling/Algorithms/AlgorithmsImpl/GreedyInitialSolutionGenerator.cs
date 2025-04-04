using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using SmartSchedulingSystem.Scheduling.Algorithms;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;

namespace SchedulingSystem.Scheduling.Algorithms.AlgorithmsImpl
{
    /// <summary>
    /// Greedy algorithm implementation for generating initial scheduling solutions
    /// </summary>
    public class GreedyAlgorithm : ISchedulingAlgorithm
    {
        private readonly ISolutionEvaluator _evaluator;
        private readonly IConstraintManager _constraintManager;
        private readonly IConflictResolver _conflictResolver;
        private readonly ILogger<GreedyAlgorithm> _logger;
        private readonly Random _random;

        private SchedulingProblem _problem;
        private SchedulingParameters _parameters;

        public GreedyAlgorithm(
            ISolutionEvaluator evaluator,
            IConstraintManager constraintManager,
            IConflictResolver conflictResolver,
            ILogger<GreedyAlgorithm> logger,
            int? randomSeed = null)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _constraintManager = constraintManager ?? throw new ArgumentNullException(nameof(constraintManager));
            _conflictResolver = conflictResolver ?? throw new ArgumentNullException(nameof(conflictResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _random = new Random(randomSeed ?? Environment.TickCount);
        }

        public void Initialize(
            SchedulingProblem problem,
            SchedulingParameters parameters,
            IConstraintManager constraintManager,
            IConflictResolver conflictResolver,
            ISolutionEvaluator evaluator)
        {
            _problem = problem ?? throw new ArgumentNullException(nameof(problem));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            // We already have these injected, so we don't need to reassign them
        }

        /// <summary>
        /// Generate initial scheduling solution
        /// </summary>
        public async Task<SchedulingSolution> GenerateInitialSolutionAsync(CancellationToken cancellationToken = default)
        {
            if (_problem == null || _parameters == null)
                throw new InvalidOperationException("Must call Initialize before generating solution");

            _logger.LogInformation("Starting to generate initial scheduling solution");

            var solution = new SchedulingSolution
            {
                ProblemId = _problem.Id,
                Name = "Initial Solution",
                Algorithm = "Greedy Algorithm"
            };

            // Sort courses by difficulty (prioritize special requirements and high enrollment)
            var sortedSections = _problem.CourseSections
                .OrderByDescending(s => !string.IsNullOrEmpty(s.RequiredRoomType) || !string.IsNullOrEmpty(s.RequiredEquipment))
                .ThenByDescending(s => s.Enrollment)
                .ThenByDescending(s => s.CrossListedWithId.HasValue)
                .ThenBy(s => _random.Next()) // Add randomness to avoid identical results
                .ToList();

            _logger.LogDebug($"Number of courses sorted by priority: {sortedSections.Count}");

            int assignedCount = 0;
            int failedCount = 0;

            // Handle cross-listed courses first to ensure they're scheduled together
            var crossListedGroups = sortedSections
                .Where(s => s.CrossListedWithId.HasValue)
                .GroupBy(s => s.CrossListedWithId.Value)
                .ToList();

            foreach (var group in crossListedGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var primarySection = group.First();
                _logger.LogTrace($"Attempting to assign cross-listed course group: {primarySection.CourseCode}");

                if (await TryAssignCrossListedGroupAsync(group.ToList(), solution, cancellationToken))
                {
                    assignedCount += group.Count();
                    _logger.LogDebug($"Successfully assigned cross-listed group for {primarySection.CourseCode}");
                }
                else
                {
                    failedCount += group.Count();
                    _logger.LogWarning($"Failed to assign cross-listed group for {primarySection.CourseCode}");
                }
            }

            // Process remaining courses
            foreach (var section in sortedSections.Where(s => !s.CrossListedWithId.HasValue))
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogTrace($"Attempting to assign course: {section.CourseCode} {section.SectionCode}");

                // Try to assign the course
                if (await TryAssignSectionAsync(section, solution, cancellationToken))
                {
                    assignedCount++;

                    // Check for conflicts periodically
                    if (assignedCount % 10 == 0)
                    {
                        _logger.LogDebug($"Assigned {assignedCount} courses, performing intermediate conflict check");

                        var evaluation = _evaluator.Evaluate(solution);
                        if (!evaluation.IsFeasible)
                        {
                            _logger.LogWarning("Solution has conflicts, attempting to repair");

                            // Try to resolve conflicts
                            solution = await _conflictResolver.ResolveConflictsAsync(
                                solution,
                                evaluation.Conflicts,
                                _parameters.ConflictResolutionStrategy,
                                cancellationToken) ?? solution;
                        }
                    }
                }
                else
                {
                    failedCount++;
                    _logger.LogWarning($"Could not assign course: {section.CourseCode} {section.SectionCode}");
                }
            }

            // Final evaluation
            var finalEvaluation = _evaluator.Evaluate(solution);
            solution.Evaluation = finalEvaluation;

            _logger.LogInformation($"Initial solution generated. Total courses: {sortedSections.Count}, Assigned: {assignedCount}, Unassigned: {failedCount}, Feasible: {finalEvaluation.IsFeasible}, Score: {finalEvaluation.Score:F4}");

            return solution;
        }

        public Task<SchedulingSolution> OptimizeSolutionAsync(SchedulingSolution solution, CancellationToken cancellationToken = default)
        {
            // Greedy algorithm doesn't optimize, just return the original solution
            return Task.FromResult(solution);
        }

        /// <summary>
        /// Try to assign a cross-listed course group together
        /// </summary>
        private async Task<bool> TryAssignCrossListedGroupAsync(
            List<CourseSectionInfo> sections,
            SchedulingSolution solution,
            CancellationToken cancellationToken)
        {
            // Find suitable teachers, classrooms and time slots that can accommodate all sections
            var primarySection = sections.First();

            // Since all cross-listed sections share same time and room, we find suitable teachers for each
            Dictionary<int, List<TeacherInfo>> sectionTeacherMap = new Dictionary<int, List<TeacherInfo>>();

            foreach (var section in sections)
            {
                var teachers = FindSuitableTeachers(section);
                if (!teachers.Any())
                    return false;

                sectionTeacherMap[section.Id] = teachers;
            }

            // Find suitable classrooms based on combined enrollment
            int totalEnrollment = sections.Sum(s => s.Enrollment);
            var suitableClassrooms = FindSuitableClassrooms(primarySection, totalEnrollment);

            if (!suitableClassrooms.Any())
                return false;

            // Find suitable time slots
            var suitableTimeSlots = FindSuitableTimeSlots(primarySection);

            if (!suitableTimeSlots.Any())
                return false;

            // Try combinations
            foreach (var classroom in suitableClassrooms)
            {
                foreach (var timeSlot in suitableTimeSlots)
                {
                    // Check if the classroom is available at this time
                    if (solution.HasClassroomConflict(classroom.Id, timeSlot.Id))
                        continue;

                    // Try to find a valid teacher assignment for all sections
                    bool assignmentSuccessful = true;
                    var assignedTeachers = new Dictionary<int, TeacherInfo>();

                    foreach (var section in sections)
                    {
                        bool teacherAssigned = false;

                        foreach (var teacher in sectionTeacherMap[section.Id])
                        {
                            // Check if this teacher is already assigned to another section in this group
                            if (assignedTeachers.Values.Any(t => t.Id == teacher.Id))
                                continue;

                            // Check if teacher is available at this time
                            if (solution.HasTeacherConflict(teacher.Id, timeSlot.Id))
                                continue;

                            assignedTeachers[section.Id] = teacher;
                            teacherAssigned = true;
                            break;
                        }

                        if (!teacherAssigned)
                        {
                            assignmentSuccessful = false;
                            break;
                        }
                    }

                    // If all sections have been assigned a teacher, create assignments
                    if (assignmentSuccessful)
                    {
                        int assignmentId = solution.Assignments.Count > 0 ?
                            solution.Assignments.Max(a => a.Id) + 1 : 1;

                        foreach (var section in sections)
                        {
                            var teacher = assignedTeachers[section.Id];

                            var assignment = new ScheduleAssignment
                            {
                                Id = assignmentId++,
                                SectionId = section.Id,
                                SectionCode = section.SectionCode,
                                TeacherId = teacher.Id,
                                TeacherName = teacher.Name,
                                ClassroomId = classroom.Id,
                                ClassroomName = $"{classroom.Building}-{classroom.Name}",
                                TimeSlotId = timeSlot.Id,
                                DayOfWeek = timeSlot.DayOfWeek,
                                StartTime = timeSlot.StartTime,
                                EndTime = timeSlot.EndTime,
                                WeekPattern = Enumerable.Range(1, 14).ToList() // Default to all weeks
                            };

                            if (!solution.AddAssignment(assignment))
                            {
                                // If adding failed, there's a conflict we didn't catch
                                _logger.LogWarning($"Failed to add cross-listed assignment for {section.SectionCode}");
                                return false;
                            }
                        }

                        _logger.LogInformation($"Successfully assigned cross-listed group with {sections.Count} sections");
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Try to assign a course to a teacher, classroom and time slot
        /// </summary>
        private async Task<bool> TryAssignSectionAsync(
            CourseSectionInfo section,
            SchedulingSolution solution,
            CancellationToken cancellationToken)
        {
            var suitableTeachers = FindSuitableTeachers(section);
            if (!suitableTeachers.Any())
            {
                _logger.LogWarning($"No suitable teachers found for course {section.CourseCode}");
                return false;
            }

            var suitableClassrooms = FindSuitableClassrooms(section, section.Enrollment);
            if (!suitableClassrooms.Any())
            {
                _logger.LogWarning($"No suitable classrooms found for course {section.CourseCode}");
                return false;
            }

            var suitableTimeSlots = FindSuitableTimeSlots(section);
            if (!suitableTimeSlots.Any())
            {
                _logger.LogWarning($"No suitable time slots found for course {section.CourseCode}");
                return false;
            }

            // Try all combinations to find a valid assignment
            foreach (var teacher in suitableTeachers)
            {
                foreach (var classroom in suitableClassrooms)
                {
                    foreach (var timeSlot in suitableTimeSlots)
                    {
                        // Check for conflicts
                        if (solution.HasTeacherConflict(teacher.Id, timeSlot.Id) ||
                            solution.HasClassroomConflict(classroom.Id, timeSlot.Id))
                        {
                            continue;
                        }

                        // Check gender restriction constraints
                        if (!string.IsNullOrEmpty(section.GenderRestriction) &&
                            !IsGenderRestrictionSatisfied(section, classroom, timeSlot, solution))
                        {
                            continue;
                        }

                        // Check teacher workload constraints
                        if (!IsTeacherWorkloadAcceptable(teacher, timeSlot, solution))
                        {
                            continue;
                        }

                        // Create assignment
                        int assignmentId = solution.Assignments.Count > 0 ?
                            solution.Assignments.Max(a => a.Id) + 1 : 1;

                        var assignment = new ScheduleAssignment
                        {
                            Id = assignmentId,
                            SectionId = section.Id,
                            SectionCode = section.SectionCode,
                            TeacherId = teacher.Id,
                            TeacherName = teacher.Name,
                            ClassroomId = classroom.Id,
                            ClassroomName = $"{classroom.Building}-{classroom.Name}",
                            TimeSlotId = timeSlot.Id,
                            DayOfWeek = timeSlot.DayOfWeek,
                            StartTime = timeSlot.StartTime,
                            EndTime = timeSlot.EndTime,
                            WeekPattern = Enumerable.Range(1, 14).ToList() // Default to all weeks
                        };

                        if (solution.AddAssignment(assignment))
                        {
                            _logger.LogDebug($"Successfully assigned course {section.CourseCode} to teacher {teacher.Name}, classroom {classroom.Name}, timeslot {timeSlot.DayOfWeek}-{timeSlot.StartTime}");
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Find suitable teachers for a course section
        /// </summary>
        private List<TeacherInfo> FindSuitableTeachers(CourseSectionInfo section)
        {
            // First try teachers with preference for this course
            var preferredTeachers = _problem.TeacherCoursePreferences
                .Where(p => p.CourseId == section.CourseId)
                .OrderByDescending(p => p.PreferenceLevel)
                .Select(p => p.TeacherId)
                .ToList();

            var qualifiedTeachers = new List<TeacherInfo>();

            foreach (var teacherId in preferredTeachers)
            {
                var teacher = _problem.Teachers.FirstOrDefault(t => t.Id == teacherId);
                if (teacher != null)
                    qualifiedTeachers.Add(teacher);
            }

            // If no preferred teachers, try teachers from the same department
            if (!qualifiedTeachers.Any())
            {
                qualifiedTeachers.AddRange(_problem.Teachers
                    .Where(t => t.DepartmentId == section.DepartmentId)
                    .ToList());
            }

            // If still no teachers and cross-department teaching is allowed, use any teacher
            if (!qualifiedTeachers.Any() && _parameters.Constraints.AllowCrossDepartmentTeaching)
            {
                qualifiedTeachers.AddRange(_problem.Teachers);
            }

            return qualifiedTeachers.Any()
                ? qualifiedTeachers.OrderBy(_ => _random.Next()).ToList()
                : new List<TeacherInfo>();
        }

        /// <summary>
        /// Find suitable classrooms for a course section
        /// </summary>
        private List<ClassroomInfo> FindSuitableClassrooms(CourseSectionInfo section, int requiredCapacity)
        {
            // Filter by classroom type if required
            var suitableClassrooms = !string.IsNullOrEmpty(section.RequiredRoomType)
                ? _problem.Classrooms.Where(c => c.Type == section.RequiredRoomType).ToList()
                : _problem.Classrooms.ToList();

            // Filter by capacity
            suitableClassrooms = suitableClassrooms
                .Where(c => c.Capacity >= requiredCapacity)
                .ToList();

            // Filter by required equipment
            if (!string.IsNullOrEmpty(section.RequiredEquipment))
            {
                var requiredEquipments = section.RequiredEquipment.Split(',')
                    .Select(e => e.Trim())
                    .ToList();

                suitableClassrooms = suitableClassrooms
                    .Where(c => !string.IsNullOrEmpty(c.Equipment) &&
                        requiredEquipments.All(req => c.Equipment.Split(',')
                            .Select(e => e.Trim())
                            .Contains(req)))
                    .ToList();
            }

            // Prioritize classrooms in the home building/department
            if (_parameters.Constraints.PrioritizeHomeBuildings && !string.IsNullOrEmpty(section.DepartmentName))
            {
                var homeClassrooms = suitableClassrooms
                    .Where(c => c.Building.Contains(section.DepartmentName))
                    .ToList();

                if (homeClassrooms.Any())
                    suitableClassrooms = homeClassrooms;
            }

            // Check classroom availabilities
            var unavailableClassroomIds = _problem.ClassroomAvailabilities
                .Where(a => !a.IsAvailable)
                .Select(a => a.ClassroomId)
                .Distinct()
                .ToList();

            suitableClassrooms = suitableClassrooms
                .Where(c => !unavailableClassroomIds.Contains(c.Id))
                .ToList();

            return suitableClassrooms.Any()
                ? suitableClassrooms.OrderBy(_ => _random.Next()).ToList()
                : new List<ClassroomInfo>();
        }

        /// <summary>
        /// Find suitable time slots for a course section
        /// </summary>
        private List<TimeSlotInfo> FindSuitableTimeSlots(CourseSectionInfo section)
        {
            var availableTimeSlots = _problem.TimeSlots.ToList();

            // Handle Ramadan schedule if enabled
            if (_parameters.Constraints.EnableRamadanSchedule)
            {
                availableTimeSlots = availableTimeSlots
                    .Where(ts => ts.Type == "Ramadan" || ts.Type == "Regular")
                    .ToList();
            }

            // Remove holiday time slots if specified
            if (_parameters.Constraints.HolidayExclusions)
            {
                // In a real implementation, we would check against a holiday database
                // For now, we'll assume all timeslots are valid
            }

            return availableTimeSlots.Any()
                ? availableTimeSlots.OrderBy(_ => _random.Next()).ToList()
                : new List<TimeSlotInfo>();
        }

        /// <summary>
        /// Check if a classroom assignment satisfies gender restriction requirements
        /// </summary>
        private bool IsGenderRestrictionSatisfied(
            CourseSectionInfo section,
            ClassroomInfo classroom,
            TimeSlotInfo timeSlot,
            SchedulingSolution solution)
        {
            if (!_parameters.Constraints.EnableGenderSegregation ||
                string.IsNullOrEmpty(section.GenderRestriction))
                return true;

            // Find all courses in the same building and time slot
            string buildingName = classroom.Building;

            var conflictingAssignments = solution.Assignments
                .Where(a => a.TimeSlotId == timeSlot.Id &&
                      a.ClassroomName.StartsWith(buildingName))
                .ToList();

            if (!conflictingAssignments.Any())
                return true;

            // Check if any conflicting assignments have a different gender restriction
            foreach (var assignment in conflictingAssignments)
            {
                var conflictingSection = _problem.CourseSections
                    .FirstOrDefault(s => s.Id == assignment.SectionId);

                if (conflictingSection != null &&
                    !string.IsNullOrEmpty(conflictingSection.GenderRestriction) &&
                    conflictingSection.GenderRestriction != section.GenderRestriction)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if adding this assignment keeps the teacher's workload acceptable
        /// </summary>
        private bool IsTeacherWorkloadAcceptable(
            TeacherInfo teacher,
            TimeSlotInfo timeSlot,
            SchedulingSolution solution)
        {
            var teacherAssignments = solution.GetAssignmentsForTeacher(teacher.Id).ToList();

            // Check weekly hours
            int totalHours = teacherAssignments.Count * 2; // Assuming 2 hours per assignment
            if (teacher.MaxWeeklyHours > 0 && totalHours >= teacher.MaxWeeklyHours)
                return false;

            // Check daily hours
            var dailyAssignments = teacherAssignments
                .Where(a => a.DayOfWeek == timeSlot.DayOfWeek)
                .ToList();

            int dailyHours = dailyAssignments.Count * 2;
            if (teacher.MaxDailyHours > 0 && dailyHours >= teacher.MaxDailyHours)
                return false;

            // Check consecutive hours
            if (dailyAssignments.Any())
            {
                // Sort assignments by time
                var sortedAssignments = dailyAssignments
                    .OrderBy(a => a.StartTime)
                    .ToList();

                // Check if this assignment would create too many consecutive hours
                bool wouldBeConsecutive = sortedAssignments.Any(a =>
                    (a.EndTime >= timeSlot.StartTime && a.EndTime <= timeSlot.EndTime) ||
                    (a.StartTime <= timeSlot.EndTime && a.StartTime >= timeSlot.StartTime) ||
                    (a.StartTime <= timeSlot.StartTime && a.EndTime >= timeSlot.EndTime));

                if (wouldBeConsecutive)
                {
                    int consecutiveHours = CalculateConsecutiveHours(sortedAssignments, timeSlot);
                    if (consecutiveHours > _parameters.Constraints.MaximumConsecutiveClasses)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculate the number of consecutive teaching hours if a new time slot is added
        /// </summary>
        private int CalculateConsecutiveHours(List<ScheduleAssignment> assignments, TimeSlotInfo newTimeSlot)
        {
            var allSlots = new List<(TimeSpan Start, TimeSpan End)>();

            // Add existing assignments
            foreach (var assignment in assignments)
            {
                allSlots.Add((assignment.StartTime, assignment.EndTime));
            }

            // Add the new time slot
            allSlots.Add((newTimeSlot.StartTime, newTimeSlot.EndTime));

            // Sort by start time
            allSlots = allSlots.OrderBy(s => s.Start).ToList();

            // Calculate longest consecutive chain
            int maxConsecutive = 1;
            int currentConsecutive = 1;

            for (int i = 1; i < allSlots.Count; i++)
            {
                var previous = allSlots[i - 1];
                var current = allSlots[i];

                // Check if slots are consecutive (≤15 minutes gap)
                if ((current.Start - previous.End).TotalMinutes <= 15)
                {
                    currentConsecutive++;
                }
                else
                {
                    maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
                    currentConsecutive = 1;
                }
            }

            return Math.Max(maxConsecutive, currentConsecutive);
        }
    }
}