using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Data.Context;
using SmartSchedulingSystem.Data.Entities;

namespace SmartSchedulingSystem.Core.Services
{
    public class TimeSlotService : ITimeSlotService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public TimeSlotService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<TimeSlotDto>> GetAllTimeSlotsAsync()
        {
            var timeSlots = await _dbContext.TimeSlots.ToListAsync();
            return _mapper.Map<List<TimeSlotDto>>(timeSlots);
        }

        public async Task<TimeSlotDto> GetTimeSlotByIdAsync(int timeSlotId)
        {
            var timeSlot = await _dbContext.TimeSlots.FindAsync(timeSlotId);
            return _mapper.Map<TimeSlotDto>(timeSlot);
        }

        public async Task<TimeSlotDto> CreateTimeSlotAsync(TimeSlotDto timeSlotDto)
        {
            var timeSlot = _mapper.Map<TimeSlot>(timeSlotDto);
            _dbContext.TimeSlots.Add(timeSlot);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<TimeSlotDto>(timeSlot);
        }

        public async Task<TimeSlotDto> UpdateTimeSlotAsync(TimeSlotDto timeSlotDto)
        {
            var timeSlot = await _dbContext.TimeSlots.FindAsync(timeSlotDto.TimeSlotId);

            if (timeSlot == null)
                throw new KeyNotFoundException($"TimeSlot with ID {timeSlotDto.TimeSlotId} not found");

            _mapper.Map(timeSlotDto, timeSlot);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TimeSlotDto>(timeSlot);
        }

        public async Task<bool> DeleteTimeSlotAsync(int timeSlotId)
        {
            var timeSlot = await _dbContext.TimeSlots.FindAsync(timeSlotId);

            if (timeSlot == null)
                return false;

            _dbContext.TimeSlots.Remove(timeSlot);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<List<TimeSlotDto>> GetAvailableTimeSlotsAsync(int semesterId)
        {
            // 获取特定学期的可用时间段
            // 可以根据需要实现更复杂的逻辑，如排除已被占用的时间段
            var timeSlots = await _dbContext.TimeSlots.ToListAsync();
            return _mapper.Map<List<TimeSlotDto>>(timeSlots);
        }
    }
}