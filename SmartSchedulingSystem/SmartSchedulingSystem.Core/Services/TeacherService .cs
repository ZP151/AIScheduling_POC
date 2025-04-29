using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Data.Context;
using SmartSchedulingSystem.Data.Entities;

namespace SmartSchedulingSystem.Core.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public TeacherService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<TeacherDto>> GetAllTeachersAsync()
        {
            var teachers = await _dbContext.Teachers
                .Include(t => t.Department)
                .ToListAsync();
            return _mapper.Map<List<TeacherDto>>(teachers);
        }

        public async Task<List<TeacherDto>> GetTeachersByDepartmentAsync(int departmentId)
        {
            var teachers = await _dbContext.Teachers
                .Where(t => t.DepartmentId == departmentId)
                .Include(t => t.Department)
                .ToListAsync();
            return _mapper.Map<List<TeacherDto>>(teachers);
        }

        public async Task<TeacherDto> GetTeacherByIdAsync(int teacherId)
        {
            var teacher = await _dbContext.Teachers
                .Include(t => t.Department)
                .FirstOrDefaultAsync(t => t.TeacherId == teacherId);
            return _mapper.Map<TeacherDto>(teacher);
        }

        public async Task<List<TeacherAvailabilityDto>> GetTeacherAvailabilityAsync(int teacherId)
        {
            var availabilities = await _dbContext.TeacherAvailabilities
                .Where(ta => ta.TeacherId == teacherId)
                .Include(ta => ta.TimeSlot)
                .ToListAsync();
            return _mapper.Map<List<TeacherAvailabilityDto>>(availabilities);
        }

        public async Task<bool> UpdateTeacherAvailabilityAsync(int teacherId, List<TeacherAvailabilityDto> availabilities)
        {
            // 删除现有可用性设置
            var existingAvailabilities = await _dbContext.TeacherAvailabilities
                .Where(ta => ta.TeacherId == teacherId)
                .ToListAsync();
            _dbContext.TeacherAvailabilities.RemoveRange(existingAvailabilities);

            // 添加新的可用性设置
            var newAvailabilities = _mapper.Map<List<TeacherAvailability>>(availabilities);
            foreach (var availability in newAvailabilities)
            {
                availability.TeacherId = teacherId;
            }
            _dbContext.TeacherAvailabilities.AddRange(newAvailabilities);

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<TeacherDto> CreateTeacherAsync(TeacherDto teacherDto)
        {
            var teacher = _mapper.Map<Teacher>(teacherDto);
            _dbContext.Teachers.Add(teacher);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<TeacherDto>(teacher);
        }

        public async Task<TeacherDto> UpdateTeacherAsync(TeacherDto teacherDto)
        {
            var teacher = await _dbContext.Teachers.FindAsync(teacherDto.TeacherId);

            if (teacher == null)
                throw new KeyNotFoundException($"Teacher with ID {teacherDto.TeacherId} not found");

            _mapper.Map(teacherDto, teacher);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TeacherDto>(teacher);
        }

        public async Task<bool> DeleteTeacherAsync(int teacherId)
        {
            var teacher = await _dbContext.Teachers.FindAsync(teacherId);

            if (teacher == null)
                return false;

            _dbContext.Teachers.Remove(teacher);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}