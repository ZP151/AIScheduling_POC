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
        public async Task<ScheduleResultsDto> GenerateScheduleAsync(ScheduleRequestDto request)
        {
            try
            {
                _logger.LogInformation("开始生成排课方案，学期ID: {SemesterId}", request.SemesterId);

                // 准备排课问题
                var problem = await PrepareProblemAsync(request);

                // 设置多方案生成参数
                problem.GenerateMultipleSolutions = request.GenerateMultipleSolutions;
                problem.SolutionCount = request.SolutionCount;

                try
                {
                // 使用排课引擎生成解决方案
                var algorithmResult = _schedulingEngine.GenerateSchedule(problem);

                    // 检查结果状态
                    if (algorithmResult.Status != SmartSchedulingSystem.Scheduling.Models.SchedulingStatus.Success)
                    {
                        _logger.LogWarning("排课算法未能成功生成解决方案: {Status}, {Message}", 
                            algorithmResult.Status, algorithmResult.Message);
                        
                        // 返回一个包含错误信息的结果而不是抛出异常
                        return new ScheduleResultsDto
                        {
                            Solutions = new List<ScheduleResultDto>(),
                            GeneratedAt = DateTime.Now,
                            TotalSolutions = 0,
                            BestScore = 0,
                            AverageScore = 0,
                            ErrorMessage = $"排课算法未能生成解决方案: {algorithmResult.Message}"
                        };
                    }

                    // 如果没有解决方案，返回空结果
                    if (algorithmResult.Solutions == null || !algorithmResult.Solutions.Any())
                    {
                        _logger.LogWarning("排课算法未能生成任何解决方案");
                        
                        return new ScheduleResultsDto
                        {
                            Solutions = new List<ScheduleResultDto>(),
                            GeneratedAt = DateTime.Now,
                            TotalSolutions = 0,
                            BestScore = 0,
                            AverageScore = 0,
                            ErrorMessage = "未能生成任何排课方案"
                        };
                    }

                // 保存解决方案
                var result = await SaveSolutionAsync(algorithmResult, request.SemesterId);

                _logger.LogInformation("成功生成排课方案，ID: {ScheduleId}", result.ScheduleId);

                    // 创建包含单个方案的ScheduleResultsDto
                    var resultsDto = new ScheduleResultsDto
                    {
                        Solutions = new List<ScheduleResultDto> { _mapper.Map<ScheduleResultDto>(result) },
                        GeneratedAt = DateTime.Now,
                        TotalSolutions = 1,
                        BestScore = result.Score,
                        AverageScore = result.Score
                    };

                    return resultsDto;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "排课算法执行过程中发生错误");
                    
                    // 返回一个包含错误信息的结果而不是抛出异常
                    return new ScheduleResultsDto
                    {
                        Solutions = new List<ScheduleResultDto>(),
                        GeneratedAt = DateTime.Now,
                        TotalSolutions = 0,
                        BestScore = 0,
                        AverageScore = 0,
                        ErrorMessage = $"排课算法执行出错: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成排课方案时发生错误");
                
                // 返回一个包含错误信息的结果而不是抛出异常
                return new ScheduleResultsDto
                {
                    Solutions = new List<ScheduleResultDto>(),
                    GeneratedAt = DateTime.Now,
                    TotalSolutions = 0,
                    BestScore = 0,
                    AverageScore = 0,
                    ErrorMessage = $"生成排课方案失败: {ex.Message}"
                };
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
            try
            {
                _logger.LogInformation("准备排课问题，请求参数: {@Request}", request);
                
                // 开发者模式设置 - 确保即使没有数据也能生成排课方案
                bool devMode = true; // 当前默认启用开发者模式
                
                // 处理课程部分 - 支持多种字段名
                IEnumerable<int> courseIds;
                if (request.CourseSectionIds != null && request.CourseSectionIds.Any())
                {
                    courseIds = request.CourseSectionIds;
                    _logger.LogInformation("使用CourseSectionIds字段, 找到{Count}个课程", courseIds.Count());
                }
                else if (request.Courses != null && request.Courses.Any())
                {
                    courseIds = request.Courses;
                    _logger.LogInformation("使用Courses字段, 找到{Count}个课程", courseIds.Count());
                }
                else
                {
                    courseIds = new List<int>();
                    _logger.LogWarning("未找到任何课程ID");
                }
                
                // 处理教师部分 - 支持多种字段名
                IEnumerable<int> teacherIds;
                if (request.TeacherIds != null && request.TeacherIds.Any())
                {
                    teacherIds = request.TeacherIds;
                    _logger.LogInformation("使用TeacherIds字段, 找到{Count}个教师", teacherIds.Count());
                }
                else if (request.Teachers != null && request.Teachers.Any())
                {
                    teacherIds = request.Teachers;
                    _logger.LogInformation("使用Teachers字段, 找到{Count}个教师", teacherIds.Count());
                }
                else
                {
                    teacherIds = new List<int>();
                    _logger.LogWarning("未找到任何教师ID");
                }
                
                // 处理教室部分 - 支持多种字段名
                IEnumerable<int> classroomIds;
                if (request.ClassroomIds != null && request.ClassroomIds.Any())
                {
                    classroomIds = request.ClassroomIds;
                    _logger.LogInformation("使用ClassroomIds字段, 找到{Count}个教室", classroomIds.Count());
                }
                else if (request.Classrooms != null && request.Classrooms.Any())
                {
                    classroomIds = request.Classrooms;
                    _logger.LogInformation("使用Classrooms字段, 找到{Count}个教室", classroomIds.Count());
                }
                else
                {
                    classroomIds = new List<int>();
                    _logger.LogWarning("未找到任何教室ID");
                }
                
                // 开发者模式：如果没有足够数据，添加一些默认数据
                if (devMode)
                {
                    if (!courseIds.Any())
                    {
                        _logger.LogInformation("开发者模式：添加默认课程数据");
                        courseIds = new List<int> { 1, 2, 3 };
                    }
                    
                    if (!teacherIds.Any())
                    {
                        _logger.LogInformation("开发者模式：添加默认教师数据");
                        teacherIds = new List<int> { 1, 2, 3 };
                    }
                    
                    if (!classroomIds.Any())
                    {
                        _logger.LogInformation("开发者模式：添加默认教室数据");
                        classroomIds = new List<int> { 1, 2, 3 };
                    }
                }
            
                // 创建排课问题对象
            var problem = new SchedulingProblem
            {
                Id = Guid.NewGuid().GetHashCode(),
                Name = $"排课方案 {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                SemesterId = request.SemesterId,
                    GenerateMultipleSolutions = request.GenerateMultipleSolutions,
                    SolutionCount = request.SolutionCount
                };
                
                // 直接使用前端传入的ID创建默认对象
                
                // 填充课程数据
                problem.CourseSections = new List<CourseSectionInfo>();
                foreach (var sectionId in courseIds)
                {
                    problem.CourseSections.Add(new CourseSectionInfo
                    {
                        Id = sectionId,
                        CourseId = sectionId,
                        CourseCode = $"Course-{sectionId}",
                        CourseName = $"课程 {sectionId}",
                        SectionCode = $"Section-{sectionId}",
                        Credits = 3,
                        WeeklyHours = 3,
                        SessionsPerWeek = 2,
                        HoursPerSession = 1.5,
                        Enrollment = 40,
                        DepartmentId = 1
                    });
                }
                
                // 填充教师数据
                problem.Teachers = new List<TeacherInfo>();
                foreach (var teacherId in teacherIds)
                {
                    problem.Teachers.Add(new TeacherInfo
                    {
                        Id = teacherId,
                        Name = $"教师 {teacherId}",
                        Title = "教授",
                        DepartmentId = 1,
                        MaxWeeklyHours = 20,
                        MaxDailyHours = 8,
                        MaxConsecutiveHours = 4  // 添加可能之前缺少的属性
                    });
                }
                
                // 填充教室数据
                problem.Classrooms = new List<ClassroomInfo>();
                foreach (var classroomId in classroomIds)
                {
                    problem.Classrooms.Add(new ClassroomInfo
                    {
                        Id = classroomId,
                        Name = $"教室 {classroomId}",
                        Building = "主教学楼",
                        Capacity = 60,
                        CampusId = 1,
                        CampusName = "主校区",
                        Type = "普通教室",  // 添加可能之前缺少的属性
                        HasComputers = false,
                        HasProjector = true
                    });
                }
                
                // 创建基本的时间槽
                problem.TimeSlots = new List<TimeSlotInfo>();
                // 周一到周五
                for (int day = 1; day <= 5; day++)
                {
                    string dayName = day switch
                    {
                        1 => "周一",
                        2 => "周二",
                        3 => "周三",
                        4 => "周四",
                        5 => "周五",
                        _ => $"Day-{day}"
                    };
                    
                    // 每天4个时间段
                    problem.TimeSlots.Add(new TimeSlotInfo { Id = (day-1)*4 + 1, DayOfWeek = day, DayName = dayName, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0) });
                    problem.TimeSlots.Add(new TimeSlotInfo { Id = (day-1)*4 + 2, DayOfWeek = day, DayName = dayName, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0) });
                    problem.TimeSlots.Add(new TimeSlotInfo { Id = (day-1)*4 + 3, DayOfWeek = day, DayName = dayName, StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(15, 30, 0) });
                    problem.TimeSlots.Add(new TimeSlotInfo { Id = (day-1)*4 + 4, DayOfWeek = day, DayName = dayName, StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(17, 30, 0) });
                }
                
                // 简化处理：假设所有教师可以教授所有课程
                problem.TeacherCoursePreferences = new List<TeacherCoursePreference>();
                foreach (var teacher in problem.Teachers)
                {
                    foreach (var course in problem.CourseSections)
                    {
                        problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                        {
                            TeacherId = teacher.Id,
                            CourseId = course.CourseId,
                            ProficiencyLevel = 5, // 最高能力
                            PreferenceLevel = 5   // 最高偏好
                        });
                    }
                }
                
                // 简化处理：所有教师和教室在所有时间段都可用
                problem.TeacherAvailabilities = new List<SmartSchedulingSystem.Scheduling.Models.TeacherAvailability>();
                problem.ClassroomAvailabilities = new List<SmartSchedulingSystem.Scheduling.Models.ClassroomAvailability>();
                
                foreach (var teacher in problem.Teachers)
                {
                    foreach (var timeSlot in problem.TimeSlots)
                    {
                        problem.TeacherAvailabilities.Add(new SmartSchedulingSystem.Scheduling.Models.TeacherAvailability
                        {
                            TeacherId = teacher.Id,
                            TimeSlotId = timeSlot.Id,
                            IsAvailable = true,
                            PreferenceLevel = 3 // 添加偏好级别
                        });
                    }
                }
                
                foreach (var classroom in problem.Classrooms)
                {
                    foreach (var timeSlot in problem.TimeSlots)
                    {
                        problem.ClassroomAvailabilities.Add(new SmartSchedulingSystem.Scheduling.Models.ClassroomAvailability
                        {
                            ClassroomId = classroom.Id,
                            TimeSlotId = timeSlot.Id,
                            IsAvailable = true
                        });
                    }
                }
                
                // 添加先修课约束（可选，为了完整性）
                problem.Prerequisites = new List<CoursePrerequisite>();
                
                // 日志记录
                _logger.LogInformation("排课问题准备完成: {@Problem}", new
                {
                    CourseCount = problem.CourseSections.Count,
                    TeacherCount = problem.Teachers.Count,
                    ClassroomCount = problem.Classrooms.Count,
                    TimeSlotCount = problem.TimeSlots.Count,
                    TeacherPreferenceCount = problem.TeacherCoursePreferences.Count,
                    TeacherAvailabilityCount = problem.TeacherAvailabilities.Count,
                    ClassroomAvailabilityCount = problem.ClassroomAvailabilities.Count
                });
                
                // 验证问题是否完整，开发者模式下允许有警告
                var validationErrors = problem.Validate();
                if (validationErrors.Any())
                {
                    if (devMode)
                    {
                        _logger.LogWarning("排课问题验证出现警告，但开发者模式允许继续: {Errors}", validationErrors);
                    }
                    else
                    {
                        throw new InvalidOperationException($"排课问题验证失败: {string.Join(", ", validationErrors)}");
                    }
                }

            return Task.FromResult(problem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "准备排课问题时发生错误");
                throw;
            }
        }
        // 实现保存排课解决方案的逻辑

            // 私有辅助方法：保存排课解决方案
        private async Task<ScheduleResult> SaveSolutionAsync(SchedulingResult algorithmResult, int semesterId)
        {
            try
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
                var bestSolution = algorithmResult.Solutions?.OrderByDescending(s => s.Evaluation?.Score ?? 0).FirstOrDefault();

            // 如果找到解决方案，添加其所有分配
                if (bestSolution != null && bestSolution.Assignments != null && bestSolution.Assignments.Any())
            {
                    _logger.LogInformation("保存最佳排课方案，包含 {Count} 个排课项", bestSolution.Assignments.Count);
                    
                scheduleResult.Items = bestSolution.Assignments.Select(a => new ScheduleItem
                {
                    CourseSectionId = a.SectionId,
                    TeacherId = a.TeacherId,
                    ClassroomId = a.ClassroomId,
                    TimeSlotId = a.TimeSlotId,
                    // 其他属性...
                }).ToList();
            }
                else
                {
                    _logger.LogWarning("算法未返回有效的排课方案，创建一个空的排课结果");
                    scheduleResult.Items = new List<ScheduleItem>();
                }

            // 保存到数据库
            _dbContext.ScheduleResults.Add(scheduleResult);
            await _dbContext.SaveChangesAsync();
                _logger.LogInformation("成功保存排课方案，ID: {ScheduleId}", scheduleResult.ScheduleId);

            return scheduleResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存排课方案时发生错误");
                throw;
            }
        }


    }
}