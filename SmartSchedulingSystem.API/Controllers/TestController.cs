using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SmartSchedulingSystem.Core.DTOs;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        // 不注入任何服务，避免依赖注入问题
        
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "pong", timestamp = DateTime.Now });
        }
        
        [HttpPost("validate-request")]
        public IActionResult ValidateRequest([FromBody] ScheduleRequestDto request)
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
                    HasConstraintSettings = request.ConstraintSettings?.Any() ?? false,
                    GenerateMultipleSolutions = request.GenerateMultipleSolutions,
                    SolutionCount = request.SolutionCount
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
        
        [HttpPost("mock-schedule")]
        public IActionResult MockSchedule([FromBody] ScheduleRequestDto request)
        {
            try
            {
                if (request.CourseSectionObjects == null || !request.CourseSectionObjects.Any())
                {
                    return BadRequest(new { error = "请求数据中缺少课程信息" });
                }
                
                if (request.TeacherObjects == null || !request.TeacherObjects.Any())
                {
                    return BadRequest(new { error = "Teacher information is missing from the request data" });
                }
                
                if (request.ClassroomObjects == null || !request.ClassroomObjects.Any())
                {
                    return BadRequest(new { error = "Classroom information is missing from the request data" });
                }
                
                if (request.TimeSlotObjects == null || !request.TimeSlotObjects.Any())
                {
                    return BadRequest(new { error = "Time slot information is missing from the request data" });
                }
                
                // 生成一个简单的模拟排课结果
                var solutions = new List<object>();
                var random = new Random();
                
                // 生成指定数量的排课方案
                int solutionCount = request.SolutionCount > 0 ? request.SolutionCount : 3;
                
                for (int i = 0; i < solutionCount; i++)
                {
                    var solution = new
                    {
                        scheduleId = 1000 + i,
                        createdAt = DateTime.Now,
                        status = "Generated",
                        score = Math.Round(0.7 + random.NextDouble() * 0.3, 2), // 随机生成0.7-1.0之间的分数
                        items = new List<object>(),
                        algorithmType = "Test",
                        executionTimeMs = 50 + random.Next(100),
                        semesterId = request.SemesterId,
                        metrics = new Dictionary<string, double>
                        {
                            { "teacherSatisfaction", Math.Round(0.5 + random.NextDouble() * 0.5, 2) },
                            { "classroomUtilization", Math.Round(0.6 + random.NextDouble() * 0.4, 2) }
                        },
                        statistics = new Dictionary<string, int>
                        {
                            { "totalAssignments", request.CourseSectionObjects.Count }
                        }
                    };
                    
                    // 创建临时解决方案对象
                    var tempSolution = solution;
                    var items = new List<object>();
                    
                    // 为每个课程创建一个排课项
                    foreach (var course in request.CourseSectionObjects)
                    {
                        // 随机选择教师、教室和时间槽
                        var teacher = request.TeacherObjects[random.Next(request.TeacherObjects.Count)];
                        var classroom = request.ClassroomObjects[random.Next(request.ClassroomObjects.Count)];
                        var timeSlot = request.TimeSlotObjects[random.Next(request.TimeSlotObjects.Count)];
                        
                        // 添加对前端期望的字段格式
                        items.Add(new
                        {
                            courseSectionId = course.Id,
                            courseCode = course.CourseCode,  // 添加courseCode字段
                            courseName = course.CourseName,  // 添加courseName字段
                            sectionCode = course.SectionCode,
                            teacherId = teacher.Id,
                            teacherName = teacher.Name,
                            classroomId = classroom.Id,
                            classroomName = classroom.Name,
                            building = classroom.Building,   // 添加building字段
                            timeSlotId = timeSlot.Id,
                            dayOfWeek = timeSlot.DayOfWeek,
                            dayName = timeSlot.DayName,
                            startTime = timeSlot.StartTime,
                            endTime = timeSlot.EndTime
                        });
                    }
                    
                    // 添加items到解决方案
                    var solutionWithItems = new
                    {
                        scheduleId = tempSolution.scheduleId,
                        createdAt = tempSolution.createdAt,
                        status = tempSolution.status,
                        score = tempSolution.score,
                        items = items,
                        algorithmType = tempSolution.algorithmType,
                        executionTimeMs = tempSolution.executionTimeMs,
                        semesterId = tempSolution.semesterId,
                        metrics = tempSolution.metrics,
                        statistics = tempSolution.statistics
                    };
                    
                    solutions.Add(solutionWithItems);
                }
                
                // 计算最佳分数
                double bestScore = 0;
                foreach (dynamic sol in solutions)
                {
                    if (sol.score > bestScore)
                    {
                        bestScore = sol.score;
                    }
                }
                
                // 计算平均分数
                double totalScore = 0;
                foreach (dynamic sol in solutions)
                {
                    totalScore += sol.score;
                }
                double averageScore = Math.Round(totalScore / solutions.Count, 2);
                
                // 创建结果对象
                var result = new
                {
                    solutions = solutions,
                    schedules = solutions.Select(s => {
                        dynamic solution = s;
                        return new {
                            id = solution.scheduleId,
                            name = $"Schedule {solution.scheduleId}",
                            createdAt = solution.createdAt,
                            status = solution.status,
                            score = solution.score,
                            // 将items转换为前端期望的details格式
                            details = ((System.Collections.Generic.List<object>)solution.items).Select(i => {
                                dynamic item = i;
                                return new {
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
                        };
                    }).ToList(),
                    generatedAt = DateTime.Now,
                    totalSolutions = solutions.Count,
                    bestScore = bestScore,
                    averageScore = averageScore,
                    errorMessage = (string)null,
                    primaryScheduleId = ((dynamic)solutions.OrderByDescending(s => ((dynamic)s).score).First()).scheduleId
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Mock scheduling execution failed",
                    message = ex.Message,
                    solutions = new List<object>(),
                    schedules = new List<object>(), // Empty schedules list
                    generatedAt = DateTime.Now,
                    totalSolutions = 0,
                    bestScore = 0.0,
                    averageScore = 0.0,
                    errorMessage = $"Internal server error: {ex.Message}"
                });
            }
        }
    }
} 