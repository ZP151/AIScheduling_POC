using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class ClassroomAvailability
    {
        public int ClassroomId { get; set; }
        public int TimeSlotId { get; set; }
        public bool IsAvailable { get; set; }

        // 导航属性
        public Classroom Classroom { get; set; }
        public TimeSlot TimeSlot { get; set; }
    }

}
