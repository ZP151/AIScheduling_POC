using SmartSchedulingSystem.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Core.Interfaces
{
    public interface ISchedulingConstraintService
    {
        Task<List<SchedulingConstraintDto>> GetAllConstraintsAsync();
        Task<SchedulingConstraintDto> GetConstraintByIdAsync(int constraintId);
        Task<SchedulingConstraintDto> CreateConstraintAsync(SchedulingConstraintDto constraintDto);
        Task<SchedulingConstraintDto> UpdateConstraintAsync(SchedulingConstraintDto constraintDto);
        Task<bool> DeleteConstraintAsync(int constraintId);
        Task<bool> UpdateConstraintsSettingsAsync(List<ConstraintSettingDto> constraintSettings);
    }
}
