using SmartSchedulingSystem.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.Interfaces
{
    public interface ISemesterService
    {
        Task<List<SemesterDto>> GetAllSemestersAsync();
        Task<SemesterDto> GetSemesterByIdAsync(int semesterId);
        Task<SemesterDto> CreateSemesterAsync(SemesterDto semesterDto);
        Task<SemesterDto> UpdateSemesterAsync(SemesterDto semesterDto);
        Task<bool> DeleteSemesterAsync(int semesterId);
    }
}
