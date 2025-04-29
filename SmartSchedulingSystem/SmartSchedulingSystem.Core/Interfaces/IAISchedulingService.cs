using SmartSchedulingSystem.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.Interfaces
{
    public interface IAISchedulingService
    {
        Task<AISchedulingRecommendationDto> GenerateAISchedulingRecommendationAsync(ScheduleRequestDto request);
        Task<List<AISchedulingRecommendationDto>> GetAISchedulingRecommendationHistoryAsync(int scheduleRequestId);
        Task<ScheduleResultDto> ApplyAISuggestionsAsync(int scheduleId, List<string> selectedSuggestions);

    }
}
