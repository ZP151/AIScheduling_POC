using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Data.Context;
using SmartSchedulingSystem.Data.Entities;

namespace SmartSchedulingSystem.Core.Services
{
    public class CourseSectionService : ICourseSectionService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public CourseSectionService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<CourseSectionDto>> GetAllCourseSectionsAsync()
        {
            var sections = await _dbContext.CourseSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Semester)
                .ToListAsync();
            return _mapper.Map<List<CourseSectionDto>>(sections);
        }

        public async Task<List<CourseSectionDto>> GetCourseSectionsBySemesterAsync(int semesterId)
        {
            var sections = await _dbContext.CourseSections
                .Where(cs => cs.SemesterId == semesterId)
                .Include(cs => cs.Course)
                .Include(cs => cs.Semester)
                .ToListAsync();
            return _mapper.Map<List<CourseSectionDto>>(sections);
        }

        public async Task<List<CourseSectionDto>> GetCourseSectionsByCourseAsync(int courseId)
        {
            var sections = await _dbContext.CourseSections
                .Where(cs => cs.CourseId == courseId)
                .Include(cs => cs.Course)
                .Include(cs => cs.Semester)
                .ToListAsync();
            return _mapper.Map<List<CourseSectionDto>>(sections);
        }

        public async Task<CourseSectionDto> GetCourseSectionByIdAsync(int courseSectionId)
        {
            var section = await _dbContext.CourseSections
                .Include(cs => cs.Course)
                .Include(cs => cs.Semester)
                .FirstOrDefaultAsync(cs => cs.CourseSectionId == courseSectionId);
            return _mapper.Map<CourseSectionDto>(section);
        }

        public async Task<CourseSectionDto> CreateCourseSectionAsync(CourseSectionDto sectionDto)
        {
            var courseSection = _mapper.Map<CourseSection>(sectionDto);
            _dbContext.CourseSections.Add(courseSection);
            await _dbContext.SaveChangesAsync();

            // 重新加载关联数据以确保 Include 的属性被填充
            await _dbContext.Entry(courseSection)
                .Reference(cs => cs.Course)
                .LoadAsync();
            await _dbContext.Entry(courseSection)
                .Reference(cs => cs.Semester)
                .LoadAsync();

            return _mapper.Map<CourseSectionDto>(courseSection);
        }

        public async Task<CourseSectionDto> UpdateCourseSectionAsync(CourseSectionDto sectionDto)
        {
            var courseSection = await _dbContext.CourseSections.FindAsync(sectionDto.CourseSectionId);

            if (courseSection == null)
                throw new KeyNotFoundException($"Course Section with ID {sectionDto.CourseSectionId} not found");

            _mapper.Map(sectionDto, courseSection);
            await _dbContext.SaveChangesAsync();

            // 重新加载关联数据
            await _dbContext.Entry(courseSection)
                .Reference(cs => cs.Course)
                .LoadAsync();
            await _dbContext.Entry(courseSection)
                .Reference(cs => cs.Semester)
                .LoadAsync();

            return _mapper.Map<CourseSectionDto>(courseSection);
        }

        public async Task<bool> DeleteCourseSectionAsync(int courseSectionId)
        {
            var courseSection = await _dbContext.CourseSections.FindAsync(courseSectionId);

            if (courseSection == null)
                return false;

            _dbContext.CourseSections.Remove(courseSection);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}