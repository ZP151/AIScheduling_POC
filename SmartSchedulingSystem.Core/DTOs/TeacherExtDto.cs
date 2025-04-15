using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    // 扩展版TeacherDto，用于排课服务
    public class TeacherExtDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int DepartmentId { get; set; }
        public int MaxWeeklyHours { get; set; }
        public int MaxDailyHours { get; set; }
        public int MaxConsecutiveHours { get; set; }
    }
} 