using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.DTOs
{
    // 响应DTO
    public class ScheduleResultDto
    {
        public int ScheduleId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public double Score { get; set; }
        public List<ScheduleItemDto> Items { get; set; }
        public List<string> Conflicts { get; set; }
    }
}
