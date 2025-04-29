using SmartSchedulingSystem.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.Interfaces
{
    public interface ITimeSlotService
    {
        Task<List<TimeSlotDto>> GetAllTimeSlotsAsync();
        Task<TimeSlotDto> GetTimeSlotByIdAsync(int timeSlotId);
        Task<TimeSlotDto> CreateTimeSlotAsync(TimeSlotDto timeSlotDto);
        Task<TimeSlotDto> UpdateTimeSlotAsync(TimeSlotDto timeSlotDto);
        Task<bool> DeleteTimeSlotAsync(int timeSlotId);

        Task<List<TimeSlotDto>> GetAvailableTimeSlotsAsync(int semesterId);

    }
}
