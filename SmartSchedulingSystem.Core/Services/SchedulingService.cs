using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Data;
using SmartSchedulingSystem.Data.Entities;
using SmartSchedulingSystem.Data.Context;

namespace SmartSchedulingSystem.Core.Services
{
    public class SchedulingService : ISchedulingService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SchedulingService> _logger;

        public SchedulingService(
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<SchedulingService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }
        
        // Implement interface methods
        public async Task<List<ScheduleResultDto>> GetScheduleHistoryAsync(int semesterId)
        {
            return await GetScheduleHistoryAsync(semesterId, null, null, null, null, null, null, null);
        }
        
        // Get schedule history records, supporting various filtering conditions
        public async Task<List<ScheduleResultDto>> GetScheduleHistoryAsync(
            int semesterId,
            string status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? courseId = null,
            int? teacherId = null,
            double? minScore = null,
            int? maxItems = null)
        {
            try
            {
                _logger.LogInformation("Getting schedule history for semester {SemesterId}", semesterId);

                // Build the base query
                var query = _dbContext.ScheduleResults
                    .Include(sr => sr.Items)
                    .AsQueryable();
                
                // Filter by status (if specified)
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(sr => sr.Status == status);
                }
                
                // Filter by creation date range
                if (startDate.HasValue)
                {
                    query = query.Where(sr => sr.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    // Include the entire end date
                    DateTime endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(sr => sr.CreatedAt <= endOfDay);
                }

                // Filter by course
                if (courseId.HasValue)
                {
                    query = query.Where(sr => sr.Items.Any(item => item.CourseSection.CourseId == courseId.Value));
                }

                // Filter by teacher
                if (teacherId.HasValue)
                {
                    query = query.Where(sr => sr.Items.Any(item => item.TeacherId == teacherId.Value));
                }

                // Filter by score
                if (minScore.HasValue)
                {
                    double minScoreValue = minScore.Value / 100.0; // Convert percentage to 0-1 range
                    query = query.Where(sr => sr.Score >= minScoreValue);
                }
                
                query = query.Where(sr => sr.SemesterId == semesterId);

                // Sort by creation time (descending)
                query = query.OrderByDescending(sr => sr.CreatedAt);

                // Execute the query
                var results = await query.ToListAsync();

                _logger.LogInformation("Found {Count} schedule history records that match the conditions", results.Count);

                // If a maximum number of items is specified, limit the result count
                if (maxItems.HasValue && maxItems.Value > 0 && results.Count > maxItems.Value)
                {
                    results = results.Take(maxItems.Value).ToList();
                }

                // Load related data (teachers, classrooms, time slots)
                foreach (var result in results)
                {
                    // Manually load related entities for schedule items
                    foreach (var item in result.Items)
                    {
                        await _dbContext.Entry(item).Reference(i => i.Teacher).LoadAsync();
                        await _dbContext.Entry(item).Reference(i => i.Classroom).LoadAsync();
                        await _dbContext.Entry(item).Reference(i => i.TimeSlot).LoadAsync();
                    }
                }

                // Map to DTO and return
                return _mapper.Map<List<ScheduleResultDto>>(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule history for semester {SemesterId}", semesterId);
                throw;
            }
        }

        // Generate schedule (simplified version, no actual generation logic)
        public async Task<ScheduleResultsDto> GenerateScheduleAsync(ScheduleRequestDto request)
        {
            try
            {
                _logger.LogInformation("Starting to generate schedule, semester ID: {SemesterId}", request.SemesterId);

                // Since the Scheduling project was removed, this will only return a simulated result
                var result = new ScheduleResultsDto
                {
                    Solutions = new List<ScheduleResultDto>(),
                    GeneratedAt = DateTime.Now,
                    TotalSolutions = 0,
                    BestScore = 0,
                    AverageScore = 0,
                    ErrorMessage = "Scheduling engine is temporarily unavailable"
                };
                // Explicitly set the IsSuccess property
                result.IsSuccess = false;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating schedule");
                
                var result = new ScheduleResultsDto
                {
                    Solutions = new List<ScheduleResultDto>(),
                    GeneratedAt = DateTime.Now,
                    TotalSolutions = 0,
                    BestScore = 0,
                    AverageScore = 0,
                    ErrorMessage = $"Error generating schedule: {ex.Message}"
                };
                // Explicitly set the IsSuccess property
                result.IsSuccess = false;
                return result;
            }
        }

        // Return the date name
        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                7 => "Sunday",
                _ => $"Unknown ({dayOfWeek})"
            };
        }

        // Get the schedule by ID
        public async Task<ScheduleResultDto> GetScheduleByIdAsync(int scheduleId)
        {
            try
            {
                var scheduleResult = await _dbContext.ScheduleResults
                    .Include(sr => sr.Items)
                    .FirstOrDefaultAsync(sr => sr.ScheduleId == scheduleId);

                if (scheduleResult == null)
                {
                    return null;
                }

                // Load related data
                foreach (var item in scheduleResult.Items)
                {
                    await _dbContext.Entry(item).Reference(i => i.Teacher).LoadAsync();
                    await _dbContext.Entry(item).Reference(i => i.Classroom).LoadAsync();
                    await _dbContext.Entry(item).Reference(i => i.TimeSlot).LoadAsync();
                }

                return _mapper.Map<ScheduleResultDto>(scheduleResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule {ScheduleId}", scheduleId);
                throw;
            }
        }

        // Publish the schedule
        public async Task<bool> PublishScheduleAsync(int scheduleId)
        {
            try
            {
                var scheduleResult = await _dbContext.ScheduleResults
                    .FirstOrDefaultAsync(sr => sr.ScheduleId == scheduleId);

                if (scheduleResult == null)
                {
                    return false;
                }

                scheduleResult.Status = "Published";
                scheduleResult.CreatedAt = DateTime.Now;

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing schedule {ScheduleId}", scheduleId);
                return false;
            }
        }

        // Cancel the schedule
        public async Task<bool> CancelScheduleAsync(int scheduleId)
        {
            try
            {
                var scheduleResult = await _dbContext.ScheduleResults
                    .FirstOrDefaultAsync(sr => sr.ScheduleId == scheduleId);

                if (scheduleResult == null)
                {
                    return false;
                }

                scheduleResult.Status = "Cancelled";
                scheduleResult.CreatedAt = DateTime.Now;

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling schedule {ScheduleId}", scheduleId);
                return false;
            }
        }
    }
}