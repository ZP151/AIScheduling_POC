using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Data.Entities
{

    public class AISchedulingSuggestion
    {
        public int SuggestionId { get; set; }
        public int ScheduleRequestId { get; set; }
        public string SuggestionData { get; set; } // JSON格式存储AI建议
        public double Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
