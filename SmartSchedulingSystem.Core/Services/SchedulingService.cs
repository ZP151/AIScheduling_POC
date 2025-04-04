using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;
using SmartSchedulingSystem.Data.Entities;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Data.Context;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Algorithms.CP;

namespace SmartSchedulingSystem.Core.Services
{
    public class SchedulingService : ISchedulingService
    {
        private readonly AppDbContext _dbContext;
        private readonly SchedulingEngine _schedulingEngine;
        private readonly IMapper _mapper;
        private readonly ILogger<SchedulingService> _logger;
        private readonly CPScheduler _scheduler;

        public SchedulingService(
            AppDbContext dbContext,
            SchedulingEngine schedulingEngine,
            IMapper mapper,
            ILogger<SchedulingService> logger,
            CPScheduler scheduler)
        {
            _dbContext = dbContext;
            _schedulingEngine = schedulingEngine;
            _mapper = mapper;
            _logger = logger;
            _scheduler = scheduler;
        }
        // 实现接口方法
        public async Task<List<ScheduleResultDto>> GetScheduleHistoryAsync(int semesterId)
        {
            return await GetScheduleHistoryAsync(semesterId, null, null, null, null, null, null, null);
        }
        // 获取排课历史记录，支持各种筛选条件
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
                _logger.LogInformation("获取学期 {SemesterId} 的排课历史记录", semesterId);

                // 构建基础查询
                var query = _dbContext.ScheduleResults
                    .Include(sr => sr.Items)
                    .AsQueryable();
                // 按状态筛选（如果指定）
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(sr => sr.Status == status);
                }
                // 按创建日期范围筛选
                if (startDate.HasValue)
                {
                    query = query.Where(sr => sr.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    // 包含整个结束日期
                    DateTime endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(sr => sr.CreatedAt <= endOfDay);
                }

                // 按课程筛选
                if (courseId.HasValue)
                {
                    query = query.Where(sr => sr.Items.Any(item => item.CourseSection.CourseId == courseId.Value));
                }

                // 按教师筛选
                if (teacherId.HasValue)
                {
                    query = query.Where(sr => sr.Items.Any(item => item.TeacherId == teacherId.Value));
                }

                // 按评分筛选
                if (minScore.HasValue)
                {
                    double minScoreValue = minScore.Value / 100.0; // 转换百分比为0-1范围
                    query = query.Where(sr => sr.Score >= minScoreValue);
                }
                query = query.Where(sr => sr.SemesterId == semesterId); // ✅ 你已添加该字段

                // 排序（按创建时间降序）
                query = query.OrderByDescending(sr => sr.CreatedAt);

                // 执行查询
                var results = await query.ToListAsync();

                _logger.LogInformation("找到 {Count} 条满足条件的排课历史记录", results.Count);

                // 如果指定了最大条目数，限制结果数量
                if (maxItems.HasValue && maxItems.Value > 0 && results.Count > maxItems.Value)
                {
                    results = results.Take(maxItems.Value).ToList();
                }

                // 加载相关的数据（教师、教室、时间段）
                foreach (var result in results)
                {
                    // 手动加载排课项的相关实体
                    foreach (var item in result.Items)
                    {
                        await _dbContext.Entry(item).Reference(i => i.Teacher).LoadAsync();
                        await _dbContext.Entry(item).Reference(i => i.Classroom).LoadAsync();
                        await _dbContext.Entry(item).Reference(i => i.TimeSlot).LoadAsync();
                    }
                }

                // 映射为DTO并返回
                return _mapper.Map<List<ScheduleResultDto>>(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取学期 {SemesterId} 的排课历史记录时发生错误", semesterId);
                throw;
            }
        }

        // 生成排课方案
        public async Task<ScheduleResultDto> GenerateScheduleAsync(ScheduleRequestDto request)
        {
            try
            {
                _logger.LogInformation("开始生成排课方案，学期ID: {SemesterId}", request.SemesterId);


                // 准备排课问题
                var problem = await PrepareProblemAsync(request);

                // 使用排课引擎生成解决方案
                var algorithmResult = _schedulingEngine.GenerateSchedule(problem);

                // 保存解决方案
                var result = await SaveSolutionAsync(algorithmResult, request.SemesterId);

                _logger.LogInformation("成功生成排课方案，ID: {ScheduleId}", result.ScheduleId);

                return _mapper.Map<ScheduleResultDto>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成排课方案时发生错误");
                throw;
            }
        }

        // 根据ID获取排课方案
        public async Task<ScheduleResultDto> GetScheduleByIdAsync(int scheduleId)
        {
            try
            {
                _logger.LogInformation("获取排课方案 {ScheduleId}", scheduleId);

                var schedule = await _dbContext.ScheduleResults
                    .Include(sr => sr.Items)
                        .ThenInclude(item => item.Teacher)
                    .Include(sr => sr.Items)
                        .ThenInclude(item => item.Classroom)
                    .Include(sr => sr.Items)
                        .ThenInclude(item => item.TimeSlot)
                    .FirstOrDefaultAsync(sr => sr.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    _logger.LogWarning("未找到排课方案 {ScheduleId}", scheduleId);
                    return null;
                }

                return _mapper.Map<ScheduleResultDto>(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取排课方案 {ScheduleId} 时发生错误", scheduleId);
                throw;
            }
        }

        // 发布排课方案
        public async Task<bool> PublishScheduleAsync(int scheduleId)
        {
            try
            {
                _logger.LogInformation("发布排课方案 {ScheduleId}", scheduleId);

                var schedule = await _dbContext.ScheduleResults
                    .FirstOrDefaultAsync(sr => sr.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    _logger.LogWarning("未找到排课方案 {ScheduleId}", scheduleId);
                    return false;
                }

                schedule.Status = "Published";
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("排课方案 {ScheduleId} 已发布", scheduleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布排课方案 {ScheduleId} 时发生错误", scheduleId);
                throw;
            }
        }

        // 取消排课方案
        public async Task<bool> CancelScheduleAsync(int scheduleId)
        {
            try
            {
                _logger.LogInformation("取消排课方案 {ScheduleId}", scheduleId);

                var schedule = await _dbContext.ScheduleResults
                    .FirstOrDefaultAsync(sr => sr.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    _logger.LogWarning("未找到排课方案 {ScheduleId}", scheduleId);
                    return false;
                }

                schedule.Status = "Cancelled";
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("排课方案 {ScheduleId} 已取消", scheduleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消排课方案 {ScheduleId} 时发生错误", scheduleId);
                throw;
            }
        }

        // 私有辅助方法：准备排课问题
        private Task<SchedulingProblem> PrepareProblemAsync(ScheduleRequestDto request)
        {
            // 实现准备排课问题的逻辑

            var problem = new SchedulingProblem
            {
                Id = Guid.NewGuid().GetHashCode(),
                Name = $"排课方案 {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                SemesterId = request.SemesterId,
            };

            return Task.FromResult(problem);
        }
        // 实现保存排课解决方案的逻辑

            // 私有辅助方法：保存排课解决方案
        private async Task<ScheduleResult> SaveSolutionAsync(SchedulingResult algorithmResult, int semesterId)
        {
            // 创建排课结果实体
            var scheduleResult = new ScheduleResult
            {
                SemesterId = semesterId,
                CreatedAt = DateTime.Now,
                Status = "Draft", // 初始状态为草稿
                Score = algorithmResult.Evaluation?.Score ?? 0
            };

            // 添加排课明细
            // 选择最佳解决方案（通常是第一个，或者使用评分最高的）
            // 从这里选择从算法结果中取多个解决方案（目前只去取一个）
            var bestSolution = algorithmResult.Solutions
                .OrderByDescending(s => s.Evaluation?.Score ?? 0)
                .FirstOrDefault();

            // 如果找到解决方案，添加其所有分配
            if (bestSolution != null)
            {
                scheduleResult.Items = bestSolution.Assignments.Select(a => new ScheduleItem
                {
                    CourseSectionId = a.SectionId,
                    TeacherId = a.TeacherId,
                    ClassroomId = a.ClassroomId,
                    TimeSlotId = a.TimeSlotId,
                    // 其他属性...
                }).ToList();
            }

            // 保存到数据库
            _dbContext.ScheduleResults.Add(scheduleResult);
            await _dbContext.SaveChangesAsync();

            return scheduleResult;
        }


    }
}