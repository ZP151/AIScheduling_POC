using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Models
{
    /// <summary>
    /// Course scheduling assignment, represents a single course arrangement
    /// </summary>
    public class SchedulingAssignment
    {
        /// <summary>
        /// Unique ID of the scheduling assignment
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Course section ID
        /// </summary>
        public int SectionId { get; set; }

        /// <summary>
        /// Course section ID (synonym with SectionId, for compatibility)
        /// </summary>
        public int CourseSectionId { get => SectionId; set => SectionId = value; }

        /// <summary>
        /// Course section code
        /// </summary>
        public string SectionCode { get; set; }

        /// <summary>
        /// Teacher ID
        /// </summary>
        public int TeacherId { get; set; }

        /// <summary>
        /// Teacher name
        /// </summary>
        public string TeacherName { get; set; }

        /// <summary>
        /// Classroom ID
        /// </summary>
        public int ClassroomId { get; set; }

        // Which session of the weekly course meetings
        public int SessionNumber { get; set; }
        
        /// <summary>
        /// Classroom name
        /// </summary>
        public string ClassroomName { get; set; }

        /// <summary>
        /// Building where classroom is located
        /// </summary>
        public string Building { get; set; }

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
        /// Teaching weeks pattern (e.g. [1,2,3,4,5,6,7,8,9,10,11,12])
        /// </summary>
        public List<int> WeekPattern { get; set; } = new List<int>();

        /// <summary>
        /// Teacher navigation property (populated at runtime, not stored)
        /// </summary>
        public TeacherInfo Teacher { get; set; }

        /// <summary>
        /// Classroom navigation property (populated at runtime, not stored)
        /// </summary>
        public ClassroomInfo Classroom { get; set; }

        /// <summary>
        /// Course section navigation property (populated at runtime, not stored)
        /// </summary>
        public CourseSectionInfo CourseSection { get; set; }

        /// <summary>
        /// Time slot navigation property (populated at runtime, not stored)
        /// </summary>
        public TimeSlotInfo TimeSlot { get; set; }

        /// <summary>
        /// Week number (corresponds to WeekPattern, for compatibility)
        /// </summary>
        public int Week { get => WeekPattern.Count > 0 ? WeekPattern.First() : 1; }
    }
}
