using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class Semester
    {
        public int SemesterId { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // 导航属性
        public ICollection<CourseSection> CourseSections { get; set; }
        public ICollection<ScheduleResult> ScheduleResults { get; set; }

    }
}
