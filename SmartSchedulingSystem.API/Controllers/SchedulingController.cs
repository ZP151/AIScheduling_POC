using Microsoft.AspNetCore.Mvc;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using System.Text.Json;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchedulingController : ControllerBase
    {
        private readonly ISchedulingService _schedulingService;
        private readonly ISemesterService _semesterService;
        private readonly ITeacherService _teacherService;
        private readonly IClassroomService _classroomService;
        private readonly ITimeSlotService _timeSlotService;
        private readonly ISchedulingConstraintService _constraintService;
        private readonly ICourseService _courseService;
        private readonly ICourseSectionService _courseSectionService;

        private readonly ILogger<SchedulingController> _logger;
        private readonly SchedulingEngine _schedulingEngine;

        public SchedulingController(
            ISchedulingService schedulingService,
            ISemesterService semesterService,
            ITeacherService teacherService,
            IClassroomService classroomService,
            ITimeSlotService timeSlotService,
            ISchedulingConstraintService constraintService,
            ICourseService courseService,
            ICourseSectionService courseSectionService,
            ILogger<SchedulingController> logger,
            SchedulingEngine schedulingEngine)
        {
            _schedulingService = schedulingService;
            _semesterService = semesterService;
            _teacherService = teacherService;
            _classroomService = classroomService;
            _timeSlotService = timeSlotService;
            _constraintService = constraintService;
            _courseService = courseService;
            _courseSectionService = courseSectionService;
            _logger = logger;
            _schedulingEngine = schedulingEngine;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<object>> GenerateSchedule([FromBody] ScheduleRequestDto request)
        {
            try
            {
                // 详细记录请求内容
                _logger.LogInformation("收到排课请求: {@RequestSummary}", new
                {
                    SemesterId = request.SemesterId,
                    CoursesCount = request.CourseSectionIds?.Count ?? 0,
                    TeachersCount = request.TeacherIds?.Count ?? 0,
                    ClassroomsCount = request.ClassroomIds?.Count ?? 0,
                    TimeSlotsCount = request.TimeSlotIds?.Count ?? 0,
                    HasObjectData = new 
                    {
                        Courses = request.CourseSectionObjects?.Count > 0,
                        Teachers = request.TeacherObjects?.Count > 0,
                        Classrooms = request.ClassroomObjects?.Count > 0,
                        TimeSlots = request.TimeSlotObjects?.Count > 0
                    }
                });

                // 记录对象数据详情（如果有）
                if (request.CourseSectionObjects?.Count > 0)
                {
                    _logger.LogInformation("课程对象数据: {@CourseData}", request.CourseSectionObjects.Select(c => new { 
                        Id = c.Id, CourseId = c.CourseId, CourseName = c.CourseName, SectionCode = c.SectionCode 
                    }).ToList());
                }
                
                if (request.TeacherObjects?.Count > 0)
                {
                    _logger.LogInformation("教师对象数据: {@TeacherData}", request.TeacherObjects.Select(t => new { 
                        Id = t.Id, Name = t.Name, Title = t.Title 
                    }).ToList());
                }
                
                if (request.ClassroomObjects?.Count > 0)
                {
                    _logger.LogInformation("教室对象数据: {@ClassroomData}", request.ClassroomObjects.Select(c => new { 
                        Id = c.Id, Name = c.Name, Building = c.Building, Type = c.Type 
                    }).ToList());
                }
                
                if (request.TimeSlotObjects?.Count > 0)
                {
                    _logger.LogInformation("时间槽对象数据: {@TimeSlotData}", request.TimeSlotObjects.Select(t => new { 
                        Id = t.Id, DayOfWeek = t.DayOfWeek, StartTime = t.StartTime, EndTime = t.EndTime 
                    }).ToList());
                }

                // 记录参数设置
                _logger.LogInformation("排课参数设置: {@Parameters}", new
                {
                    GenerateMultipleSolutions = request.GenerateMultipleSolutions,
                    SolutionCount = request.SolutionCount,
                    FacultyWorkloadBalance = request.FacultyWorkloadBalance,
                    StudentScheduleCompactness = request.StudentScheduleCompactness,
                    ClassroomTypeMatchingWeight = request.ClassroomTypeMatchingWeight
                });

                // 原有的代码继续执行...
                _logger.LogInformation("开始生成排课方案");
                var result = await _schedulingService.GenerateScheduleAsync(request);
                
                // 检查结果状态
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("排课方案生成失败: {ErrorMessage}", result.ErrorMessage);
                }
                else
                {
                    _logger.LogInformation("成功生成排课方案: {@ResultSummary}", new
                    {
                        SolutionCount = result.TotalSolutions,
                        BestScore = result.BestScore,
                        AverageScore = result.AverageScore
                    });
                }

                // 向前端返回小写字段名的结果
                var formattedResult = new
                {
                    solutions = result.Solutions.Select(s => new
                    {
                        scheduleId = s.ScheduleId,
                        createdAt = s.CreatedAt,
                        status = s.Status,
                        score = s.Score,
                        items = s.Items.Select(i => new
                        {
                            courseSectionId = i.CourseSectionId,
                            teacherId = i.TeacherId,
                            teacherName = i.TeacherName,
                            classroomId = i.ClassroomId,
                            classroomName = i.ClassroomName,
                            dayOfWeek = i.DayOfWeek,
                            dayName = i.DayName
                        }).ToList()
                    }).ToList(),
                    generatedAt = result.GeneratedAt,
                    totalSolutions = result.TotalSolutions,
                    bestScore = result.BestScore,
                    averageScore = result.AverageScore,
                    errorMessage = result.ErrorMessage,
                    isSuccess = result.IsSuccess
                };

                return Ok(formattedResult);
            }
            catch (Exception ex)
            {
                var innerExceptionMessage = ex.InnerException != null ? $" Inner exception: {ex.InnerException.Message}" : "";
                _logger.LogError(ex, "生成排课时发生未知错误: {Message}.{InnerMessage}", ex.Message, innerExceptionMessage);
                
                // 创建一个不抛出异常的结果
                var errorResult = new
                {
                    solutions = new List<object>(),
                    generatedAt = DateTime.Now,
                    totalSolutions = 0,
                    bestScore = 0.0,
                    averageScore = 0.0,
                    errorMessage = $"服务器内部错误: {ex.Message}{innerExceptionMessage}",
                    isSuccess = false
                };
                
                // 返回200 OK但包含错误信息，而不是500，避免前端处理错误
                return Ok(errorResult);
            }
        }

        [HttpGet("history/{semesterId}")]
        public async Task<ActionResult<List<ScheduleResultDto>>> GetScheduleHistory(
            int semesterId,
            [FromQuery] string status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? courseId = null,
            [FromQuery] int? teacherId = null,
            [FromQuery] double? minScore = null,
            [FromQuery] int? maxItems = null)
        {
            try
            {
                _logger.LogInformation("获取学期 {SemesterId} 的排课历史，带筛选条件", semesterId);

                // 验证学期是否存在
                var semester = await _semesterService.GetSemesterByIdAsync(semesterId);
                if (semester == null)
                {
                    _logger.LogWarning("尝试获取不存在的学期ID {SemesterId} 的排课历史", semesterId);
                    return NotFound(new { message = $"学期ID {semesterId} 不存在" });
                }

                // 调用服务方法，传入全部8个筛选条件
                var history = await _schedulingService.GetScheduleHistoryAsync(
                    semesterId,
                    status,
                    startDate,
                    endDate,
                    courseId,
                    teacherId,
                    minScore,
                    maxItems
                );

                _logger.LogInformation("成功获取到 {Count} 条排课历史记录", history.Count);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取学期 {SemesterId} 的排课历史时发生错误", semesterId);
                return StatusCode(500, new { message = $"获取排课历史时发生错误: {ex.Message}" });
            }
        }

        [HttpGet("{scheduleId}")]
        public async Task<ActionResult<ScheduleResultDto>> GetScheduleById(int scheduleId)
        {
            try
            {
                var schedule = await _schedulingService.GetScheduleByIdAsync(scheduleId);

                if (schedule == null)
                    return NotFound();

                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("publish/{scheduleId}")]
        public async Task<ActionResult> PublishSchedule(int scheduleId)
        {
            try
            {
                var result = await _schedulingService.PublishScheduleAsync(scheduleId);

                if (result)
                    return Ok(new { message = "Schedule published successfully" });

                return NotFound(new { message = "Schedule not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("cancel/{scheduleId}")]
        public async Task<ActionResult> CancelSchedule(int scheduleId)
        {
            try
            {
                var result = await _schedulingService.CancelScheduleAsync(scheduleId);

                if (result)
                    return Ok(new { message = "Schedule cancelled successfully" });

                return NotFound(new { message = "Schedule not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("data")]
        public async Task<ActionResult<object>> GetSchedulingData()
        {
            try
            {
                // 并行获取所有基础数据
                var semesters = await _semesterService.GetAllSemestersAsync();
                var teachers = await _teacherService.GetAllTeachersAsync();
                var classrooms = await _classroomService.GetAllClassroomsAsync();
                var timeSlots = await _timeSlotService.GetAllTimeSlotsAsync();
                var constraints = await _constraintService.GetAllConstraintsAsync();
                var courses = await _courseService.GetAllCoursesAsync();
                var courseSections = await _courseSectionService.GetAllCourseSectionsAsync();

                return Ok(new
                {
                    semesters,
                    teachers,
                    classrooms,
                    timeSlots,
                    constraints,
                    courses,
                    courseSections
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("test-schedule")]
        public ActionResult<object> TestScheduleGeneration()
        {
            try
            {
                _logger.LogInformation("开始测试排课功能");
                
                // 准备测试数据
                var testRequest = new ScheduleRequestDto
                {
                    SemesterId = 1,
                    CourseSectionIds = new List<int> { 1, 2, 3 },
                    TeacherIds = new List<int> { 1, 2, 3 },
                    ClassroomIds = new List<int> { 1, 2, 3 },
                    GenerateMultipleSolutions = true,
                    SolutionCount = 2
                };
                
                // 返回直接构造的测试结果
                var testResult = new 
                {
                    requestData = testRequest,
                    solutions = new List<object>
                    {
                        new 
                        {
                            scheduleId = 1,
                            createdAt = DateTime.Now,
                            status = "Test",
                            score = 0.95,
                            items = new List<object>
                            {
                                new 
                                {
                                    courseSectionId = 1,
                                    courseCode = "CS101",
                                    courseName = "计算机科学导论",
                                    teacherId = 1,
                                    teacherName = "张教授",
                                    classroomId = 1,
                                    classroomName = "101",
                                    building = "主楼",
                                    dayOfWeek = 1,
                                    dayName = "周一",
                                    startTime = "08:00",
                                    endTime = "09:30"
                                },
                                new 
                                {
                                    courseSectionId = 2,
                                    courseCode = "CS102",
                                    courseName = "编程基础",
                                    teacherId = 2,
                                    teacherName = "李教授",
                                    classroomId = 2,
                                    classroomName = "102",
                                    building = "主楼",
                                    dayOfWeek = 2,
                                    dayName = "周二",
                                    startTime = "10:00",
                                    endTime = "11:30"
                                },
                                new 
                                {
                                    courseSectionId = 3,
                                    courseCode = "CS103",
                                    courseName = "数据结构",
                                    teacherId = 3,
                                    teacherName = "王教授",
                                    classroomId = 3,
                                    classroomName = "103",
                                    building = "主楼",
                                    dayOfWeek = 3,
                                    dayName = "周三",
                                    startTime = "14:00",
                                    endTime = "15:30"
                                }
                            }
                        }
                    },
                    generatedAt = DateTime.Now,
                    totalSolutions = 1,
                    bestScore = 0.95,
                    averageScore = 0.95
                };
                
                return Ok(testResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试排课功能时发生错误");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpPost("test-schedule-with-data")]
        public async Task<ActionResult<object>> TestScheduleWithData([FromBody] ScheduleRequestDto request)
        {
            try
            {
                _logger.LogInformation("使用提供的数据测试排课功能: {@Request}", request);
                
                // 验证请求
                if (request == null)
                {
                    return BadRequest(new { error = "请求数据不能为空" });
                }
                
                // 补充默认值
                if (request.CourseSectionIds == null || !request.CourseSectionIds.Any())
                {
                    request.CourseSectionIds = new List<int> { 1, 2, 3 };
                    _logger.LogInformation("使用默认课程ID: {IDs}", request.CourseSectionIds);
                }
                
                if (request.TeacherIds == null || !request.TeacherIds.Any())
                {
                    request.TeacherIds = new List<int> { 1, 2, 3 };
                    _logger.LogInformation("使用默认教师ID: {IDs}", request.TeacherIds);
                }
                
                if (request.ClassroomIds == null || !request.ClassroomIds.Any())
                {
                    request.ClassroomIds = new List<int> { 1, 2, 3 };
                    _logger.LogInformation("使用默认教室ID: {IDs}", request.ClassroomIds);
                }
                
                // 尝试调用实际排课服务
                try
                {
                    _logger.LogInformation("尝试调用实际排课服务");
                    var result = await _schedulingService.GenerateScheduleAsync(request);
                    
                    // 将结果转换为小写字段名
                    var formattedResult = new
                    {
                        solutions = result.Solutions.Select(s => new
                        {
                            scheduleId = s.ScheduleId,
                            createdAt = s.CreatedAt,
                            status = s.Status,
                            score = s.Score,
                            items = s.Items.Select(i => new
                            {
                                courseSectionId = i.CourseSectionId,
                                teacherId = i.TeacherId,
                                teacherName = i.TeacherName,
                                classroomId = i.ClassroomId,
                                classroomName = i.ClassroomName,
                                dayOfWeek = i.DayOfWeek,
                                dayName = i.DayName
                            }).ToList()
                        }).ToList(),
                        generatedAt = result.GeneratedAt,
                        totalSolutions = result.TotalSolutions,
                        bestScore = result.BestScore,
                        averageScore = result.AverageScore,
                        errorMessage = result.ErrorMessage,
                        isSuccess = result.IsSuccess
                    };
                    
                    return Ok(new
                    {
                        message = "成功调用实际排课服务",
                        requestData = request,
                        result = formattedResult
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "调用实际排课服务失败: {Message}", ex.Message);
                    
                    // 实际服务调用失败，返回测试数据
                    var fallbackResult = new
                    {
                        message = $"实际排课服务调用失败: {ex.Message}，返回测试数据",
                        requestData = request,
                        result = new
                        {
                            solutions = new List<object>
                            {
                                new
                                {
                                    scheduleId = 999,
                                    createdAt = DateTime.Now,
                                    status = "Fallback",
                                    score = 0.75,
                                    items = request.CourseSectionIds.Select((courseId, index) => new
                                    {
                                        courseSectionId = courseId,
                                        courseCode = $"COURSE-{courseId}",
                                        courseName = $"课程 {courseId}",
                                        teacherId = request.TeacherIds.Count > index ? request.TeacherIds[index] : 1,
                                        teacherName = $"教师 {(request.TeacherIds.Count > index ? request.TeacherIds[index] : 1)}",
                                        classroomId = request.ClassroomIds.Count > index ? request.ClassroomIds[index] : 1,
                                        classroomName = $"教室 {(request.ClassroomIds.Count > index ? request.ClassroomIds[index] : 1)}",
                                        building = "主教学楼",
                                        dayOfWeek = (index % 5) + 1,
                                        dayName = $"周{new[] { "一", "二", "三", "四", "五" }[index % 5]}",
                                        startTime = "08:00",
                                        endTime = "09:30"
                                    }).ToList()
                                }
                            },
                            generatedAt = DateTime.Now,
                            totalSolutions = 1,
                            bestScore = 0.75,
                            averageScore = 0.75,
                            errorMessage = $"实际排课服务调用失败: {ex.Message}",
                            isSuccess = false
                        }
                    };
                    
                    return Ok(fallbackResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试排课功能时发生错误");
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpGet("direct-test")]
        public async Task<ActionResult<object>> DirectTestScheduling()
        {
            try
            {
                _logger.LogInformation("开始直接测试端点");
                
                // 创建一个简单的测试问题
                var problem = new SchedulingProblem
                {
                    // 添加课程
                    CourseSections = new List<CourseSectionInfo>
                    {
                        new CourseSectionInfo { Id = 1, CourseCode = "CS101-A", CourseName = "计算机导论A", Credits = 3 },
                        new CourseSectionInfo { Id = 2, CourseCode = "CS102-A", CourseName = "编程基础A", Credits = 4 },
                        new CourseSectionInfo { Id = 3, CourseCode = "CS103-A", CourseName = "数据结构A", Credits = 4 },
                        new CourseSectionInfo { Id = 4, CourseCode = "CS104-A", CourseName = "算法设计A", Credits = 3 }
                    },
                    
                    // 添加教师
                    Teachers = new List<TeacherInfo>
                    {
                        new TeacherInfo { Id = 1, Name = "Teacher 1" },
                        new TeacherInfo { Id = 2, Name = "Teacher 2" },
                        new TeacherInfo { Id = 3, Name = "Teacher 3" }
                    },
                    
                    // 添加教室
                    Classrooms = new List<ClassroomInfo>
                    {
                        new ClassroomInfo { Id = 1, Name = "Room 1", Capacity = 40 },
                        new ClassroomInfo { Id = 2, Name = "Room 2", Capacity = 60 },
                        new ClassroomInfo { Id = 3, Name = "Room 3", Capacity = 80 }
                    },
                    
                    // 添加时间槽
                    TimeSlots = new List<TimeSlotInfo>
                    {
                        new TimeSlotInfo { Id = 1, DayOfWeek = 1, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 30, 0) },
                        new TimeSlotInfo { Id = 2, DayOfWeek = 3, StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(15, 30, 0) },
                        new TimeSlotInfo { Id = 3, DayOfWeek = 5, StartTime = new TimeSpan(19, 0, 0), EndTime = new TimeSpan(20, 30, 0) }
                    },
                    
                    // 添加教师课程偏好
                    TeacherCoursePreferences = new List<TeacherCoursePreference>
                    {
                        new TeacherCoursePreference { TeacherId = 1, CourseId = 1, PreferenceLevel = 5 },
                        new TeacherCoursePreference { TeacherId = 1, CourseId = 2, PreferenceLevel = 4 },
                        new TeacherCoursePreference { TeacherId = 2, CourseId = 3, PreferenceLevel = 5 },
                        new TeacherCoursePreference { TeacherId = 2, CourseId = 4, PreferenceLevel = 3 },
                        new TeacherCoursePreference { TeacherId = 3, CourseId = 3, PreferenceLevel = 4 },
                        new TeacherCoursePreference { TeacherId = 3, CourseId = 4, PreferenceLevel = 5 }
                    }
                };

                // 设置算法参数
                var parameters = new SchedulingParameters
                {
                    InitialSolutionCount = 1,
                    CpTimeLimit = 60,
                    MaxLsIterations = 100,
                    EnableParallelOptimization = false
                };

                // 直接调用调度引擎（跳过数据转换）
                var result = _schedulingEngine.GenerateSchedule(problem, parameters);

                // 转换结果为简化形式
                var simplifiedResult = new
                {
                    Status = result.Status.ToString(),
                    Message = result.Message,
                    SolutionCount = result.Solutions.Count,
                    Solutions = result.Solutions.Select(s => new
                    {
                        Score = s.Evaluation?.Score ?? 0.0,
                        AssignmentCount = s.Assignments.Count,
                        Assignments = s.Assignments.Select(a => new
                        {
                            CourseSection = problem.CourseSections.FirstOrDefault(c => c.Id == a.SectionId)?.CourseCode,
                            Teacher = problem.Teachers.FirstOrDefault(t => t.Id == a.TeacherId)?.Name,
                            Classroom = problem.Classrooms.FirstOrDefault(c => c.Id == a.ClassroomId)?.Name,
                            Day = problem.TimeSlots.FirstOrDefault(t => t.Id == a.TimeSlotId)?.DayOfWeek,
                            StartTime = problem.TimeSlots.FirstOrDefault(t => t.Id == a.TimeSlotId)?.StartTime.ToString(@"hh\:mm\:ss"),
                            EndTime = problem.TimeSlots.FirstOrDefault(t => t.Id == a.TimeSlotId)?.EndTime.ToString(@"hh\:mm\:ss")
                        }).ToList()
                    }).ToList()
                };

                return Ok(simplifiedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "直接测试排课时发生错误");
                return StatusCode(500, new { error = "直接测试排课时发生内部错误", details = ex.ToString() });
            }
        }

        [HttpPost("debug-request")]
        public ActionResult<object> DebugRequest([FromBody] ScheduleRequestDto request)
        {
            try
            {
                // 详细记录请求内容
                _logger.LogInformation("收到调试请求: {@RequestSummary}", new
                {
                    SemesterId = request.SemesterId,
                    CourseSectionIds = request.CourseSectionIds,
                    TeacherIds = request.TeacherIds,
                    ClassroomIds = request.ClassroomIds,
                    TimeSlotIds = request.TimeSlotIds,
                    CourseSectionObjects = request.CourseSectionObjects?.Select(c => new { Id = c.Id, Name = c.CourseName }).ToList(),
                    TeacherObjects = request.TeacherObjects?.Select(t => new { Id = t.Id, Name = t.Name }).ToList(),
                    ClassroomObjects = request.ClassroomObjects?.Select(c => new { Id = c.Id, Name = c.Name }).ToList(),
                    TimeSlotObjects = request.TimeSlotObjects?.Select(t => new { Id = t.Id, DayOfWeek = t.DayOfWeek }).ToList()
                });

                // 不处理请求，只返回接收到的数据和简单摘要
                return Ok(new
                {
                    message = "成功接收请求数据",
                    counts = new
                    {
                        courseSections = (request.CourseSectionIds?.Count ?? 0) + (request.CourseSectionObjects?.Count ?? 0),
                        teachers = (request.TeacherIds?.Count ?? 0) + (request.TeacherObjects?.Count ?? 0),
                        classrooms = (request.ClassroomIds?.Count ?? 0) + (request.ClassroomObjects?.Count ?? 0),
                        timeSlots = (request.TimeSlotIds?.Count ?? 0) + (request.TimeSlotObjects?.Count ?? 0)
                    },
                    requestData = request // 返回完整请求数据，以便检查
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理调试请求时出错: {Message}", ex.Message);
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        // 添加一个不依赖数据库的测试方法
        [HttpPost("test-request-data")]
        public ActionResult<object> TestRequestData([FromBody] ScheduleRequestDto request)
        {
            try
            {
                // 只检查请求数据，不调用任何服务
                var summary = new
                {
                    SemesterId = request.SemesterId,
                    DataCounts = new
                    {
                        CourseSections = request.CourseSectionIds?.Count ?? 0,
                        Teachers = request.TeacherIds?.Count ?? 0,
                        Classrooms = request.ClassroomIds?.Count ?? 0,
                        TimeSlots = request.TimeSlotIds?.Count ?? 0,
                        
                        CourseSectionObjects = request.CourseSectionObjects?.Count ?? 0,
                        TeacherObjects = request.TeacherObjects?.Count ?? 0,
                        ClassroomObjects = request.ClassroomObjects?.Count ?? 0,
                        TimeSlotObjects = request.TimeSlotObjects?.Count ?? 0
                    },
                    CourseDetails = request.CourseSectionObjects?.Select(c => new 
                    { 
                        Id = c.Id, 
                        Name = c.CourseName,
                        SectionCode = c.SectionCode
                    }).ToList(),
                    TeacherDetails = request.TeacherObjects?.Select(t => new 
                    { 
                        Id = t.Id, 
                        Name = t.Name 
                    }).ToList(),
                    ClassroomDetails = request.ClassroomObjects?.Select(c => new 
                    { 
                        Id = c.Id, 
                        Name = c.Name 
                    }).ToList(),
                    TimeSlotSample = request.TimeSlotObjects?.Take(3).Select(t => new 
                    { 
                        Id = t.Id, 
                        Day = t.DayName, 
                        Start = t.StartTime, 
                        End = t.EndTime 
                    }).ToList()
                };

                return Ok(new
                {
                    message = "请求数据检查成功",
                    request_summary = summary,
                    data_valid = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    error = "处理请求数据时出错", 
                    message = ex.Message,
                    data_valid = false
                });
            }
        }

        // 添加一个简化的排课测试方法，不依赖数据库
        [HttpPost("test-schedule-generation")]
        public ActionResult<object> TestScheduleGeneration([FromBody] ScheduleRequestDto request)
        {
            try
            {
                _logger.LogInformation("测试排课方法：收到请求数据 {SemesterId}", request.SemesterId);
                
                // 简单验证请求数据
                if (request.CourseSectionObjects == null || !request.CourseSectionObjects.Any())
                {
                    return BadRequest(new { error = "请求数据中缺少课程信息" });
                }
                
                if (request.TeacherObjects == null || !request.TeacherObjects.Any())
                {
                    return BadRequest(new { error = "请求数据中缺少教师信息" });
                }
                
                if (request.ClassroomObjects == null || !request.ClassroomObjects.Any())
                {
                    return BadRequest(new { error = "请求数据中缺少教室信息" });
                }
                
                if (request.TimeSlotObjects == null || !request.TimeSlotObjects.Any())
                {
                    return BadRequest(new { error = "请求数据中缺少时间槽信息" });
                }
                
                // 生成一个简单的模拟排课结果（不实际调用排课算法）
                var solutions = new List<ScheduleResultDto>();
                var random = new Random();
                
                // 生成指定数量的排课方案
                int solutionCount = request.SolutionCount > 0 ? request.SolutionCount : 3;
                
                for (int i = 0; i < solutionCount; i++)
                {
                    var solution = new ScheduleResultDto
                    {
                        ScheduleId = 1000 + i,
                        CreatedAt = DateTime.Now,
                        Status = "Generated",
                        Score = Math.Round(0.7 + random.NextDouble() * 0.3, 2), // 随机生成0.7-1.0之间的分数
                        Items = new List<ScheduleItemDto>(),
                        AlgorithmType = "Test",
                        ExecutionTimeMs = 50 + random.Next(100),
                        SemesterId = request.SemesterId,
                        Metrics = new Dictionary<string, double>
                        {
                            { "TeacherSatisfaction", Math.Round(0.5 + random.NextDouble() * 0.5, 2) },
                            { "ClassroomUtilization", Math.Round(0.6 + random.NextDouble() * 0.4, 2) }
                        },
                        Statistics = new Dictionary<string, int>
                        {
                            { "TotalAssignments", request.CourseSectionObjects.Count }
                        }
                    };
                    
                    // 为每个课程创建一个排课项
                    foreach (var course in request.CourseSectionObjects)
                    {
                        // 随机选择教师、教室和时间槽
                        var teacher = request.TeacherObjects[random.Next(request.TeacherObjects.Count)];
                        var classroom = request.ClassroomObjects[random.Next(request.ClassroomObjects.Count)];
                        var timeSlot = request.TimeSlotObjects[random.Next(request.TimeSlotObjects.Count)];
                        
                        solution.Items.Add(new ScheduleItemDto
                        {
                            CourseSectionId = course.Id,
                            SectionCode = course.SectionCode,
                            TeacherId = teacher.Id,
                            TeacherName = teacher.Name,
                            ClassroomId = classroom.Id,
                            ClassroomName = classroom.Name,
                            TimeSlotId = timeSlot.Id,
                            DayOfWeek = timeSlot.DayOfWeek,
                            DayName = timeSlot.DayName,
                            StartTime = timeSlot.StartTime,
                            EndTime = timeSlot.EndTime
                        });
                    }
                    
                    solutions.Add(solution);
                }
                
                // 创建结果对象
                var result = new ScheduleResultsDto
                {
                    Solutions = solutions,
                    GeneratedAt = DateTime.Now,
                    TotalSolutions = solutions.Count,
                    BestScore = solutions.Max(s => s.Score),
                    AverageScore = Math.Round(solutions.Average(s => s.Score), 2),
                    ErrorMessage = null, // 设置为null表示没有错误
                    PrimaryScheduleId = solutions.OrderByDescending(s => s.Score).First().ScheduleId
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试排课方法执行失败");
                return StatusCode(500, new
                {
                    error = "测试排课方法执行失败",
                    message = ex.Message
                });
            }
        }
    }
}