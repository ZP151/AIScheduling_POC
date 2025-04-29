using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Data.Context;
using SmartSchedulingSystem.Data.Entities;

namespace SmartSchedulingSystem.Core.Services
{
    public class ClassroomService : IClassroomService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public ClassroomService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<ClassroomDto>> GetAllClassroomsAsync()
        {
            var classrooms = await _dbContext.Classrooms.ToListAsync();
            return _mapper.Map<List<ClassroomDto>>(classrooms);
        }

        public async Task<ClassroomDto> GetClassroomByIdAsync(int classroomId)
        {
            var classroom = await _dbContext.Classrooms.FindAsync(classroomId);
            return _mapper.Map<ClassroomDto>(classroom);
        }

        public async Task<List<ClassroomAvailabilityDto>> GetClassroomAvailabilityAsync(int classroomId)
        {
            var availabilities = await _dbContext.ClassroomAvailabilities
                .Where(ca => ca.ClassroomId == classroomId)
                .Include(ca => ca.TimeSlot)
                .ToListAsync();
            return _mapper.Map<List<ClassroomAvailabilityDto>>(availabilities);
        }

        public async Task<bool> UpdateClassroomAvailabilityAsync(int classroomId, List<ClassroomAvailabilityDto> availabilities)
        {
            // 删除现有可用性设置
            var existingAvailabilities = await _dbContext.ClassroomAvailabilities
                .Where(ca => ca.ClassroomId == classroomId)
                .ToListAsync();
            _dbContext.ClassroomAvailabilities.RemoveRange(existingAvailabilities);

            // 添加新的可用性设置
            var newAvailabilities = _mapper.Map<List<ClassroomAvailability>>(availabilities);
            foreach (var availability in newAvailabilities)
            {
                availability.ClassroomId = classroomId;
            }
            _dbContext.ClassroomAvailabilities.AddRange(newAvailabilities);

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<ClassroomDto> CreateClassroomAsync(ClassroomDto classroomDto)
        {
            var classroom = _mapper.Map<Classroom>(classroomDto);
            _dbContext.Classrooms.Add(classroom);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<ClassroomDto>(classroom);
        }

        public async Task<ClassroomDto> UpdateClassroomAsync(ClassroomDto classroomDto)
        {
            var classroom = await _dbContext.Classrooms.FindAsync(classroomDto.ClassroomId);

            if (classroom == null)
                throw new KeyNotFoundException($"Classroom with ID {classroomDto.ClassroomId} not found");

            _mapper.Map(classroomDto, classroom);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<ClassroomDto>(classroom);
        }

        public async Task<bool> DeleteClassroomAsync(int classroomId)
        {
            var classroom = await _dbContext.Classrooms.FindAsync(classroomId);

            if (classroom == null)
                return false;

            _dbContext.Classrooms.Remove(classroom);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}