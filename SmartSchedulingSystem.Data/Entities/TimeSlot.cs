using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class TimeSlot
    {
        public int TimeSlotId { get; set; }
        public int DayOfWeek { get; set; } // 1 = Monday, 7 = Sunday
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // 导航属性
        public ICollection<TeacherAvailability> TeacherAvailabilities { get; set; }
        public ICollection<ClassroomAvailability> ClassroomAvailabilities { get; set; }
        public ICollection<ScheduleResult> ScheduleResults { get; set; }
    }

}
