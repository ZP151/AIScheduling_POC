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

                    // 直接从算法结果创建DTO（跳过数据库保存）
                    var solutions = new List<ScheduleResultDto>();
                    int solutionId = 1000; // 起始ID
                    
                    foreach (var solution in algorithmResult.Solutions)
                    {
                        var scheduleResultDto = new ScheduleResultDto
                        {
                            ScheduleId = solutionId++,
                            CreatedAt = DateTime.Now,
                            Status = "Generated", // 标记为生成状态
                            Score = solution.Evaluation?.Score ?? 0,
                            Items = new List<ScheduleItemDto>(),
                            AlgorithmType = "Hybrid",
                            ExecutionTimeMs = algorithmResult.ExecutionTimeMs,
                            SemesterId = request.SemesterId,
                            TotalAssignments = solution.Assignments.Count,
                            Metrics = new Dictionary<string, double>(),
                            Statistics = new Dictionary<string, int>()
                        };
                        
                        // 添加排课项
                        int itemId = 1;
                        foreach (var assignment in solution.Assignments)
                        {
                            // 查找对应的课程、教师、教室、时间槽信息
                            var courseSection = problem.CourseSections.FirstOrDefault(c => c.Id == assignment.SectionId);
                            var teacher = problem.Teachers.FirstOrDefault(t => t.Id == assignment.TeacherId);
                            var classroom = problem.Classrooms.FirstOrDefault(c => c.Id == assignment.ClassroomId);
                            var timeSlot = problem.TimeSlots.FirstOrDefault(t => t.Id == assignment.TimeSlotId);
                            
                            if (courseSection != null && teacher != null && classroom != null && timeSlot != null)
                            {
                                var dayName = GetDayName(timeSlot.DayOfWeek);
                                scheduleResultDto.Items.Add(new ScheduleItemDto
                                {
                                    ScheduleId = scheduleResultDto.ScheduleId, // 添加正确的ScheduleId
                                    CourseSectionId = assignment.SectionId,
                                    SectionCode = courseSection.SectionCode,
                                    TeacherId = assignment.TeacherId,
                                    TeacherName = teacher.Name,
                                    ClassroomId = assignment.ClassroomId,
                                    ClassroomName = classroom.Name,
                                    TimeSlotId = assignment.TimeSlotId,
                                    DayOfWeek = timeSlot.DayOfWeek,
                                    DayName = dayName,
                                  
                                    AssignmentScore = 0 // 暂不计算单项评分
                                });
                            }
                            else
                            {
                                _logger.LogWarning("无法为分配创建DTO，缺少必要数据: SectionId={SectionId}, TeacherId={TeacherId}, ClassroomId={ClassroomId}, TimeSlotId={TimeSlotId}",
                                    assignment.SectionId, assignment.TeacherId, assignment.ClassroomId, assignment.TimeSlotId);
                            }
                            
                            itemId++;
                        }
                        
                        solutions.Add(scheduleResultDto);
                    }
                    
                    // 计算统计数据
                    double bestScore = solutions.Count > 0 ? solutions.Max(s => s.Score) : 0;
                    double avgScore = solutions.Count > 0 ? solutions.Average(s => s.Score) : 0;
                    
                    // 创建结果DTO
                    var resultsDto = new ScheduleResultsDto
                    {
                        Solutions = solutions,
                        GeneratedAt = DateTime.Now,
                        TotalSolutions = solutions.Count,
                        BestScore = bestScore,
                        AverageScore = avgScore,
                        PrimaryScheduleId = solutions.Count > 0 ? solutions.OrderByDescending(s => s.Score).First().ScheduleId : null
                    };

                    _logger.LogInformation("成功生成排课方案，共 {Count} 个方案", solutions.Count);
                    return resultsDto;
                }
                catch (Exception ex)
                {
                    var innerExceptionMessage = ex.InnerException != null ? $" Inner exception: {ex.InnerException.Message}" : "";
                    _logger.LogError(ex, "排课算法执行过程中发生异常: {Message}.{InnerMessage}. StackTrace: {StackTrace}", 
                        ex.Message, innerExceptionMessage, ex.StackTrace);
                    
                    // 返回一个包含错误信息的结果而不是抛出异常
                    return new ScheduleResultsDto
                    {
                        Solutions = new List<ScheduleResultDto>(),
                        GeneratedAt = DateTime.Now,
                        TotalSolutions = 0,
                        BestScore = 0,
                        AverageScore = 0,
                        ErrorMessage = $"排课算法执行出错: {ex.Message}{innerExceptionMessage}"
                    };
                }
            }
            catch (Exception ex)
            {
                var innerExceptionMessage = ex.InnerException != null ? $" Inner exception: {ex.InnerException.Message}" : "";
                _logger.LogError(ex, "生成排课方案时发生异常: {Message}.{InnerMessage}. StackTrace: {StackTrace}", 
                    ex.Message, innerExceptionMessage, ex.StackTrace);
                
                // 返回一个包含错误信息的结果而不是抛出异常
                return new ScheduleResultsDto
                {
                    Solutions = new List<ScheduleResultDto>(),
                    GeneratedAt = DateTime.Now,
                    TotalSolutions = 0,
                    BestScore = 0,
                    AverageScore = 0,
                    ErrorMessage = $"生成排课方案失败: {ex.Message}{innerExceptionMessage}"
                };
            }
        }

        // 辅助方法：获取星期几的名称
        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "周一",
                2 => "周二",
                3 => "周三",
                4 => "周四",
                5 => "周五",
                6 => "周六",
                7 => "周日",
                _ => "未知"
            };
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
                _logger.LogInformation("准备排课问题，请求参数: {@Request}", new
                {
                    SemesterId = request.SemesterId,
                    CourseCount = (request.CourseSectionIds?.Count ?? 0) + (request.Courses?.Count ?? 0) + (request.CourseSectionObjects?.Count ?? 0),
                    TeacherCount = (request.TeacherIds?.Count ?? 0) + (request.Teachers?.Count ?? 0) + (request.TeacherObjects?.Count ?? 0),
                    ClassroomCount = (request.ClassroomIds?.Count ?? 0) + (request.Classrooms?.Count ?? 0) + (request.ClassroomObjects?.Count ?? 0),
                    TimeSlotCount = (request.TimeSlotIds?.Count ?? 0) + (request.TimeSlots?.Count ?? 0) + (request.TimeSlotObjects?.Count ?? 0)
                });
                
                // 检查TimeSlotObjects详细信息
                if (request.TimeSlotObjects != null && request.TimeSlotObjects.Any())
                {
                    _logger.LogInformation("时间槽对象数据: 共 {Count} 个时间槽", request.TimeSlotObjects.Count);
                    _logger.LogDebug("时间槽示例: ID={Id}, 日期={Day}, 开始时间={Start}, 结束时间={End}",
                        request.TimeSlotObjects[0].Id,
                        request.TimeSlotObjects[0].DayName,
                        request.TimeSlotObjects[0].StartTime,
                        request.TimeSlotObjects[0].EndTime);
                }
                
                // 检查ClassroomObjects详细信息
                if (request.ClassroomObjects != null && request.ClassroomObjects.Any())
                {
                    _logger.LogInformation("教室对象数据: 共 {Count} 个教室", request.ClassroomObjects.Count);
                    foreach (var classroom in request.ClassroomObjects)
                    {
                        _logger.LogDebug("教室详情: ID={Id}, 名称={Name}, 类型={Type}, 校区={Campus}",
                            classroom.Id, classroom.Name, classroom.Type, classroom.CampusName);
                    }
                }
                
                // 开发者模式设置 - 确保即使没有数据也能生成排课方案
                bool devMode = true; // 当前默认启用开发者模式
                
                // 检查是否有完整的对象数据
                bool hasObjectData = (request.CourseSectionObjects?.Count > 0 || request.TeacherObjects?.Count > 0 || 
                                     request.ClassroomObjects?.Count > 0 || request.TimeSlotObjects?.Count > 0);
                
                if (hasObjectData)
                {
                    _logger.LogInformation("检测到完整的对象数据，将优先使用对象数据而不是ID");
                }
                
                // 处理课程部分 - 优先使用对象数据
                IEnumerable<int> courseIds = new List<int>();
                if (request.CourseSectionObjects != null && request.CourseSectionObjects.Any())
                {
                    // 不需要获取ID，因为我们有完整对象
                    _logger.LogInformation("使用CourseSectionObjects，找到{Count}个课程", request.CourseSectionObjects.Count);
                }
                else if (request.CourseSectionIds != null && request.CourseSectionIds.Any())
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
                    _logger.LogWarning("未找到任何课程数据");
                }
                
                // 处理教师部分 - 优先使用对象数据
                IEnumerable<int> teacherIds = new List<int>();
                if (request.TeacherObjects != null && request.TeacherObjects.Any())
                {
                    // 不需要获取ID，因为我们有完整对象
                    _logger.LogInformation("使用TeacherObjects，找到{Count}个教师", request.TeacherObjects.Count);
                }
                else if (request.TeacherIds != null && request.TeacherIds.Any())
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
                    _logger.LogWarning("未找到任何教师数据");
                }
                
                // 处理教室部分 - 优先使用对象数据
                IEnumerable<int> classroomIds = new List<int>();
                if (request.ClassroomObjects != null && request.ClassroomObjects.Any())
                {
                    // 不需要获取ID，因为我们有完整对象
                    _logger.LogInformation("使用ClassroomObjects，找到{Count}个教室", request.ClassroomObjects.Count);
                }
                else if (request.ClassroomIds != null && request.ClassroomIds.Any())
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
                    _logger.LogWarning("未找到任何教室数据");
                }
                
                // 处理时间槽 - 优先使用对象数据
                IEnumerable<int> timeSlotIds = new List<int>();
                if (request.TimeSlotObjects != null && request.TimeSlotObjects.Any())
                {
                    // 不需要获取ID，因为我们有完整对象
                    _logger.LogInformation("使用TimeSlotObjects，找到{Count}个时间槽", request.TimeSlotObjects.Count);
                }
                else if (request.TimeSlotIds != null && request.TimeSlotIds.Any())
                {
                    timeSlotIds = request.TimeSlotIds;
                    _logger.LogInformation("使用TimeSlotIds字段, 找到{Count}个时间槽", timeSlotIds.Count());
                }
                else if (request.TimeSlots != null && request.TimeSlots.Any())
                {
                    timeSlotIds = request.TimeSlots;
                    _logger.LogInformation("使用TimeSlots字段, 找到{Count}个时间槽", timeSlotIds.Count());
                }
                
                // 开发者模式：如果没有足够数据，添加一些默认数据
                if (devMode)
                {
                    if (!hasObjectData && !courseIds.Any())
                    {
                        _logger.LogInformation("开发者模式：添加默认课程数据");
                        courseIds = new List<int> { 1, 2, 3 };
                    }
                    
                    if (!hasObjectData && !teacherIds.Any())
                    {
                        _logger.LogInformation("开发者模式：添加默认教师数据");
                        teacherIds = new List<int> { 1, 2, 3 };
                    }
                    
                    if (!hasObjectData && !classroomIds.Any())
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
                
                // 填充课程数据 - 优先使用对象数据
                problem.CourseSections = new List<CourseSectionInfo>();
                if (request.CourseSectionObjects != null && request.CourseSectionObjects.Any())
                {
                    // 使用完整对象数据
                    foreach (var courseObj in request.CourseSectionObjects)
                    {
                        problem.CourseSections.Add(new CourseSectionInfo
                        {
                            Id = courseObj.Id,
                            CourseId = courseObj.CourseId,
                            CourseCode = courseObj.CourseCode,
                            CourseName = courseObj.CourseName,
                            Credits = courseObj.Credits,
                            SessionsPerWeek = courseObj.SessionsPerWeek,
                            HoursPerSession = courseObj.HoursPerSession,
                            Enrollment = courseObj.Enrollment,
                            DepartmentId = courseObj.DepartmentId
                        });
                    }
                }
                else
                {
                    // 使用ID，创建简单的对象
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
                }
                
                // 填充教师数据 - 优先使用对象数据
                problem.Teachers = new List<TeacherInfo>();
                if (request.TeacherObjects != null && request.TeacherObjects.Any())
                {
                    // 使用完整对象数据
                    foreach (var teacherObj in request.TeacherObjects)
                    {
                        problem.Teachers.Add(new TeacherInfo
                        {
                            Id = teacherObj.Id,
                            Name = teacherObj.Name,
                            Title = teacherObj.Title,
                            DepartmentId = teacherObj.DepartmentId,
                            MaxWeeklyHours = teacherObj.MaxWeeklyHours,
                            MaxDailyHours = teacherObj.MaxDailyHours,
                            MaxConsecutiveHours = teacherObj.MaxConsecutiveHours
                        });
                    }
                }
                else
                {
                    // 使用ID，创建简单的对象
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
                            MaxConsecutiveHours = 4
                        });
                    }
                }
                
                // 填充教室数据 - 优先使用对象数据
                problem.Classrooms = new List<ClassroomInfo>();
                if (request.ClassroomObjects != null && request.ClassroomObjects.Any())
                {
                    // 使用完整对象数据
                    foreach (var classroomObj in request.ClassroomObjects)
                    {
                        problem.Classrooms.Add(new ClassroomInfo
                        {
                            Id = classroomObj.Id,
                            Name = classroomObj.Name,
                            Building = classroomObj.Building,
                            Capacity = classroomObj.Capacity,
                            CampusId = classroomObj.CampusId,
                            CampusName = classroomObj.CampusName,
                            Type = classroomObj.Type,
                            HasComputers = classroomObj.HasComputers,
                            HasProjector = classroomObj.HasProjector
                        });
                    }
                }
                else
                {
                    // 使用ID，创建简单的对象
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
                            Type = "普通教室",
                            HasComputers = false,
                            HasProjector = true
                        });
                    }
                }
                
                // 填充时间槽数据 - 优先使用对象数据
                problem.TimeSlots = new List<TimeSlotInfo>();
                if (request.TimeSlotObjects != null && request.TimeSlotObjects.Any())
                {
                    // 使用完整对象数据
                    foreach (var timeSlotObj in request.TimeSlotObjects)
                    {
                        // 解析时间字符串
                        TimeSpan startTime, endTime;
                        if (!TimeSpan.TryParse(timeSlotObj.StartTime, out startTime))
                        {
                            _logger.LogWarning("无法解析时间字符串 '{StartTime}'，使用默认值", timeSlotObj.StartTime);
                            startTime = new TimeSpan(8, 0, 0); // 默认早上8点
                        }
                        
                        if (!TimeSpan.TryParse(timeSlotObj.EndTime, out endTime))
                        {
                            _logger.LogWarning("无法解析时间字符串 '{EndTime}'，使用默认值", timeSlotObj.EndTime);
                            endTime = new TimeSpan(9, 30, 0); // 默认早上9:30
                        }
                        
                        // 添加时间槽对象到问题中
                        problem.TimeSlots.Add(new TimeSlotInfo
                        {
                            Id = timeSlotObj.Id,
                            DayOfWeek = timeSlotObj.DayOfWeek,
                            DayName = timeSlotObj.DayName,
                            StartTime = startTime,
                            EndTime = endTime
                        });
                        
                        _logger.LogDebug("添加时间槽: ID={Id}, 日期={Day}, 开始时间={Start}, 结束时间={End}",
                            timeSlotObj.Id, timeSlotObj.DayName, startTime, endTime);
                    }
                    
                    _logger.LogInformation("从前端对象添加了 {Count} 个时间槽", request.TimeSlotObjects.Count);
                }
                else if (timeSlotIds.Any())
                {
                    // 使用ID创建简单对象
                    foreach (var timeSlotId in timeSlotIds)
                    {
                        int day = ((timeSlotId - 1) / 4) + 1; // 简单换算
                        int slot = ((timeSlotId - 1) % 4) + 1;
                        
                        string dayName = day switch
                        {
                            1 => "周一",
                            2 => "周二",
                            3 => "周三",
                            4 => "周四",
                            5 => "周五",
                            _ => $"Day-{day}"
                        };
                        
                        TimeSpan startTime, endTime;
                        switch (slot)
                        {
                            case 1:
                                startTime = new TimeSpan(8, 0, 0);
                                endTime = new TimeSpan(9, 30, 0);
                                break;
                            case 2:
                                startTime = new TimeSpan(10, 0, 0);
                                endTime = new TimeSpan(11, 30, 0);
                                break;
                            case 3:
                                startTime = new TimeSpan(14, 0, 0);
                                endTime = new TimeSpan(15, 30, 0);
                                break;
                            case 4:
                                startTime = new TimeSpan(16, 0, 0);
                                endTime = new TimeSpan(17, 30, 0);
                                break;
                            default:
                                startTime = new TimeSpan(8, 0, 0);
                                endTime = new TimeSpan(9, 30, 0);
                                break;
                        }
                        
                        problem.TimeSlots.Add(new TimeSlotInfo
                        {
                            Id = timeSlotId,
                            DayOfWeek = day,
                            DayName = dayName,
                            StartTime = startTime,
                            EndTime = endTime
                        });
                    }
                }
                else
                {
                    // 如果没有时间槽数据，创建默认时间槽
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
                
                // 确保足够的资源匹配
                int courseCount = problem.CourseSections.Count;
                int teacherCount = problem.Teachers.Count;
                int classroomCount = problem.Classrooms.Count;
                
                // 如果课程数量大于教师数量，添加额外的默认教师
                if (courseCount > teacherCount && devMode)
                {
                    _logger.LogWarning("课程数量({CourseCount})大于教师数量({TeacherCount})，添加额外的默认教师", courseCount, teacherCount);
                    for (int i = teacherCount + 1; i <= courseCount; i++)
                    {
                        var teacher = new TeacherInfo
                        {
                            Id = 1000 + i, // 使用较大ID避免冲突
                            Name = $"默认教师 {i}",
                            Title = "讲师",
                            DepartmentId = 1,
                            MaxWeeklyHours = 20,
                            MaxDailyHours = 8,
                            MaxConsecutiveHours = 4
                        };
                        
                        problem.Teachers.Add(teacher);
                        
                        // 为新教师添加课程偏好和可用性
                        foreach (var course in problem.CourseSections)
                        {
                            problem.TeacherCoursePreferences.Add(new TeacherCoursePreference
                            {
                                TeacherId = teacher.Id,
                                CourseId = course.CourseId,
                                ProficiencyLevel = 5,
                                PreferenceLevel = 5
                            });
                        }
                        
                        foreach (var timeSlot in problem.TimeSlots)
                        {
                            problem.TeacherAvailabilities.Add(new SmartSchedulingSystem.Scheduling.Models.TeacherAvailability
                            {
                                TeacherId = teacher.Id,
                                TimeSlotId = timeSlot.Id,
                                IsAvailable = true,
                                PreferenceLevel = 3
                            });
                        }
                    }
                }
                
                // 如果课程数量大于教室数量，添加额外的默认教室
                if (courseCount > classroomCount && devMode)
                {
                    _logger.LogWarning("课程数量({CourseCount})大于教室数量({ClassroomCount})，添加额外的默认教室", courseCount, classroomCount);
                    for (int i = classroomCount + 1; i <= courseCount; i++)
                    {
                        var classroom = new ClassroomInfo
                        {
                            Id = 1000 + i, // 使用较大ID避免冲突
                            Name = $"默认教室 {i}",
                            Building = "主教学楼",
                            Capacity = 60,
                            CampusId = 1,
                            CampusName = "主校区",
                            Type = "普通教室",
                            HasComputers = false,
                            HasProjector = true
                        };
                        
                        problem.Classrooms.Add(classroom);
                        
                        // 为新教室添加可用性
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
                }
                
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
                        _logger.LogWarning("排课问题验证出现警告，但开发者模式允许继续: {Errors}", string.Join("; ", validationErrors));
                        
                        // 添加额外诊断信息
                        _logger.LogWarning("问题诊断信息:\n" +
                            "课程数量: {CourseCount} (需要>0)\n" +
                            "教师数量: {TeacherCount} (需要>0)\n" +
                            "教室数量: {ClassroomCount} (需要>0)\n" +
                            "时间槽数量: {TimeSlotCount} (需要>0)\n" +
                            "教师课程偏好数量: {PreferenceCount}\n" +
                            "教师可用性数量: {TeacherAvailCount}\n" +
                            "教室可用性数量: {ClassroomAvailCount}",
                            problem.CourseSections.Count,
                            problem.Teachers.Count,
                            problem.Classrooms.Count,
                            problem.TimeSlots.Count,
                            problem.TeacherCoursePreferences.Count,
                            problem.TeacherAvailabilities.Count,
                            problem.ClassroomAvailabilities.Count);
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
                var innerExceptionMessage = ex.InnerException != null ? $" Inner exception: {ex.InnerException.Message}" : "";
                _logger.LogError(ex, "准备排课问题时发生错误: {Message}.{InnerMessage}. StackTrace: {StackTrace}", 
                    ex.Message, innerExceptionMessage, ex.StackTrace);
                
                // 记录请求数据（去除敏感信息）
                _logger.LogError("请求信息: SemesterId={SemesterId}, CourseCount={CourseCount}, TeacherCount={TeacherCount}, ClassroomCount={ClassroomCount}",
                    request.SemesterId,
                    (request.CourseSectionIds?.Count ?? 0) + (request.Courses?.Count ?? 0) + (request.CourseSectionObjects?.Count ?? 0),
                    (request.TeacherIds?.Count ?? 0) + (request.Teachers?.Count ?? 0) + (request.TeacherObjects?.Count ?? 0),
                    (request.ClassroomIds?.Count ?? 0) + (request.Classrooms?.Count ?? 0) + (request.ClassroomObjects?.Count ?? 0));
                
                throw; // 重新抛出异常
            }
        }
        // 实现保存排课解决方案的逻辑

            // 私有辅助方法：保存排课解决方案
        private async Task<ScheduleResult> SaveSolutionAsync(SchedulingResult algorithmResult, int semesterId)
        {
            try
            {
                // 模拟模式（无数据库连接时使用）
                bool simulationMode = true;
                
                _logger.LogInformation("保存排课方案，模拟模式：{SimulationMode}", simulationMode);
                    
                // 创建排课结果实体
                var scheduleResult = new ScheduleResult
                {
                    SemesterId = semesterId,
                    CreatedAt = DateTime.Now,
                    Status = "Draft", // 初始状态为草稿
                    Score = algorithmResult.Evaluation?.Score ?? 0
                };

                if (!simulationMode)
                {
                    // 数据库操作部分 - 仅在非模拟模式下执行
                    try
                    {
                        // 先保存到数据库获取ScheduleId
                        _dbContext.ScheduleResults.Add(scheduleResult);
                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("成功保存排课结果基本信息，ID: {ScheduleId}", scheduleResult.ScheduleId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "数据库保存操作失败，切换到模拟模式");
                        simulationMode = true;
                    }
                }
                
                if (simulationMode)
                {
                    // 模拟模式 - 手动设置ID
                    scheduleResult.ScheduleId = new Random().Next(1000, 9999);
                    _logger.LogInformation("模拟模式：手动设置ScheduleId为 {ScheduleId}", scheduleResult.ScheduleId);
                }

                // 选择最佳解决方案（通常是第一个，或者使用评分最高的）
                var bestSolution = algorithmResult.Solutions?.OrderByDescending(s => s.Evaluation?.Score ?? 0).FirstOrDefault();

                // 如果找到解决方案，添加其所有分配
                if (bestSolution != null && bestSolution.Assignments != null && bestSolution.Assignments.Any())
                {
                    _logger.LogInformation("保存最佳排课方案，包含 {Count} 个排课项", bestSolution.Assignments.Count);
                    
                    var scheduleItems = bestSolution.Assignments.Select(a => new ScheduleItem
                    {
                        ScheduleResultId = scheduleResult.ScheduleId, // 显式设置外键
                        ScheduleItemId = simulationMode ? new Random().Next(10000, 99999) : 0, // 在模拟模式下手动设置ID
                        CourseSectionId = a.SectionId,
                        TeacherId = a.TeacherId,
                        ClassroomId = a.ClassroomId,
                        TimeSlotId = a.TimeSlotId
                    }).ToList();
                    
                    // 添加排课项
                    scheduleResult.Items = scheduleItems;
                    
                    if (!simulationMode)
                    {
                        // 只在非模拟模式下保存到数据库
                        _dbContext.SaveChanges(); 
                        _logger.LogInformation("成功保存 {Count} 个排课项", scheduleItems.Count);
                    }
                    else
                    {
                        _logger.LogInformation("模拟模式：已创建 {Count} 个排课项（不保存到数据库）", scheduleItems.Count);
                    }
                }
                else
                {
                    _logger.LogWarning("算法未返回有效的排课方案，创建一个空的排课结果");
                    scheduleResult.Items = new List<ScheduleItem>();
                }

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