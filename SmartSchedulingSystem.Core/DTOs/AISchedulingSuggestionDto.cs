using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    // AI集成DTO
    public class AISchedulingSuggestionDto
    {
        public int SuggestionId { get; set; }
        public int ScheduleRequestId { get; set; }
        public string SuggestionDescription { get; set; }
        public double Score { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ScheduleItemDto> SuggestedItems { get; set; }
    }
}
