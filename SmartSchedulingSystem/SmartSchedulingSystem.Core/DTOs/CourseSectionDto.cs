using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    public class CourseSectionDto
    {
        public int CourseSectionId { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public int SemesterId { get; set; }
        public string SemesterName { get; set; }
        public string SectionCode { get; set; }
        public int MaxEnrollment { get; set; }
        public int ActualEnrollment { get; set; }
    }
}
