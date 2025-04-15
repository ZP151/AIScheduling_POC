using System;
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

        // 添加字段
        public int EnrollmentCount { get; set; } // 课程注册人数
        public int ClassroomCapacity { get; set; } // 教室容量
        public string CourseType { get; set; } // 课程类型（讲座、实验室等）
        public string RoomType { get; set; } // 教室类型
        public int? CampusId { get; set; } // 校区ID
        public string CampusName { get; set; } // 校区名称
        public int? DepartmentId { get; set; } // 系ID
        public string DepartmentName { get; set; } // 系名称
        
        // 冲突信息
        public bool HasConflict { get; set; } = false;
        public string ConflictDescription { get; set; }
    }
}
