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
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                7 => "Sunday",
                _ => "Unknown"
            };
        }
    }
}
