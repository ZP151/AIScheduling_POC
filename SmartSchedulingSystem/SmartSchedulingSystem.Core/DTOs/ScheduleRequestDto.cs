using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    // Request DTO
    public class ScheduleRequestDto
    {
        // Original fields - backend style
        public int SemesterId { get; set; }
        public List<int> CourseSectionIds { get; set; }
        public List<int> TeacherIds { get; set; }
        public List<int> ClassroomIds { get; set; }
        public List<int> TimeSlotIds { get; set; }

        // Compatible field names for frontend
        public int? Semester { get; set; } // Map to SemesterId
        public List<int> Courses { get; set; } // Map to CourseSectionIds
        public List<int> Teachers { get; set; } // Map to TeacherIds
        public List<int> Classrooms { get; set; } // Map to ClassroomIds
        public List<int> TimeSlots { get; set; } // Map to TimeSlotIds
        
        // Full data objects (used when not connected to database)
        public List<CourseSectionExtDto> CourseSectionObjects { get; set; }
        public List<TeacherExtDto> TeacherObjects { get; set; }
        public List<ClassroomExtDto> ClassroomObjects { get; set; }
        public List<TimeSlotExtDto> TimeSlotObjects { get; set; }

        public bool UseAIAssistance { get; set; } = false;
        public List<ConstraintSettingDto> ConstraintSettings { get; set; }
        // Add fields
        public string SchedulingScope { get; set; } = "programme"; // university, campus, school, department, programme
        public int? CampusId { get; set; }
        public int? SchoolId { get; set; }
        public int? DepartmentId { get; set; }
        public int? ProgrammeId { get; set; }
        
        // Fields matching frontend
        public int? Campus { get; set; } // Map to CampusId
        public int? School { get; set; } // Map to SchoolId
        public int? Department { get; set; } // Map to DepartmentId
        public int? Subject { get; set; } // New field
        public int? Programme { get; set; } // Map to ProgrammeId
        
        // Scheduling parameters
        public double FacultyWorkloadBalance { get; set; } = 0.8;
        public double StudentScheduleCompactness { get; set; } = 0.7;
        public double ClassroomTypeMatchingWeight { get; set; } = 0.7;
        public int MinimumTravelTime { get; set; } = 30;
        public int MaximumConsecutiveClasses { get; set; } = 3;
        public double CampusTravelTimeWeight { get; set; } = 0.6;
        public double PreferredClassroomProximity { get; set; } = 0.5;
        public bool GenerateMultipleSolutions { get; set; } = true;
        public int SolutionCount { get; set; } = 3;
    }
}
