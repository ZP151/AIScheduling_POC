using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// Solution status enumeration
    /// </summary>
    public enum SolutionStatus
    {
        /// <summary>
        /// Feasible solution
        /// </summary>
        Feasible,
        
        /// <summary>
        /// Optimal solution
        /// </summary>
        Optimal,
        
        /// <summary>
        /// Infeasible solution
        /// </summary>
        Infeasible,
        
        /// <summary>
        /// Unknown status
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Represents a solution to the course scheduling problem
    /// </summary>
    public class SchedulingSolution
    {
        /// <summary>
        /// Unique ID of the solution
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID of the scheduling problem this solution belongs to
        /// </summary>
        public int ProblemId { get; set; }
        public SchedulingProblem Problem { get; set; }

        public int? SolutionSetId { get; set; }
        public SchedulingEvaluation Evaluation { get; set; } // optional

        /// <summary>
        /// Solution score, directly returns Evaluation.Score
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Solution name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of course assignments
        /// </summary>
        public List<SchedulingAssignment> Assignments { get; set; } = new List<SchedulingAssignment>();

        /// <summary>
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Generation time
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Algorithm used to generate this solution
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Add property to track at which constraint level the solution was generated
        /// </summary>
        public Engine.ConstraintApplicationLevel ConstraintLevel { get; set; } = Engine.ConstraintApplicationLevel.Basic;

        /// <summary>
        /// Solution status
        /// </summary>
        public SolutionStatus Status { get; set; } = SolutionStatus.Unknown;
        
        /// <summary>
        /// Additional data from solution generation process
        /// </summary>
        public Dictionary<string, string> GenerationData { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Get course assignments for a specific section
        /// </summary>
        /// <param name="sectionId">Course section ID</param>
        /// <returns>List of course assignments</returns>
        public IEnumerable<SchedulingAssignment> GetAssignmentsForSection(int sectionId)
        {
            return Assignments.Where(a => a.SectionId == sectionId);
        }

        /// <summary>
        /// Get course assignments for a specific teacher
        /// </summary>
        /// <param name="teacherId">Teacher ID</param>
        /// <returns>List of course assignments</returns>
        public IEnumerable<SchedulingAssignment> GetAssignmentsForTeacher(int teacherId)
        {
            return Assignments.Where(a => a.TeacherId == teacherId);
        }

        /// <summary>
        /// Get course assignments for a specific classroom
        /// </summary>
        /// <param name="classroomId">Classroom ID</param>
        /// <returns>List of course assignments</returns>
        public IEnumerable<SchedulingAssignment> GetAssignmentsForClassroom(int classroomId)
        {
            return Assignments.Where(a => a.ClassroomId == classroomId);
        }

        /// <summary>
        /// Get course assignments for a specific time slot
        /// </summary>
        /// <param name="timeSlotId">Time slot ID</param>
        /// <returns>List of course assignments</returns>
        public IEnumerable<SchedulingAssignment> GetAssignmentsForTimeSlot(int timeSlotId)
        {
            return Assignments.Where(a => a.TimeSlotId == timeSlotId);
        }

        public IEnumerable<SchedulingAssignment> GetAssignmentsForDay(int dayOfWeek, List<TimeSlotInfo> timeSlots)
        {
            var relevantTimeSlots = timeSlots.Where(ts => ts.DayOfWeek == dayOfWeek).Select(ts => ts.Id).ToList();
            return Assignments.Where(a => relevantTimeSlots.Contains(a.TimeSlotId));
        }

        /// <summary>
        /// Check if there is a teacher conflict for the specified time slot
        /// </summary>
        /// <param name="teacherId">Teacher ID</param>
        /// <param name="timeSlotId">Time slot ID</param>
        /// <param name="ignoreSectionId">Course section ID to ignore (optional)</param>
        /// <returns>Whether there is a conflict</returns>
        public bool HasTeacherConflict(int teacherId, int timeSlotId, int? ignoreSectionId = null)
        {
            return Assignments.Any(a =>
                a.TeacherId == teacherId &&
                a.TimeSlotId == timeSlotId &&
                (!ignoreSectionId.HasValue || a.SectionId != ignoreSectionId.Value));
        }

        /// <summary>
        /// Check if there is a classroom conflict for the specified time slot
        /// </summary>
        /// <param name="classroomId">Classroom ID</param>
        /// <param name="timeSlotId">Time slot ID</param>
        /// <param name="ignoreSectionId">Course section ID to ignore (optional)</param>
        /// <returns>Whether there is a conflict</returns>
        public bool HasClassroomConflict(int classroomId, int timeSlotId, int? ignoreSectionId = null)
        {
            return Assignments.Any(a =>
                a.ClassroomId == classroomId &&
                a.TimeSlotId == timeSlotId &&
                (!ignoreSectionId.HasValue || a.SectionId != ignoreSectionId.Value));
        }

        /// <summary>
        /// Add a course assignment
        /// </summary>
        /// <param name="assignment">Course assignment</param>
        /// <returns>Whether the addition was successful</returns>
        public bool AddAssignment(SchedulingAssignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment));

            // Check for conflicts
            if (HasTeacherConflict(assignment.TeacherId, assignment.TimeSlotId) ||
                HasClassroomConflict(assignment.ClassroomId, assignment.TimeSlotId))
            {
                return false;
            }

            Assignments.Add(assignment);
            return true;
        }

        /// <summary>
        /// Remove a course assignment
        /// </summary>
        /// <param name="assignmentId">Course assignment ID</param>
        /// <returns>Whether the removal was successful</returns>
        public bool RemoveAssignment(int assignmentId)
        {
            var assignment = Assignments.FirstOrDefault(a => a.Id == assignmentId);
            if (assignment != null)
            {
                return Assignments.Remove(assignment);
            }

            return false;
        }

        /// <summary>
        /// Update a course assignment
        /// </summary>
        /// <param name="assignment">Updated course assignment</param>
        /// <returns>Whether the update was successful</returns>
        public bool UpdateAssignment(SchedulingAssignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment));

            int index = Assignments.FindIndex(a => a.Id == assignment.Id);
            if (index >= 0)
            {
                // Remove old assignment
                Assignments.RemoveAt(index);

                // Check if new assignment would cause conflicts
                if (HasTeacherConflict(assignment.TeacherId, assignment.TimeSlotId, assignment.SectionId) ||
                    HasClassroomConflict(assignment.ClassroomId, assignment.TimeSlotId, assignment.SectionId))
                {
                    // Conflict found, restore original assignment
                    Assignments.Insert(index, assignment);
                    return false;
                }

                // No conflicts, add new assignment
                Assignments.Add(assignment);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Create a deep copy of the solution
        /// </summary>
        public SchedulingSolution Clone()
        {
            var clone = new SchedulingSolution
            {
                Id = this.Id,
                ProblemId = this.ProblemId,
                Problem = this.Problem,
                SolutionSetId = this.SolutionSetId,
                Name = this.Name,
                CreatedAt = this.CreatedAt,
                Algorithm = this.Algorithm
            };

            // Deep copy all assignments
            clone.Assignments = Assignments.Select(a => new SchedulingAssignment
            {
                Id = a.Id,
                SectionId = a.SectionId,
                SectionCode = a.SectionCode,
                TeacherId = a.TeacherId,
                TeacherName = a.TeacherName,
                ClassroomId = a.ClassroomId,
                ClassroomName = a.ClassroomName,
                TimeSlotId = a.TimeSlotId,
                DayOfWeek = a.DayOfWeek,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                WeekPattern = a.WeekPattern != null ? new List<int>(a.WeekPattern) : new List<int>()
            }).ToList();

            return clone;
        }

        /// <summary>
        /// Get next conflict ID
        /// </summary>
        public int GetNextConflictId()
        {
            // If evaluation object exists, calculate existing conflicts max ID and add 1
            if (Evaluation != null && Evaluation.Conflicts != null && Evaluation.Conflicts.Any())
            {
                return Evaluation.Conflicts.Max(c => c.Id) + 1;
            }
            
            // Otherwise start from 1
            return 1;
        }
    }
}