// Teacher availability constraint - Hard constraint
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchedulingSystem.Scheduling.Constraints;
using SmartSchedulingSystem.Scheduling.Models;

namespace SmartSchedulingSystem.Scheduling.Constraints.Level2_ConfigurableHard
{
    /// <summary>
    /// Teacher availability constraint - Teachers can only teach during their available time slots
    /// </summary>
    public class TeacherAvailabilityConstraint : BaseConstraint, IConstraint
    {
        /// <summary>
        /// Dictionary of teacher unavailable times (teacher ID -> list of unavailable times)
        /// </summary>
        private readonly Dictionary<int, List<(DateTime Start, DateTime End, string Reason)>> _teacherUnavailableTimes;
        
        /// <summary>
        /// List of unavailable time periods
        /// </summary>
        protected readonly List<(DateTime Start, DateTime End, string Reason)> UnavailablePeriods;

        /// <summary>
        /// Dictionary of semester dates (problem ID -> semester start/end dates)
        /// </summary>
        protected readonly Dictionary<int, (DateTime Start, DateTime End)> SemesterDates;

        /// <summary>
        /// Constraint definition ID
        /// </summary>
        public override string DefinitionId => ConstraintDefinitions.TeacherAvailability;
        
        /// <summary>
        /// Constraint ID
        /// </summary>
        public override int Id => 202;
        
        /// <summary>
        /// Constraint name
        /// </summary>
        public override string Name { get; } = "Teacher Availability Constraint";
        
        /// <summary>
        /// Constraint description
        /// </summary>
        public override string Description { get; } = "Teachers can only be assigned to courses during their available time slots";
        
        /// <summary>
        /// Whether this is a hard constraint
        /// </summary>
        public override bool IsHard => true;
        
        /// <summary>
        /// Constraint hierarchy level
        /// </summary>
        public override ConstraintHierarchy Hierarchy => ConstraintHierarchy.Level2_ConfigurableHard;
        
        /// <summary>
        /// Constraint category
        /// </summary>
        public override string Category => ConstraintCategory.TimeAllocation;
        
        /// <summary>
        /// Associated basic scheduling rule
        /// </summary>
        public override string BasicRule => BasicSchedulingRules.ResourceAvailability;

        /// <summary>
        /// Constructor
        /// </summary>
        public TeacherAvailabilityConstraint() : base()
        {
            _teacherUnavailableTimes = new Dictionary<int, List<(DateTime Start, DateTime End, string Reason)>>();
            UnavailablePeriods = new List<(DateTime Start, DateTime End, string Reason)>();
            SemesterDates = new Dictionary<int, (DateTime Start, DateTime End)>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="teacherUnavailableTimes">Dictionary of teacher unavailable times</param>
        /// <param name="unavailablePeriods">Global unavailable periods (e.g., holidays)</param>
        /// <param name="semesterDates">Semester date information</param>
        public TeacherAvailabilityConstraint(
            Dictionary<int, List<(DateTime Start, DateTime End, string Reason)>> teacherUnavailableTimes,
            List<(DateTime Start, DateTime End, string Reason)> unavailablePeriods,
            Dictionary<int, (DateTime Start, DateTime End)> semesterDates) 
        {
            _teacherUnavailableTimes = teacherUnavailableTimes ?? new Dictionary<int, List<(DateTime Start, DateTime End, string Reason)>>();
            UnavailablePeriods = unavailablePeriods ?? new List<(DateTime Start, DateTime End, string Reason)>();
            SemesterDates = semesterDates ?? new Dictionary<int, (DateTime Start, DateTime End)>();
        }

        /// <summary>
        /// Add teacher unavailable time
        /// </summary>
        /// <param name="teacherId">Teacher ID</param>
        /// <param name="start">Start time</param>
        /// <param name="end">End time</param>
        /// <param name="reason">Reason</param>
        public void AddTeacherUnavailableTime(int teacherId, DateTime start, DateTime end, string reason)
        {
            if (!_teacherUnavailableTimes.ContainsKey(teacherId))
            {
                _teacherUnavailableTimes[teacherId] = new List<(DateTime, DateTime, string)>();
            }
            _teacherUnavailableTimes[teacherId].Add((start, end, reason));
        }

        /// <summary>
        /// Evaluate constraint
        /// </summary>
        /// <param name="solution">Scheduling solution</param>
        /// <returns>Constraint evaluation result</returns>
        public override (double Score, List<SchedulingConflict> Conflicts) Evaluate(SchedulingSolution solution)
        {
            if (!IsValidSolution(solution))
            {
                return (0, new List<SchedulingConflict>());
            }
            
            var conflicts = new List<SchedulingConflict>();
            
            // Iterate through all course assignments
            foreach (var assignment in solution.Assignments)
            {
                int teacherId = assignment.TeacherId;
                int timeSlotId = assignment.TimeSlotId;
                
                // Look up teacher availability from problem definition
                var availability = solution.Problem.TeacherAvailabilities
                    .FirstOrDefault(ta => ta.TeacherId == teacherId && ta.TimeSlotId == timeSlotId);
                
                // If availability record found and teacher is not available
                if (availability != null && !availability.IsAvailable)
                {
                    conflicts.Add(new SchedulingConflict
                    {
                        ConstraintId = Id,
                        Type = SchedulingConflictType.TeacherUnavailable,
                        Description = $"Teacher {assignment.TeacherName} is not available at time slot {timeSlotId}",
                        Severity = ConflictSeverity.Severe,
                        InvolvedEntities = new Dictionary<string, List<int>>
                        {
                            { "Teachers", new List<int> { teacherId } },
                            { "TimeSlots", new List<int> { timeSlotId } }
                        }
                    });
                }
            }
            
            // Calculate constraint satisfaction score
            double score = conflicts.Count == 0 ? 1.0 : 0.0;
            
            return (score, conflicts);
        }

        private bool IsValidSolution(SchedulingSolution solution)
        {
            return solution != null && 
                   solution.Assignments != null && 
                   solution.Assignments.Count > 0 && 
                   solution.Problem != null &&
                   solution.Problem.TeacherAvailabilities != null &&
                   solution.Problem.TeacherAvailabilities.Count > 0;
        }

        public override bool IsSatisfied(SchedulingSolution solution)
        {
            return Evaluate(solution).Score >= 1.0;
        }

        /// <summary>
        /// Check if two time periods overlap
        /// </summary>
        protected bool DoPeriodsOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            return start1 < end2 && start2 < end1;
        }

        /// <summary>
        /// Check if specific time period is unavailable
        /// </summary>
        protected string CheckTimeAvailability(DateTime startTime, DateTime endTime)
        {
            foreach (var period in UnavailablePeriods)
            {
                if (DoPeriodsOverlap(startTime, endTime, period.Start, period.End))
                {
                    return period.Reason;
                }
            }
            return null;
        }

        /// <summary>
        /// Calculate specific date from semester information and week
        /// </summary>
        protected DateTime? CalculateDate(int problemId, int week, int dayOfWeek)
        {
            if (!SemesterDates.TryGetValue(problemId, out var semesterDates))
                return null;

            // Calculate start date of current week
            DateTime weekStartDate = semesterDates.Start.AddDays((week - 1) * 7);
            
            // Calculate specific date (based on day of week)
            int dayOffset = dayOfWeek - 1; // Assuming dayOfWeek starts from 1, 1=Monday
            return weekStartDate.AddDays(dayOffset);
        }
    }
}