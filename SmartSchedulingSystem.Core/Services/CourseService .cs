using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Data.Context;
using SmartSchedulingSystem.Data.Entities;

namespace SmartSchedulingSystem.Core.Services
{
    public class CourseService : ICourseService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public CourseService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<CourseDto>> GetAllCoursesAsync()
        {
            var courses = await _dbContext.Courses
                .Include(c => c.Department)
                .ToListAsync();
            return _mapper.Map<List<CourseDto>>(courses);
        }

        public async Task<List<CourseDto>> GetCoursesByDepartmentAsync(int departmentId)
        {
            var courses = await _dbContext.Courses
                .Where(c => c.DepartmentId == departmentId)
                .Include(c => c.Department)
                .ToListAsync();
            return _mapper.Map<List<CourseDto>>(courses);
        }

        public async Task<CourseDto> GetCourseByIdAsync(int courseId)
        {
            var course = await _dbContext.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);
            return _mapper.Map<CourseDto>(course);
        }

        public async Task<CourseDto> CreateCourseAsync(CourseDto courseDto)
        {
            var course = _mapper.Map<Course>(courseDto);
            _dbContext.Courses.Add(course);
            await _dbContext.SaveChangesAsync();

            // 重新加载部门信息
            await _dbContext.Entry(course)
                .Reference(c => c.Department)
                .LoadAsync();

            return _mapper.Map<CourseDto>(course);
        }

        public async Task<CourseDto> UpdateCourseAsync(CourseDto courseDto)
        {
            var course = await _dbContext.Courses.FindAsync(courseDto.CourseId);

            if (course == null)
                throw new KeyNotFoundException($"Course with ID {courseDto.CourseId} not found");

            _mapper.Map(courseDto, course);
            await _dbContext.SaveChangesAsync();

            // 重新加载部门信息
            await _dbContext.Entry(course)
                .Reference(c => c.Department)
                .LoadAsync();

            return _mapper.Map<CourseDto>(course);
        }

        public async Task<bool> DeleteCourseAsync(int courseId)
        {
            var course = await _dbContext.Courses.FindAsync(courseId);

            if (course == null)
                return false;

            _dbContext.Courses.Remove(course);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<List<CoursePrerequisiteDto>> GetCoursePrerequisitesAsync(int courseId)
        {
            // 这里应该实现获取课程先修课的逻辑
            // 需要在数据库中创建 CoursePrerequisite 表
            return new List<CoursePrerequisiteDto>();
        }

        public async Task<List<CourseDto>> GetRecommendedCoursesAsync(int studentId, int semesterId)
        {
            // 推荐课程的逻辑，可以基于学生的专业、已完成课程等
            // 这里只是一个占位实现
            return await GetAllCoursesAsync();
        }
    }
}