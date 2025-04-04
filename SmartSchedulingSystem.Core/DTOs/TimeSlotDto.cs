using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    public class TimeSlotDto
    {
        public int TimeSlotId { get; set; }
        public int DayOfWeek { get; set; }
        public string DayName => GetDayName(DayOfWeek);
        public string StartTime { get; set; }
        public string EndTime { get; set; }

        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "周一",
                2 => "周二",
                3 => "周三",
                4 => "周四",
                5 => "周五",
                6 => "周六",
                7 => "周日",
                _ => "未知"
            };
        }
    }
}
