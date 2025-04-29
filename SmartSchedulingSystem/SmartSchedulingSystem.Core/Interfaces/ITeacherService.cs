using SmartSchedulingSystem.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.Interfaces
{
    public interface ITeacherService
    {
        Task<List<TeacherDto>> GetAllTeachersAsync();
        Task<List<TeacherDto>> GetTeachersByDepartmentAsync(int departmentId);
        Task<TeacherDto> GetTeacherByIdAsync(int teacherId);
        Task<TeacherDto> CreateTeacherAsync(TeacherDto teacherDto);
        Task<TeacherDto> UpdateTeacherAsync(TeacherDto teacherDto);
        Task<bool> DeleteTeacherAsync(int teacherId);
        Task<List<TeacherAvailabilityDto>> GetTeacherAvailabilityAsync(int teacherId);
        Task<bool> UpdateTeacherAvailabilityAsync(int teacherId, List<TeacherAvailabilityDto> availabilities);
    }
}
