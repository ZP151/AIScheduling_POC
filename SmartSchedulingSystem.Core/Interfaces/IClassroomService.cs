using SmartSchedulingSystem.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.Interfaces
{
    public interface IClassroomService
    {
        Task<List<ClassroomDto>> GetAllClassroomsAsync();
        Task<ClassroomDto> GetClassroomByIdAsync(int classroomId);
        Task<ClassroomDto> CreateClassroomAsync(ClassroomDto classroomDto);
        Task<ClassroomDto> UpdateClassroomAsync(ClassroomDto classroomDto);
        Task<bool> DeleteClassroomAsync(int classroomId);
        Task<List<ClassroomAvailabilityDto>> GetClassroomAvailabilityAsync(int classroomId);
        Task<bool> UpdateClassroomAvailabilityAsync(int classroomId, List<ClassroomAvailabilityDto> availabilities);
    }
}
