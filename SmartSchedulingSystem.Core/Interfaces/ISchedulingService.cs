using SmartSchedulingSystem.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.Interfaces
{
    public interface ISchedulingService
    {
        Task<ScheduleResultDto> GenerateScheduleAsync(ScheduleRequestDto request);
        Task<List<ScheduleResultDto>> GetScheduleHistoryAsync(int semesterId);
        Task<ScheduleResultDto> GetScheduleByIdAsync(int scheduleId);
        Task<bool> PublishScheduleAsync(int scheduleId);
        Task<bool> CancelScheduleAsync(int scheduleId);
        // 添加新的重载，包含所有 8 个参数
        Task<List<ScheduleResultDto>> GetScheduleHistoryAsync(
            int semesterId,
            string status,
            DateTime? startDate,
            DateTime? endDate,
            int? courseId,
            int? teacherId,
            double? minScore,
            int? maxItems);
    }
}
