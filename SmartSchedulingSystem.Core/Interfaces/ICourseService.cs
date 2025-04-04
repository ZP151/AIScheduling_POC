using SmartSchedulingSystem.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.Interfaces
{
    public interface ICourseService
    {
        Task<List<CourseDto>> GetAllCoursesAsync();
        Task<List<CourseDto>> GetCoursesByDepartmentAsync(int departmentId);
        Task<CourseDto> GetCourseByIdAsync(int courseId);
        Task<CourseDto> CreateCourseAsync(CourseDto courseDto);
        Task<CourseDto> UpdateCourseAsync(CourseDto courseDto);
        Task<bool> DeleteCourseAsync(int courseId);

        Task<List<CoursePrerequisiteDto>> GetCoursePrerequisitesAsync(int courseId);
        Task<List<CourseDto>> GetRecommendedCoursesAsync(int studentId, int semesterId);
    }
}
