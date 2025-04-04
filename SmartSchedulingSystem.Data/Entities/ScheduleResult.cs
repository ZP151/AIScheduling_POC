using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{
    public class ScheduleResult
    {
        public int ScheduleId { get; set; }
        public int SemesterId { get; set; } // ✅ 新增        
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // Draft, Published, Cancelled
        public double Score { get; set; }


        // 导航属性
        public ICollection<ScheduleItem> Items { get; set; } = new List<ScheduleItem>();
        public Semester Semester { get; set; }      // 导航属性

    }
}
