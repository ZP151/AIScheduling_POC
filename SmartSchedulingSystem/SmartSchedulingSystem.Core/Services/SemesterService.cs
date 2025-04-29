// Core/Services/SemesterService.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Data.Context;
using SmartSchedulingSystem.Data.Entities;

namespace SmartSchedulingSystem.Core.Services
{
    public class SemesterService : ISemesterService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public SemesterService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<SemesterDto>> GetAllSemestersAsync()
        {
            var semesters = await _dbContext.Semesters.ToListAsync();
            return _mapper.Map<List<SemesterDto>>(semesters);
        }

        public async Task<SemesterDto> GetSemesterByIdAsync(int semesterId)
        {
            var semester = await _dbContext.Semesters.FindAsync(semesterId);
            return _mapper.Map<SemesterDto>(semester);
        }

        public async Task<SemesterDto> CreateSemesterAsync(SemesterDto semesterDto)
        {
            var semester = _mapper.Map<Semester>(semesterDto);
            _dbContext.Semesters.Add(semester);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<SemesterDto>(semester);
        }

        public async Task<SemesterDto> UpdateSemesterAsync(SemesterDto semesterDto)
        {
            var semester = await _dbContext.Semesters.FindAsync(semesterDto.SemesterId);

            if (semester == null)
                throw new KeyNotFoundException($"Semester with ID {semesterDto.SemesterId} not found");

            _mapper.Map(semesterDto, semester);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<SemesterDto>(semester);
        }

        public async Task<bool> DeleteSemesterAsync(int semesterId)
        {
            var semester = await _dbContext.Semesters.FindAsync(semesterId);

            if (semester == null)
                return false;

            _dbContext.Semesters.Remove(semester);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}