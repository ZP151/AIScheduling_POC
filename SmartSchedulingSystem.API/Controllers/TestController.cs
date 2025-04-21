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
                
                // 打印输入的所有TimeSlotObjects，便于调试
                Console.WriteLine("输入的所有TimeSlotObjects:");
                foreach (var ts in request.TimeSlotObjects)
                {
                    Console.WriteLine($"  ID: {ts.Id}, 星期{ts.DayOfWeek} {ts.DayName}, 时间: {ts.StartTime}-{ts.EndTime}, StartTime类型: {ts.StartTime?.GetType().Name}, 长度: {ts.StartTime?.Length}");
                    if (ts.StartTime != null)
                    {
                        // 检查时间格式和是否包含晚上时间段
                        bool isEveningByStartsWith = ts.StartTime.StartsWith("19") || ts.StartTime.StartsWith("20") || ts.StartTime.StartsWith("21");
                        bool isEveningByHour = false;
                        if (ts.StartTime.Contains(":"))
                        {
                            var hour = ts.StartTime.Split(':')[0];
                            if (int.TryParse(hour, out int hourValue))
                            {
                                isEveningByHour = hourValue >= 19 && hourValue <= 21;
                            }
                        }
                        Console.WriteLine($"    StartsWith检测晚上时间段: {isEveningByStartsWith}, 小时值检测: {isEveningByHour}");
                    }
                }
                
                // 生成一个简单的模拟排课结果
                var solutions = new List<object>();
                var random = new Random();
                
                // 生成指定数量的排课方案
                int solutionCount = request.SolutionCount > 0 ? request.SolutionCount : 3;
                
                // 对所有时间段进行分类，便于均匀分配
                var allTimeSlots = request.TimeSlotObjects.ToList();
                
                // 按时间段类型分组，便于后续输出调试信息
                var morningSlots = allTimeSlots.Where(ts => ts.StartTime != null && 
                    (ts.StartTime.StartsWith("08") || ts.StartTime.StartsWith("09") || 
                     ts.StartTime.StartsWith("10") || ts.StartTime.StartsWith("11"))).ToList();
                     
                var afternoonSlots = allTimeSlots.Where(ts => ts.StartTime != null && 
                    (ts.StartTime.StartsWith("14") || ts.StartTime.StartsWith("15") || 
                     ts.StartTime.StartsWith("16") || ts.StartTime.StartsWith("17"))).ToList();
                     
                var eveningSlots = allTimeSlots.Where(ts => ts.StartTime != null && 
                    (ts.StartTime.StartsWith("19") || ts.StartTime.StartsWith("20") || ts.StartTime.StartsWith("21"))).ToList();
                
                Console.WriteLine($"早上时间段数量: {morningSlots.Count}, 下午时间段数量: {afternoonSlots.Count}, 晚上时间段数量: {eveningSlots.Count}");
                
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
                    
                    // 用于统计课程分配情况
                    var assignmentStats = new Dictionary<string, int>
                    {
                        { "Morning", 0 },
                        { "Afternoon", 0 },
                        { "Evening", 0 }
                    };
                    
                    // 计算每个时间段区间应该分配的课程数量，确保均匀分配
                    int totalCourses = request.CourseSectionObjects.Count;
                    int totalTimeSlots = allTimeSlots.Count;
                    
                    // 如果有可用时间段，进行均匀分配
                    if (totalTimeSlots > 0)
                    {
                        Console.WriteLine($"总课程数: {totalCourses}, 总时间段数: {totalTimeSlots}");
                        
                        // 为每个课程分配时间段
                        for (int courseIndex = 0; courseIndex < totalCourses; courseIndex++)
                        {
                            var course = request.CourseSectionObjects[courseIndex];
                            
                            // 随机选择教师、教室
                            var teacher = request.TeacherObjects[random.Next(request.TeacherObjects.Count)];
                            var classroom = request.ClassroomObjects[random.Next(request.ClassroomObjects.Count)];
                            
                            // 选择时间段 - 使用纯随机分配
                            // 完全随机选择时间段，不再考虑时间段类型的比例
                            string timePeriod;
                            Core.DTOs.TimeSlotExtDto timeSlot;
                            
                            // 从所有可用时间段中随机选择
                            timeSlot = allTimeSlots[random.Next(allTimeSlots.Count)];
                            
                            // 根据选择的时间段确定时间段类型
                            if (timeSlot.StartTime.StartsWith("08") || timeSlot.StartTime.StartsWith("09") || 
                                timeSlot.StartTime.StartsWith("10") || timeSlot.StartTime.StartsWith("11"))
                            {
                                assignmentStats["Morning"]++;
                                timePeriod = "Morning";
                            }
                            else if (timeSlot.StartTime.StartsWith("14") || timeSlot.StartTime.StartsWith("15") || 
                                     timeSlot.StartTime.StartsWith("16") || timeSlot.StartTime.StartsWith("17"))
                            {
                                assignmentStats["Afternoon"]++;
                                timePeriod = "Afternoon";
                            }
                            else if (timeSlot.StartTime.StartsWith("19") || timeSlot.StartTime.StartsWith("20") || timeSlot.StartTime.StartsWith("21"))
                            {
                                assignmentStats["Evening"]++;
                                timePeriod = "Evening";
                            }
                            else
                            {
                                // 其他时间段
                                timePeriod = "Other";
                            }
                            
                            Console.WriteLine($"课程 {course.CourseName} 安排在 {timePeriod} 时间段: {timeSlot.StartTime}-{timeSlot.EndTime}, dayOfWeek:{timeSlot.DayOfWeek}, dayName:{timeSlot.DayName}");
                            
                            // 添加课程分配项
                            items.Add(new
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
                                startTime = timeSlot.StartTime,
                                endTime = timeSlot.EndTime
                            });
                        }
                    }
                    else
                    {
                        // 如果没有可用时间段，输出错误信息
                        Console.WriteLine("错误：没有可用的时间段！");
                    }
                    
                    // 打印课程分配统计
                    Console.WriteLine("课程分配统计:");
                    Console.WriteLine($"  早上: {assignmentStats["Morning"]} 门课程 ({(double)assignmentStats["Morning"] / totalCourses * 100:F1}%)");
                    Console.WriteLine($"  下午: {assignmentStats["Afternoon"]} 门课程 ({(double)assignmentStats["Afternoon"] / totalCourses * 100:F1}%)");
                    Console.WriteLine($"  晚上: {assignmentStats["Evening"]} 门课程 ({(double)assignmentStats["Evening"] / totalCourses * 100:F1}%)");
                    Console.WriteLine($"  总计: {assignmentStats.Values.Sum()} 门课程, 目标: {totalCourses} 门课程");
                    
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
        
        // 添加一个新的API端点，返回包含晚上时间段的测试数据
        [HttpGet("evening-test-data")]
        public IActionResult GetEveningTestData()
        {
            // 创建包含晚上时间段的测试数据
            var timeSlots = new List<Core.DTOs.TimeSlotExtDto>
            {
                // 早上时间段
                new Core.DTOs.TimeSlotExtDto { Id = 1, DayOfWeek = 1, DayName = "Monday", StartTime = "08:00", EndTime = "09:30" },
                new Core.DTOs.TimeSlotExtDto { Id = 2, DayOfWeek = 1, DayName = "Monday", StartTime = "10:00", EndTime = "11:30" },
                
                // 下午时间段
                new Core.DTOs.TimeSlotExtDto { Id = 3, DayOfWeek = 1, DayName = "Monday", StartTime = "14:00", EndTime = "15:30" },
                new Core.DTOs.TimeSlotExtDto { Id = 4, DayOfWeek = 1, DayName = "Monday", StartTime = "16:00", EndTime = "17:30" },
                
                // 晚上时间段 - 添加这些时间段
                new Core.DTOs.TimeSlotExtDto { Id = 21, DayOfWeek = 1, DayName = "Monday", StartTime = "19:00", EndTime = "20:30" },
                new Core.DTOs.TimeSlotExtDto { Id = 22, DayOfWeek = 1, DayName = "Monday", StartTime = "20:00", EndTime = "21:30" }
            };
            
            // 打印调试信息
            Console.WriteLine("生成的测试数据包含晚上时间段:");
            foreach (var ts in timeSlots)
            {
                Console.WriteLine($"  ID: {ts.Id}, 星期{ts.DayOfWeek} {ts.DayName}, 时间: {ts.StartTime}-{ts.EndTime}");
                
                // 检查时间格式
                bool isEveningByStartsWith = ts.StartTime.StartsWith("19") || ts.StartTime.StartsWith("20") || ts.StartTime.StartsWith("21");
                bool isEveningByHour = false;
                if (ts.StartTime.Contains(":"))
                {
                    var hour = ts.StartTime.Split(':')[0];
                    if (int.TryParse(hour, out int hourValue))
                    {
                        isEveningByHour = hourValue >= 19 && hourValue <= 21;
                    }
                }
                Console.WriteLine($"    StartsWith检测晚上时间段: {isEveningByStartsWith}, 小时值检测: {isEveningByHour}");
            }
            
            // 分类统计
            var morningSlots = timeSlots.Where(ts => ts.StartTime != null && 
                (ts.StartTime.StartsWith("08") || ts.StartTime.StartsWith("09") || 
                 ts.StartTime.StartsWith("10") || ts.StartTime.StartsWith("11"))).ToList();
                 
            var afternoonSlots = timeSlots.Where(ts => ts.StartTime != null && 
                (ts.StartTime.StartsWith("14") || ts.StartTime.StartsWith("15") || 
                 ts.StartTime.StartsWith("16") || ts.StartTime.StartsWith("17"))).ToList();
                 
            var eveningSlots = timeSlots.Where(ts => ts.StartTime != null && 
                (ts.StartTime.StartsWith("19") || ts.StartTime.StartsWith("20") || ts.StartTime.StartsWith("21"))).ToList();
            
            Console.WriteLine($"早上时间段数量: {morningSlots.Count}, 下午时间段数量: {afternoonSlots.Count}, 晚上时间段数量: {eveningSlots.Count}");
            
            return Ok(new 
            {
                timeSlots = timeSlots,
                counts = new 
                {
                    morning = morningSlots.Count,
                    afternoon = afternoonSlots.Count,
                    evening = eveningSlots.Count,
                    total = timeSlots.Count
                }
            });
        }
        
        // 添加一个新的API端点，生成包含晚上时间段的测试排课方案
        [HttpGet("evening-schedule-test")]
        public IActionResult GenerateEveningScheduleTest()
        {
            // 创建模拟的排课请求，包含晚上时间段
            var request = new ScheduleRequestDto
            {
                SemesterId = 1,
                GenerateMultipleSolutions = true,
                SolutionCount = 3,
                
                // 课程数据
                CourseSectionObjects = new List<Core.DTOs.CourseSectionExtDto>
                {
                    new Core.DTOs.CourseSectionExtDto { Id = 1, CourseCode = "CS101", CourseName = "Introduction to Computer Science", SectionCode = "A" },
                    new Core.DTOs.CourseSectionExtDto { Id = 2, CourseCode = "CS201", CourseName = "Data Structures", SectionCode = "A" },
                    new Core.DTOs.CourseSectionExtDto { Id = 3, CourseCode = "CS301", CourseName = "Algorithm Design", SectionCode = "A" },
                    new Core.DTOs.CourseSectionExtDto { Id = 4, CourseCode = "CS401", CourseName = "Artificial Intelligence", SectionCode = "A" },
                    new Core.DTOs.CourseSectionExtDto { Id = 5, CourseCode = "CS501", CourseName = "Computer Networks", SectionCode = "A" },
                    new Core.DTOs.CourseSectionExtDto { Id = 6, CourseCode = "CS601", CourseName = "Evening Programming Lab", SectionCode = "A" }
                },
                
                // 教师数据
                TeacherObjects = new List<Core.DTOs.TeacherExtDto>
                {
                    new Core.DTOs.TeacherExtDto { Id = 1, Name = "Prof. Smith" },
                    new Core.DTOs.TeacherExtDto { Id = 2, Name = "Dr. Johnson" },
                    new Core.DTOs.TeacherExtDto { Id = 3, Name = "Prof. Williams" }
                },
                
                // 教室数据
                ClassroomObjects = new List<Core.DTOs.ClassroomExtDto>
                {
                    new Core.DTOs.ClassroomExtDto { Id = 1, Name = "101", Building = "Main Building" },
                    new Core.DTOs.ClassroomExtDto { Id = 2, Name = "201", Building = "Science Building" },
                    new Core.DTOs.ClassroomExtDto { Id = 3, Name = "301", Building = "Computer Building" }
                },
                
                // 时间段数据，包含晚上时间段
                TimeSlotObjects = new List<Core.DTOs.TimeSlotExtDto>
                {
                    // 周一
                    new Core.DTOs.TimeSlotExtDto { Id = 1, DayOfWeek = 1, DayName = "Monday", StartTime = "08:00", EndTime = "09:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 2, DayOfWeek = 1, DayName = "Monday", StartTime = "10:00", EndTime = "11:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 3, DayOfWeek = 1, DayName = "Monday", StartTime = "14:00", EndTime = "15:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 4, DayOfWeek = 1, DayName = "Monday", StartTime = "16:00", EndTime = "17:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 21, DayOfWeek = 1, DayName = "Monday", StartTime = "19:00", EndTime = "20:30" },
                    
                    // 周二
                    new Core.DTOs.TimeSlotExtDto { Id = 5, DayOfWeek = 2, DayName = "Tuesday", StartTime = "08:00", EndTime = "09:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 6, DayOfWeek = 2, DayName = "Tuesday", StartTime = "10:00", EndTime = "11:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 7, DayOfWeek = 2, DayName = "Tuesday", StartTime = "14:00", EndTime = "15:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 8, DayOfWeek = 2, DayName = "Tuesday", StartTime = "16:00", EndTime = "17:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 22, DayOfWeek = 2, DayName = "Tuesday", StartTime = "19:00", EndTime = "20:30" },
                    
                    // 周三
                    new Core.DTOs.TimeSlotExtDto { Id = 9, DayOfWeek = 3, DayName = "Wednesday", StartTime = "08:00", EndTime = "09:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 10, DayOfWeek = 3, DayName = "Wednesday", StartTime = "10:00", EndTime = "11:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 11, DayOfWeek = 3, DayName = "Wednesday", StartTime = "14:00", EndTime = "15:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 12, DayOfWeek = 3, DayName = "Wednesday", StartTime = "16:00", EndTime = "17:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 23, DayOfWeek = 3, DayName = "Wednesday", StartTime = "19:00", EndTime = "20:30" },
                }
            };
            
            // 使用mock-schedule端点的逻辑处理请求
            return MockSchedule(request);
        }
    }
} 