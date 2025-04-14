using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    // 请求DTO
    public class ScheduleRequestDto
    {
        // 原始字段 - 后端风格
        public int SemesterId { get; set; }
        public List<int> CourseSectionIds { get; set; }
        public List<int> TeacherIds { get; set; }
        public List<int> ClassroomIds { get; set; }
        public List<int> TimeSlotIds { get; set; }

        // 兼容前端的字段名
        public int? Semester { get; set; } // 映射到SemesterId
        public List<int> Courses { get; set; } // 映射到CourseSectionIds
        public List<int> Teachers { get; set; } // 映射到TeacherIds
        public List<int> Classrooms { get; set; } // 映射到ClassroomIds
        public List<int> TimeSlots { get; set; } // 映射到TimeSlotIds

        public bool UseAIAssistance { get; set; } = false;
        public List<ConstraintSettingDto> ConstraintSettings { get; set; }
            // 添加字段
        public string SchedulingScope { get; set; } = "programme"; // university, campus, school, department, programme
        public int? CampusId { get; set; }
        public int? SchoolId { get; set; }
        public int? DepartmentId { get; set; }
        public int? ProgrammeId { get; set; }
        
        // 与前端匹配的字段
        public int? Campus { get; set; } // 映射到CampusId
        public int? School { get; set; } // 映射到SchoolId
        public int? Department { get; set; } // 映射到DepartmentId
        public int? Subject { get; set; } // 新增字段
        public int? Programme { get; set; } // 映射到ProgrammeId
        
        // 调度参数
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
