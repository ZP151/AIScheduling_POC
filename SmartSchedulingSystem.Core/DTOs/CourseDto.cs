using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    public class CourseDto
    {
        public int CourseId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int Credits { get; set; }
        public int WeeklyHours { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }
}
