using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    // 扩展版CourseSectionDto，用于排课服务
    public class CourseSectionExtDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string SectionCode { get; set; }
        public int Credits { get; set; }
        public double WeeklyHours { get; set; }
        public int SessionsPerWeek { get; set; }
        public double HoursPerSession { get; set; }
        public int Enrollment { get; set; }
        public int DepartmentId { get; set; }
    }
} 