using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class TeacherPreference
    {
        public int TeacherId { get; set; }
        public int TimeSlotId { get; set; }
        public int PreferenceLevel { get; set; } // 1-5
                                                 // ✅ 导航属性
        public Teacher Teacher { get; set; }
        public TimeSlot TimeSlot { get; set; }
    }

}
