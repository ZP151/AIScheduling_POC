using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Data.Context;
using SmartSchedulingSystem.Data.Entities;

namespace SmartSchedulingSystem.Core.Services
{
    public class SchedulingConstraintService : ISchedulingConstraintService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public SchedulingConstraintService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<SchedulingConstraintDto>> GetAllConstraintsAsync()
        {
            var constraints = await _dbContext.SchedulingConstraints.ToListAsync();
            return _mapper.Map<List<SchedulingConstraintDto>>(constraints);
        }

        public async Task<SchedulingConstraintDto> GetConstraintByIdAsync(int constraintId)
        {
            var constraint = await _dbContext.SchedulingConstraints.FindAsync(constraintId);
            return _mapper.Map<SchedulingConstraintDto>(constraint);
        }

        public async Task<SchedulingConstraintDto> CreateConstraintAsync(SchedulingConstraintDto constraintDto)
        {
            var constraint = _mapper.Map<SchedulingConstraint>(constraintDto);
            _dbContext.SchedulingConstraints.Add(constraint);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<SchedulingConstraintDto>(constraint);
        }

        public async Task<SchedulingConstraintDto> UpdateConstraintAsync(SchedulingConstraintDto constraintDto)
        {
            var constraint = await _dbContext.SchedulingConstraints.FindAsync(constraintDto.ConstraintId);

            if (constraint == null)
                throw new KeyNotFoundException($"Constraint with ID {constraintDto.ConstraintId} not found");

            _mapper.Map(constraintDto, constraint);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<SchedulingConstraintDto>(constraint);
        }

        public async Task<bool> DeleteConstraintAsync(int constraintId)
        {
            var constraint = await _dbContext.SchedulingConstraints.FindAsync(constraintId);

            if (constraint == null)
                return false;

            _dbContext.SchedulingConstraints.Remove(constraint);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateConstraintsSettingsAsync(List<ConstraintSettingDto> constraintSettings)
        {
            foreach (var setting in constraintSettings)
            {
                var constraint = await _dbContext.SchedulingConstraints.FindAsync(setting.ConstraintId);

                if (constraint == null)
                    continue;

                // 更新约束的活动状态和权重
                constraint.IsActive = setting.IsActive;
                constraint.Weight = setting.Weight;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}