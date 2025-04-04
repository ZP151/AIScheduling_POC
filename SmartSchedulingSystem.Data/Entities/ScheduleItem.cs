using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class ScheduleItem
    {
        public int ScheduleItemId { get; set; }
        public int ScheduleResultId { get; set; }  // 外键
        public int CourseSectionId { get; set; }   // 外键
        public int TeacherId { get; set; }         // 外键
        public int ClassroomId { get; set; }       // 外键
        public int TimeSlotId { get; set; }        // 外键

        // 导航属性
        public ScheduleResult ScheduleResult { get; set; }
        public CourseSection CourseSection { get; set; }
        public Teacher Teacher { get; set; }
        public Classroom Classroom { get; set; }
        public TimeSlot TimeSlot { get; set; }
    }
}
