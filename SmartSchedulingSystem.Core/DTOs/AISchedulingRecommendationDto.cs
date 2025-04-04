using System;
using System.Collections.Generic;

namespace SmartSchedulingSystem.Core.DTOs
{

    public class AISchedulingRecommendationDto
    {
      
        public ScheduleResultDto BaseSchedule { get; set; }

        public List<string> AISuggestions { get; set; }

        public List<string> ConflictAnalysis { get; set; }

        public double OptimizationScore { get; set; }

        public DateTime CreatedAt { get; set; }
    }

}