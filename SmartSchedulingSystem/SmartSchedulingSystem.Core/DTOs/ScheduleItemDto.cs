﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    public class ScheduleItemDto
    {
        public int ScheduleId { get; set; }
        public int CourseSectionId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string SectionCode { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; }
        public int ClassroomId { get; set; }
        public string ClassroomName { get; set; }
        public string Building { get; set; }
        public int TimeSlotId { get; set; }
        public int DayOfWeek { get; set; }
        public string StartTime { get; set; }
        public string DayName { get; set; }
        public string EndTime { get; set; }
        public double AssignmentScore { get; set; }

        // Add fields
        public int EnrollmentCount { get; set; } // Enrollment count
        public int ClassroomCapacity { get; set; } // Classroom capacity
        public string CourseType { get; set; } // Course type (lecture, laboratory, etc.)
        public string RoomType { get; set; } // Room type
        public int? CampusId { get; set; } // Campus ID
        public string CampusName { get; set; } // Campus name
        public int? DepartmentId { get; set; } // Department ID
        public string DepartmentName { get; set; } // Department name
        
        // Conflict information
        public bool HasConflict { get; set; } = false;
        public string ConflictDescription { get; set; }
    }
}
