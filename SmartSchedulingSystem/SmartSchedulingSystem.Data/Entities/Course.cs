using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SmartSchedulingSystem.Data.Entities.Classroom;

namespace SmartSchedulingSystem.Data.Entities
{
    public class Course
    {
        public int CourseId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int Credits { get; set; }
        public int WeeklyHours { get; set; }
        public int DepartmentId { get; set; }
       
        // 导航属性
        public Department Department { get; set; }
        public ICollection<CourseSection> CourseSections { get; set; }
        public ICollection<Prerequisite> Prerequisites { get; set; }

    }
}
