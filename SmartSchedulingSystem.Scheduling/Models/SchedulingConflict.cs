using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Models
{
    public class SchedulingConflict
    {
        public int Id { get; set; }
        public int ConstraintId { get; set; }
        public SchedulingConflictType Type { get; set; }
        public string Description { get; set; }
        public ConflictSeverity Severity { get; set; }
        public Dictionary<string, List<int>> InvolvedEntities { get; set; } = new Dictionary<string, List<int>>();
        public List<int> InvolvedTimeSlots { get; set; } = new List<int>();

        // AI-assisted analysis fields
        public string RootCause { get; set; }
        public List<ConflictResolutionOption> ResolutionOptions { get; set; } = new List<ConflictResolutionOption>();
        public string AIAnalysisDetails { get; set; }
        public DateTime AnalyzedAt { get; set; }

        // Conflict category
        public string Category { get; set; }

        // Conflict resolution status
        public ConflictResolutionStatus ResolutionStatus { get; set; } = ConflictResolutionStatus.Unresolved;

        // Applied resolution ID
        public int? AppliedResolutionId { get; set; }
    }

    public enum ConflictResolutionStatus
    {
        Unresolved,
        Resolved,
        Ignored,
        InProgress
    }

    /// <summary>
    /// Conflict types
    /// </summary>
    public enum SchedulingConflictType
    {
        /// <summary>
        /// Teacher conflict (scheduled for two courses at the same time)
        /// </summary>
        TeacherConflict,

        /// <summary>
        /// Classroom conflict (two courses scheduled at the same time)
        /// </summary>
        ClassroomConflict,

        /// <summary>
        /// Teacher availability conflict (teacher is not available at scheduled time)
        /// </summary>
        TeacherAvailabilityConflict,

        /// <summary>
        /// Classroom availability conflict (classroom is not available at scheduled time)
        /// </summary>
        ClassroomAvailabilityConflict,

        /// <summary>
        /// Classroom capacity exceeded
        /// </summary>
        ClassroomCapacityExceeded,

        /// <summary>
        /// Classroom type mismatch
        /// </summary>
        ClassroomTypeMismatch,

        /// <summary>
        /// Campus travel time conflict
        /// </summary>
        CampusTravelTimeConflict,

        /// <summary>
        /// Course prerequisite conflict
        /// </summary>
        PrerequisiteConflict,

        /// <summary>
        /// Course sequence conflict
        /// </summary>
        CourseSequenceConflict,

        /// <summary>
        /// Teacher workload exceeded
        /// </summary>
        TeacherWorkloadExceeded,

        /// <summary>
        /// Constraint evaluation error
        /// </summary>
        ConstraintEvaluationError,

        BuildingProximityConflict,
        
        /// <summary>
        /// Teacher unavailable
        /// </summary>
        TeacherUnavailable,
        
        /// <summary>
        /// Classroom unavailable
        /// </summary>
        ClassroomUnavailable,
        
        /// <summary>
        /// Building distance conflict (teacher needs to move quickly between different buildings)
        /// </summary>
        BuildingDistanceConflict,
        
        /// <summary>
        /// Equipment mismatch (required equipment not available in classroom)
        /// </summary>
        EquipmentMismatch,
        
        /// <summary>
        /// Other conflicts
        /// </summary>
        Other
    }

    /// <summary>
    /// Conflict severity levels
    /// </summary>
    public enum ConflictSeverity
    {
        /// <summary>
        /// Minor (can be ignored)
        /// </summary>
        Minor,

        /// <summary>
        /// Moderate
        /// </summary>
        Moderate,

        /// <summary>
        /// Severe
        /// </summary>
        Severe,

        /// <summary>
        /// Critical (requires immediate resolution)
        /// </summary>
        Critical
    }
}
