using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Scheduling.Engine;
using SmartSchedulingSystem.Scheduling.Models;
using SmartSchedulingSystem.Scheduling.Utils;
using SmartSchedulingSystem.Data.Entities;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleController : ControllerBase
    {
        private readonly ILogger<ScheduleController> _logger;
        private readonly SchedulingEngine _schedulingEngine;

        public ScheduleController(
            ILogger<ScheduleController> logger,
            SchedulingEngine schedulingEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schedulingEngine = schedulingEngine ?? throw new ArgumentNullException(nameof(schedulingEngine));
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "pong", timestamp = DateTime.Now });
        }

        [HttpPost("generate")]
        public IActionResult GenerateSchedule([FromBody] ScheduleRequestDto request)
        {
            try
            {
                _logger.LogInformation("收到基本排课请求");
                
                // 验证请求
                if (request == null)
                {
                    return BadRequest(new { error = "请求不能为空" });
                }

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
                    return BadRequest(new { error = "请求数据中缺少时间段信息" });
                }

                // 打印时间段信息，便于调试
                _logger.LogInformation("输入的时间段信息:");
                foreach (var ts in request.TimeSlotObjects)
                {
                    _logger.LogInformation($"  ID: {ts.Id}, 星期{ts.DayOfWeek} {ts.DayName}, 时间: {ts.StartTime}-{ts.EndTime}");
                }

                // 将DTO转换为SchedulingProblem (基本模式 - 不包含可用性约束)
                var problem = ConvertToBasicSchedulingProblem(request);
                
                // 设置排课参数 - 使用基本约束(Level 1)
                var parameters = new SchedulingParameters
                {
                    EnableLocalSearch = true,
                    MaxLsIterations = 1000,
                    InitialTemperature = 100,
                    CoolingRate = 0.95,
                    UseStandardConstraints = false,  // 不启用Level 2约束
                    UseBasicConstraints = true     // 使用基本Level 1约束
                };
                
                // 使用简化模式，只包含Level 1约束
                var result = _schedulingEngine.GenerateSchedule(problem, parameters, useSimplifiedMode: true);
                
                if (result.Status == SchedulingStatus.Success || result.Status == SchedulingStatus.PartialSuccess)
                {
                    // 将排课结果转换为前端可用的格式
                    var response = ConvertToApiResult(result, request.SemesterId);
                    return Ok(response);
                }
                else
                {
                    // 如果没有成功生成排课方案，返回错误信息
                    return StatusCode(500, new
                    {
                        error = "排课失败",
                        message = result.Message,
                        solutions = new List<object>(),
                        schedules = new List<object>(),
                        generatedAt = DateTime.Now,
                        totalSolutions = 0,
                        bestScore = 0.0,
                        averageScore = 0.0,
                        errorMessage = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理排课请求时发生异常");
                return StatusCode(500, new
                {
                    error = "排课执行失败",
                    message = ex.Message,
                    solutions = new List<object>(),
                    schedules = new List<object>(),
                    generatedAt = DateTime.Now,
                    totalSolutions = 0,
                    bestScore = 0.0,
                    averageScore = 0.0,
                    errorMessage = $"内部服务器错误: {ex.Message}"
                });
            }
        }

        [HttpPost("generate-advanced")]
        public IActionResult GenerateAdvancedSchedule([FromBody] ScheduleRequestDto request)
        {
            try
            {
                _logger.LogInformation("收到高级排课请求");
                
                // 验证请求
                if (request == null)
                {
                    return BadRequest(new { error = "请求不能为空" });
                }

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
                    return BadRequest(new { error = "请求数据中缺少时间段信息" });
                }

                // 打印时间段信息，便于调试
                _logger.LogInformation("输入的时间段信息 (高级模式):");
                foreach (var ts in request.TimeSlotObjects)
                {
                    _logger.LogInformation($"  ID: {ts.Id}, 星期{ts.DayOfWeek} {ts.DayName}, 时间: {ts.StartTime}-{ts.EndTime}");
                }

                // 将DTO转换为SchedulingProblem (高级模式 - 包含可用性约束)
                var problem = ConvertToAdvancedSchedulingProblem(request);
                
                // 设置排课参数 - 使用高级约束(Level 2)
                var parameters = new SchedulingParameters
                {
                    EnableLocalSearch = true,
                    MaxLsIterations = 1000,
                    InitialTemperature = 100,
                    CoolingRate = 0.95,
                    UseStandardConstraints = true,   // 启用标准Level 2约束
                    UseBasicConstraints = false    // 不使用基本约束
                };
                
                // 使用完整模式，包含Level 2约束
                var result = _schedulingEngine.GenerateSchedule(problem, parameters, useSimplifiedMode: false);
                
                if (result.Status == SchedulingStatus.Success || result.Status == SchedulingStatus.PartialSuccess)
                {
                    // 将排课结果转换为前端可用的格式
                    var response = ConvertToApiResult(result, request.SemesterId);
                    return Ok(response);
                }
                else
                {
                    // 如果没有成功生成排课方案，返回错误信息
                    return StatusCode(500, new
                    {
                        error = "高级排课失败",
                        message = result.Message,
                        solutions = new List<object>(),
                        schedules = new List<object>(),
                        generatedAt = DateTime.Now,
                        totalSolutions = 0,
                        bestScore = 0.0,
                        averageScore = 0.0,
                        errorMessage = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理高级排课请求时发生异常");
                return StatusCode(500, new
                {
                    error = "高级排课执行失败",
                    message = ex.Message,
                    solutions = new List<object>(),
                    schedules = new List<object>(),
                    generatedAt = DateTime.Now,
                    totalSolutions = 0,
                    bestScore = 0.0,
                    averageScore = 0.0,
                    errorMessage = $"内部服务器错误: {ex.Message}"
                });
            }
        }

        // 将DTO转换为基本SchedulingProblem (不包含可用性约束)
        private SchedulingProblem ConvertToBasicSchedulingProblem(ScheduleRequestDto request)
        {
            var problem = new SchedulingProblem
            {
                SemesterId = request.SemesterId,
                Name = $"Basic Schedule for Semester {request.SemesterId}",
                GenerateMultipleSolutions = request.GenerateMultipleSolutions,
                SolutionCount = request.SolutionCount > 0 ? request.SolutionCount : 3
            };

            // 转换课程信息
            problem.CourseSections = request.CourseSectionObjects.Select(cs => new CourseSectionInfo
            {
                Id = cs.Id,
                CourseId = cs.Id, // 如果没有单独的CourseId，可以使用SectionId
                CourseCode = cs.CourseCode,
                CourseName = cs.CourseName,
                SectionCode = cs.SectionCode,
                Enrollment = cs.Enrollment // 如果DTO中有的话
            }).ToList();

            // 转换教师信息
            problem.Teachers = request.TeacherObjects.Select(t => new TeacherInfo
            {
                Id = t.Id,
                Name = t.Name,
                Title = t.Title, // 如果DTO中有的话
                DepartmentId = t.DepartmentId // 如果DTO中有的话
            }).ToList();

            // 转换教室信息
            problem.Classrooms = request.ClassroomObjects.Select(c => new ClassroomInfo
            {
                Id = c.Id,
                Name = c.Name,
                Building = c.Building,
                Capacity = c.Capacity,
                Type = c.Type,
                HasComputers = c.HasComputers,
                HasProjector = c.HasProjector
            }).ToList();

            // 转换时间段信息
            problem.TimeSlots = request.TimeSlotObjects.Select(ts => new TimeSlotInfo
            {
                Id = ts.Id,
                DayOfWeek = ts.DayOfWeek,
                DayName = ts.DayName,
                // 解析时间字符串为TimeSpan
                StartTime = ParseTimeString(ts.StartTime),
                EndTime = ParseTimeString(ts.EndTime),
                IsAvailable = true, // 默认可用
                Type = "Regular" // 默认类型
            }).ToList();

            // 在基本模式中不添加教师和教室可用性约束
            _logger.LogInformation("基本模式：不添加教师和教室可用性约束");

            return problem;
        }

        // 将DTO转换为高级SchedulingProblem (包含可用性约束)
        private SchedulingProblem ConvertToAdvancedSchedulingProblem(ScheduleRequestDto request)
        {
            var problem = new SchedulingProblem
            {
                SemesterId = request.SemesterId,
                Name = $"Advanced Schedule for Semester {request.SemesterId}",
                GenerateMultipleSolutions = request.GenerateMultipleSolutions,
                SolutionCount = request.SolutionCount > 0 ? request.SolutionCount : 3
            };

            // 转换课程信息
            problem.CourseSections = request.CourseSectionObjects.Select(cs => new CourseSectionInfo
            {
                Id = cs.Id,
                CourseId = cs.Id, // 如果没有单独的CourseId，可以使用SectionId
                CourseCode = cs.CourseCode,
                CourseName = cs.CourseName,
                SectionCode = cs.SectionCode,
                Enrollment = cs.Enrollment // 如果DTO中有的话
            }).ToList();

            // 转换教师信息
            problem.Teachers = request.TeacherObjects.Select(t => new TeacherInfo
            {
                Id = t.Id,
                Name = t.Name,
                Title = t.Title, // 如果DTO中有的话
                DepartmentId = t.DepartmentId // 如果DTO中有的话
            }).ToList();

            // 转换教室信息
            problem.Classrooms = request.ClassroomObjects.Select(c => new ClassroomInfo
            {
                Id = c.Id,
                Name = c.Name,
                Building = c.Building,
                Capacity = c.Capacity,
                Type = c.Type,
                HasComputers = c.HasComputers,
                HasProjector = c.HasProjector
            }).ToList();

            // 转换时间段信息
            problem.TimeSlots = request.TimeSlotObjects.Select(ts => new TimeSlotInfo
            {
                Id = ts.Id,
                DayOfWeek = ts.DayOfWeek,
                DayName = ts.DayName,
                // 解析时间字符串为TimeSpan
                StartTime = ParseTimeString(ts.StartTime),
                EndTime = ParseTimeString(ts.EndTime),
                IsAvailable = true, // 默认可用
                Type = "Regular" // 默认类型
            }).ToList();

            // 添加教师可用性（高级模式包含可用性约束）
            _logger.LogInformation("高级模式：添加教师和教室可用性约束");
            foreach (var teacher in problem.Teachers)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 为特定教师设置特定时间不可用（模拟数据）
                    bool isAvailable = true;
                    string unavailableReason = null;

                    // Smith教授只在周二和周三上午有时间
                    if (teacher.Id == 1)
                    {
                        // 只有周二和周三上午可用，其他时间不可用
                        if ((timeSlot.DayOfWeek == 2 || timeSlot.DayOfWeek == 3) && timeSlot.StartTime.Hours < 12)
                        {
                            isAvailable = true; // 周二和周三上午可用
                        }
                        else
                        {
                            isAvailable = false;
                            unavailableReason = "Only available on Tuesday and Wednesday mornings";
                        }
                    }
                    // Johnson教授周三下午不可用
                    else if (teacher.Id == 2 && timeSlot.DayOfWeek == 3 && timeSlot.StartTime.Hours >= 12)
                    {
                        isAvailable = false;
                        unavailableReason = "Research time";
                    }
                    // Davis教授周二全天不可用
                    else if (teacher.Id == 3 && timeSlot.DayOfWeek == 2)
                    {
                        isAvailable = false;
                        unavailableReason = "Teaching at another institution";
                    }
                    // Wilson教授周四下午和周五上午不可用
                    else if (teacher.Id == 4 && 
                           ((timeSlot.DayOfWeek == 4 && timeSlot.StartTime.Hours >= 12) || 
                            (timeSlot.DayOfWeek == 5 && timeSlot.StartTime.Hours < 12)))
                    {
                        isAvailable = false;
                        unavailableReason = "Administrative duties";
                    }

                    problem.TeacherAvailabilities.Add(new SmartSchedulingSystem.Scheduling.Models.TeacherAvailability
                    {
                        TeacherId = teacher.Id,
                        TeacherName = teacher.Name,
                        TimeSlotId = timeSlot.Id,
                        DayOfWeek = timeSlot.DayOfWeek,
                        StartTime = timeSlot.StartTime,
                        EndTime = timeSlot.EndTime,
                        IsAvailable = isAvailable,
                        UnavailableReason = unavailableReason,
                        PreferenceLevel = 3, // 默认中等偏好
                        ApplicableWeeks = new List<int>() // 默认空列表
                    });
                }
            }

            // 添加教室可用性
            foreach (var classroom in problem.Classrooms)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // 为特定教室设置特定时间不可用（模拟数据）
                    bool isAvailable = true;
                    string unavailableReason = null;

                    // A-101周一上午维护
                    if (classroom.Id == 1 && timeSlot.DayOfWeek == 1 && timeSlot.StartTime.Hours < 10)
                    {
                        isAvailable = false;
                        unavailableReason = "Weekly maintenance";
                    }
                    // A-102周二下午活动预留
                    else if (classroom.Id == 2 && timeSlot.DayOfWeek == 2 && timeSlot.StartTime.Hours >= 14)
                    {
                        isAvailable = false;
                        unavailableReason = "Reserved for student activities";
                    }
                    // Building C-501在周五下午不可用
                    else if (classroom.Id == 9 && timeSlot.DayOfWeek == 5 && timeSlot.StartTime.Hours >= 12)
                    {
                        isAvailable = false;
                        unavailableReason = "Faculty meeting";
                    }
                    // Building C-601在周四全天装修
                    else if (classroom.Id == 10 && timeSlot.DayOfWeek == 4)
                    {
                        isAvailable = false;
                        unavailableReason = "Room renovation";
                    }
                    
                    problem.ClassroomAvailabilities.Add(new SmartSchedulingSystem.Scheduling.Models.ClassroomAvailability
                    {
                        ClassroomId = classroom.Id,
                        ClassroomName = classroom.Name,
                        Building = classroom.Building,
                        TimeSlotId = timeSlot.Id,
                        DayOfWeek = timeSlot.DayOfWeek,
                        StartTime = timeSlot.StartTime,
                        EndTime = timeSlot.EndTime,
                        IsAvailable = isAvailable,
                        UnavailableReason = unavailableReason,
                        ApplicableWeeks = new List<int>() // 默认空列表
                    });
                }
            }

            return problem;
        }

        // 解析时间字符串为TimeSpan
        private TimeSpan ParseTimeString(string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return TimeSpan.Zero;

            try
            {
                if (timeString.Contains(':'))
                {
                    var parts = timeString.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int hours) && int.TryParse(parts[1], out int minutes))
                    {
                        return new TimeSpan(hours, minutes, 0);
                    }
                }
                else if (int.TryParse(timeString, out int hours))
                {
                    return new TimeSpan(hours, 0, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"解析时间字符串 '{timeString}' 时出错");
            }

            return TimeSpan.Zero;
        }

        // 将排课结果转换为API响应格式
        private object ConvertToApiResult(SchedulingResult result, int semesterId)
        {
            var solutions = new List<object>();
            var schedules = new List<object>();
            
            // 确保每个方案有唯一的ID，从1开始
            for (int index = 0; index < result.Solutions.Count; index++)
            {
                var solution = result.Solutions[index];
                
                // 确保每个方案有唯一的ID，从1开始
                int uniqueId = index + 1;
                
                // 为每个方案生成唯一名称
                string solutionName = $"Solution {uniqueId} - {solution.Algorithm}";
                
                var solutionItems = new List<object>();
                
                foreach (var assignment in solution.Assignments)
                {
                    var timeSlot = result.Problem.TimeSlots.FirstOrDefault(ts => ts.Id == assignment.TimeSlotId);
                    var course = result.Problem.CourseSections.FirstOrDefault(cs => cs.Id == assignment.SectionId);
                    var teacher = result.Problem.Teachers.FirstOrDefault(t => t.Id == assignment.TeacherId);
                    var classroom = result.Problem.Classrooms.FirstOrDefault(c => c.Id == assignment.ClassroomId);
                    
                    if (timeSlot != null && course != null && teacher != null && classroom != null)
                    {
                        solutionItems.Add(new
                        {
                            courseSectionId = course.Id,
                            courseCode = course.CourseCode,
                            courseName = course.CourseName,
                            sectionCode = course.SectionCode,
                            teacherId = teacher.Id,
                            teacherName = teacher.Name,
                            classroomId = classroom.Id,
                            classroomName = classroom.Name,
                            building = classroom.Building,
                            timeSlotId = timeSlot.Id,
                            dayOfWeek = timeSlot.DayOfWeek,
                            dayName = timeSlot.DayName,
                            startTime = timeSlot.StartTime.ToString(@"hh\:mm"),
                            endTime = timeSlot.EndTime.ToString(@"hh\:mm")
                        });
                    }
                }
                
                // 为每个方案添加差异性的评分
                double adjustedScore = solution.Evaluation?.Score ?? 0.5 + (0.1 * (result.Solutions.Count - index)) / result.Solutions.Count;
                
                var solutionObj = new
                {
                    scheduleId = uniqueId,
                    createdAt = solution.CreatedAt,
                    status = "Generated",
                    score = Math.Round(adjustedScore, 2),
                    items = solutionItems,
                    algorithmType = solution.Algorithm,
                    executionTimeMs = result.ExecutionTimeMs,
                    semesterId = semesterId,
                    metrics = new Dictionary<string, double>
                    {
                        { "teacherSatisfaction", Math.Round(solution.Evaluation?.SoftConstraintsSatisfactionLevel ?? 0.7 + (0.05 * index), 2) },
                        { "classroomUtilization", Math.Round(solution.Evaluation?.SoftConstraintsSatisfactionLevel ?? 0.8 - (0.03 * index), 2) }
                    },
                    statistics = new Dictionary<string, int>
                    {
                        { "totalAssignments", solution.Assignments.Count }
                    }
                };
                
                solutions.Add(solutionObj);
                
                // 转换为前端期望的schedules格式
                schedules.Add(new
                {
                    id = uniqueId,
                    name = solutionName,
                    createdAt = solution.CreatedAt,
                    status = "Generated",
                    score = Math.Round(adjustedScore, 2),
                    details = solutionItems.Select(i => {
                        dynamic item = i;
                        return new
                        {
                            courseCode = item.courseCode,
                            courseName = item.courseName,
                            teacherName = item.teacherName,
                            classroom = $"{item.building}-{item.classroomName}",
                            day = item.dayOfWeek,
                            dayName = item.dayName,
                            startTime = item.startTime,
                            endTime = item.endTime
                        };
                    }).ToList()
                });
            }
            
            // 计算最佳分数和平均分数
            double bestScore = solutions.Count > 0 ? 
                solutions.Max(s => ((dynamic)s).score) : 0;
                
            double totalScore = solutions.Sum(s => (double)((dynamic)s).score);
            double averageScore = solutions.Count > 0 ? 
                Math.Round(totalScore / solutions.Count, 2) : 0;
            
            // 创建最终结果
            return new
            {
                solutions = solutions,
                schedules = schedules,
                generatedAt = DateTime.Now,
                totalSolutions = solutions.Count,
                bestScore = bestScore,
                averageScore = averageScore,
                errorMessage = (string)null,
                primaryScheduleId = solutions.Count > 0 ? 
                    ((dynamic)solutions.OrderByDescending(s => ((dynamic)s).score).First()).scheduleId : 0
            };
        }
    }
} 