using SmartSchedulingSystem.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.Interfaces
{
    public interface ICourseSectionService
    {
        Task<List<CourseSectionDto>> GetAllCourseSectionsAsync();
        Task<List<CourseSectionDto>> GetCourseSectionsBySemesterAsync(int semesterId);
        Task<List<CourseSectionDto>> GetCourseSectionsByCourseAsync(int courseId);
        Task<CourseSectionDto> GetCourseSectionByIdAsync(int courseSectionId);
        Task<CourseSectionDto> CreateCourseSectionAsync(CourseSectionDto courseSectionDto);
        Task<CourseSectionDto> UpdateCourseSectionAsync(CourseSectionDto courseSectionDto);
        Task<bool> DeleteCourseSectionAsync(int courseSectionId);
    }
}
