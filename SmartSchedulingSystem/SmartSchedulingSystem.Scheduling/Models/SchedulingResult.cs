using SmartSchedulingSystem.Scheduling.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// Scheduling result, containing solution and evaluation information
    /// </summary>
    public class SchedulingResult
    {
        /// <summary>
        /// Scheduling problem
        /// </summary>
        public SchedulingProblem Problem { get; set; }

        /// <summary>
        /// Final solution
        /// </summary>
        public List<SchedulingSolution> Solutions { get; set; }

        public int SolutionSetId { get; set; }
        /// <summary>
        /// Solution evaluation
        /// </summary>
        public SchedulingEvaluation Evaluation { get; set; }

        /// <summary>
        /// Initial solution
        /// </summary>
        public SchedulingSolution InitialSolution { get; set; }

        /// <summary>
        /// Optimized solution (may differ from final solution if conflict resolution was performed)
        /// </summary>
        public SchedulingSolution OptimizedSolution { get; set; }

        /// <summary>
        /// Running status
        /// </summary>
        public SchedulingStatus Status { get; set; }

        /// <summary>
        /// Execution time (milliseconds)
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Message { get; set; }
        /// <summary>
        /// Whether the result is successful
        /// </summary>
        public bool IsSuccessful => Status == SchedulingStatus.Success;

        /// <summary>
        /// Statistical information
        /// </summary>
        public SchedulingStatistics Statistics { get; set; } = new SchedulingStatistics();

        /// <summary>
        /// Calculate various statistical information
        /// </summary>
        
    }

    /// <summary>
    /// Scheduling running status
    /// </summary>
    public enum SchedulingStatus
    {
        /// <summary>
        /// Not started
        /// </summary>
        NotStarted,

        /// <summary>
        /// Initial solution generated
        /// </summary>
        InitialSolutionGenerated,

        /// <summary>
        /// Success (all constraints satisfied)
        /// </summary>
        Success,

        /// <summary>
        /// Partial success (soft constraints may not be fully satisfied)
        /// </summary>
        PartialSuccess,

        /// <summary>
        /// Failure (no feasible solution found)
        /// </summary>
        Failure,

        /// <summary>
        /// Cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Error
        /// </summary>
        Error
    }

    /// <summary>
    /// Scheduling statistics information
    /// </summary>
    public class SchedulingStatistics
    {
        /// <summary>
        /// Total number of course sections
        /// </summary>
        public int TotalSections { get; set; }

        /// <summary>
        /// Number of scheduled course sections
        /// </summary>
        public int ScheduledSections { get; set; }

        /// <summary>
        /// Number of unscheduled course sections
        /// </summary>
        public int UnscheduledSections { get; set; }

        /// <summary>
        /// Total number of teachers
        /// </summary>
        public int TotalTeachers { get; set; }

        /// <summary>
        /// Number of assigned teachers
        /// </summary>
        public int AssignedTeachers { get; set; }

        /// <summary>
        /// Total number of classrooms
        /// </summary>
        public int TotalClassrooms { get; set; }

        /// <summary>
        /// Number of used classrooms
        /// </summary>
        public int UsedClassrooms { get; set; }

        /// <summary>
        /// Classroom utilization information
        /// </summary>
        public Dictionary<int, ClassroomUtilizationInfo> ClassroomUtilization { get; set; } = new Dictionary<int, ClassroomUtilizationInfo>();

        /// <summary>
        /// Average classroom utilization rate
        /// </summary>
        public double AverageClassroomUtilization { get; set; }

        /// <summary>
        /// Teacher workload information
        /// </summary>
        public Dictionary<int, TeacherWorkloadInfo> TeacherWorkloads { get; set; } = new Dictionary<int, TeacherWorkloadInfo>();

        /// <summary>
        /// Teacher workload standard deviation (measuring workload balance)
        /// </summary>
        public double TeacherWorkloadStdDev { get; set; }

        /// <summary>
        /// Time slot utilization information
        /// </summary>
        public Dictionary<int, TimeSlotUtilizationInfo> TimeSlotUtilization { get; set; } = new Dictionary<int, TimeSlotUtilizationInfo>();

        /// <summary>
        /// Average time slot utilization rate
        /// </summary>
        public double AverageTimeSlotUtilization { get; set; }

        /// <summary>
        /// Peak time slot ID
        /// </summary>
        public int PeakTimeSlotId { get; set; }

        /// <summary>
        /// Peak time slot utilization rate
        /// </summary>
        public double PeakTimeSlotUtilization { get; set; }

        /// <summary>
        /// Lowest time slot ID
        /// </summary>
        public int LowestTimeSlotId { get; set; }

        /// <summary>
        /// Lowest time slot utilization rate
        /// </summary>
        public double LowestTimeSlotUtilization { get; set; }
    }

    /// <summary>
    /// Classroom utilization information
    /// </summary>
    public class ClassroomUtilizationInfo
    {
        /// <summary>
        /// Classroom ID
        /// </summary>
        public int ClassroomId { get; set; }

        /// <summary>
        /// Classroom name
        /// </summary>
        public string ClassroomName { get; set; }

        /// <summary>
        /// Building location
        /// </summary>
        public string Building { get; set; }

        /// <summary>
        /// Utilization rate
        /// </summary>
        public double UtilizationRate { get; set; }

        /// <summary>
        /// Number of assigned courses
        /// </summary>
        public int AssignmentCount { get; set; }
    }

    /// <summary>
    /// Teacher workload information
    /// </summary>
    public class TeacherWorkloadInfo
    {
        /// <summary>
        /// Teacher ID
        /// </summary>
        public int TeacherId { get; set; }

        /// <summary>
        /// Teacher name
        /// </summary>
        public string TeacherName { get; set; }

        /// <summary>
        /// Total teaching hours
        /// </summary>
        public int TotalHours { get; set; }

        /// <summary>
        /// Daily workload (hours)
        /// </summary>
        public Dictionary<int, int> DailyWorkload { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Maximum daily workload (hours)
        /// </summary>
        public int MaxDailyHours { get; set; }

        /// <summary>
        /// Maximum consecutive hours
        /// </summary>
        public int MaxConsecutiveHours { get; set; }

        /// <summary>
        /// Number of assigned courses
        /// </summary>
        public int AssignmentCount { get; set; }
    }

    /// <summary>
    /// Time slot utilization information
    /// </summary>
    public class TimeSlotUtilizationInfo
    {
        /// <summary>
        /// Time slot ID
        /// </summary>
        public int TimeSlotId { get; set; }

        /// <summary>
        /// Day of week (1-7)
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// End time
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Utilization rate
        /// </summary>
        public double UtilizationRate { get; set; }

        /// <summary>
        /// Number of assigned courses
        /// </summary>
        public int AssignmentCount { get; set; }
    }
}