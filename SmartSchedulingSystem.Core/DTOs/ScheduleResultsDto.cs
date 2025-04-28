using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartSchedulingSystem.Core.DTOs
{
    public class ScheduleResultsDto
    {
        public List<ScheduleResultDto> Solutions { get; set; } = new List<ScheduleResultDto>();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public int TotalSolutions { get; set; }
        public double BestScore { get; set; }
        public double AverageScore { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsSuccess { get; set; } = true;
        
        // Optional: Add primary schedule ID property
        public int? PrimaryScheduleId { get; set; }
    }
} 