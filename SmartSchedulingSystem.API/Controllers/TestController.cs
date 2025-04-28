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
        // Do not inject any services to avoid dependency injection issues
        
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
                // Only check request data, do not call any services
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
                    message = "Request data check successful",
                    request_summary = summary,
                    data_valid = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    error = "Error processing request data", 
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
                    return BadRequest(new { error = "Course information missing in request data" });
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
                
                // Print all input TimeSlotObjects for debugging
                Console.WriteLine("All input TimeSlotObjects:");
                foreach (var ts in request.TimeSlotObjects)
                {
                    Console.WriteLine($"  ID: {ts.Id}, Week {ts.DayOfWeek} {ts.DayName}, Time: {ts.StartTime}-{ts.EndTime}, StartTime type: {ts.StartTime?.GetType().Name}, Length: {ts.StartTime?.Length}");
                    if (ts.StartTime != null)
                    {
                        // Check time format and whether it includes evening time slots
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
                        Console.WriteLine($"    StartsWith evening time slot check: {isEveningByStartsWith}, Hour value check: {isEveningByHour}");
                    }
                }
                
                // Generate a simple mock scheduling result
                var solutions = new List<object>();
                var random = new Random();
                
                // Generate specified number of scheduling solutions
                int solutionCount = request.SolutionCount > 0 ? request.SolutionCount : 3;
                
                // Categorize all time slots for even distribution
                var allTimeSlots = request.TimeSlotObjects.ToList();
                
                // Group by time slot type for subsequent debug output
                var morningSlots = allTimeSlots.Where(ts => ts.StartTime != null && 
                    (ts.StartTime.StartsWith("08") || ts.StartTime.StartsWith("09") || 
                     ts.StartTime.StartsWith("10") || ts.StartTime.StartsWith("11"))).ToList();
                     
                var afternoonSlots = allTimeSlots.Where(ts => ts.StartTime != null && 
                    (ts.StartTime.StartsWith("14") || ts.StartTime.StartsWith("15") || 
                     ts.StartTime.StartsWith("16") || ts.StartTime.StartsWith("17"))).ToList();
                     
                var eveningSlots = allTimeSlots.Where(ts => ts.StartTime != null && 
                    (ts.StartTime.StartsWith("19") || ts.StartTime.StartsWith("20") || ts.StartTime.StartsWith("21"))).ToList();
                
                Console.WriteLine($"Morning time slots: {morningSlots.Count}, Afternoon time slots: {afternoonSlots.Count}, Evening time slots: {eveningSlots.Count}");
                
                for (int i = 0; i < solutionCount; i++)
                {
                    var solution = new
                    {
                        scheduleId = 1000 + i,
                        createdAt = DateTime.Now,
                        status = "Generated",
                        score = Math.Round(0.7 + random.NextDouble() * 0.3, 2), // Randomly generate scores between 0.7-1.0
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
                    
                    // Create temporary solution object
                    var tempSolution = solution;
                    var items = new List<object>();
                    
                    // For statistics on course assignments
                    var assignmentStats = new Dictionary<string, int>
                    {
                        { "Morning", 0 },
                        { "Afternoon", 0 },
                        { "Evening", 0 }
                    };
                    
                    // Calculate how many courses should be assigned to each time slot interval, ensuring even distribution
                    int totalCourses = request.CourseSectionObjects.Count;
                    int totalTimeSlots = allTimeSlots.Count;
                    
                    // If time slots are available, distribute evenly
                    if (totalTimeSlots > 0)
                    {
                        Console.WriteLine($"Total courses: {totalCourses}, Total time slots: {totalTimeSlots}");
                        
                        // Assign time slots for each course
                        for (int courseIndex = 0; courseIndex < totalCourses; courseIndex++)
                        {
                            var course = request.CourseSectionObjects[courseIndex];
                            
                            // Randomly select teachers and classrooms
                            var teacher = request.TeacherObjects[random.Next(request.TeacherObjects.Count)];
                            var classroom = request.ClassroomObjects[random.Next(request.ClassroomObjects.Count)];
                            
                            // Select time slots - using pure random assignment
                            // Completely random time slot selection, no longer considering the ratio of time slot types
                            string timePeriod;
                            Core.DTOs.TimeSlotExtDto timeSlot;
                            
                            // Randomly select from all available time slots
                            timeSlot = allTimeSlots[random.Next(allTimeSlots.Count)];
                            
                            // Determine time slot type based on selected time slot
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
                                // Other time slots
                                timePeriod = "Other";
                            }
                            
                            Console.WriteLine($"Course {course.CourseName} scheduled at {timePeriod} time slot: {timeSlot.StartTime}-{timeSlot.EndTime}, dayOfWeek:{timeSlot.DayOfWeek}, dayName:{timeSlot.DayName}");
                            
                            // Add course assignment item
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
                        // If no time slots are available, output error message
                        Console.WriteLine("Error: No available time slots!");
                    }
                    
                    // Print course assignment statistics
                    Console.WriteLine("Course assignment statistics:");
                    Console.WriteLine($"  Morning: {assignmentStats["Morning"]} courses ({(double)assignmentStats["Morning"] / totalCourses * 100:F1}%)");
                    Console.WriteLine($"  Afternoon: {assignmentStats["Afternoon"]} courses ({(double)assignmentStats["Afternoon"] / totalCourses * 100:F1}%)");
                    Console.WriteLine($"  Evening: {assignmentStats["Evening"]} courses ({(double)assignmentStats["Evening"] / totalCourses * 100:F1}%)");
                    Console.WriteLine($"  Total: {assignmentStats.Values.Sum()} courses, Target: {totalCourses} courses");
                    
                    // Add items to solution
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
                
                // Calculate best score
                double bestScore = 0;
                foreach (dynamic sol in solutions)
                {
                    if (sol.score > bestScore)
                    {
                        bestScore = sol.score;
                    }
                }
                
                // Calculate average score
                double totalScore = 0;
                foreach (dynamic sol in solutions)
                {
                    totalScore += sol.score;
                }
                double averageScore = Math.Round(totalScore / solutions.Count, 2);
                
                // Create result object
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
                            // Convert items to details format expected by frontend
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
        
        // Add a new API endpoint that returns test data with evening time slots
        [HttpGet("evening-test-data")]
        public IActionResult GetEveningTestData()
        {
            // Create test data that includes evening time slots
            var timeSlots = new List<Core.DTOs.TimeSlotExtDto>
            {
                // Morning time slots
                new Core.DTOs.TimeSlotExtDto { Id = 1, DayOfWeek = 1, DayName = "Monday", StartTime = "08:00", EndTime = "09:30" },
                new Core.DTOs.TimeSlotExtDto { Id = 2, DayOfWeek = 1, DayName = "Monday", StartTime = "10:00", EndTime = "11:30" },
                
                // Afternoon time slots
                new Core.DTOs.TimeSlotExtDto { Id = 3, DayOfWeek = 1, DayName = "Monday", StartTime = "14:00", EndTime = "15:30" },
                new Core.DTOs.TimeSlotExtDto { Id = 4, DayOfWeek = 1, DayName = "Monday", StartTime = "16:00", EndTime = "17:30" },
                
                // Evening time slots - add these slots
                new Core.DTOs.TimeSlotExtDto { Id = 21, DayOfWeek = 1, DayName = "Monday", StartTime = "19:00", EndTime = "20:30" },
                new Core.DTOs.TimeSlotExtDto { Id = 22, DayOfWeek = 1, DayName = "Monday", StartTime = "20:00", EndTime = "21:30" }
            };
            
            // Print debug information
            Console.WriteLine("Generated test data includes evening time slots:");
            foreach (var ts in timeSlots)
            {
                Console.WriteLine($"  ID: {ts.Id}, Week {ts.DayOfWeek} {ts.DayName}, Time: {ts.StartTime}-{ts.EndTime}");
                
                // Check time format
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
                Console.WriteLine($"    StartsWith evening time slot check: {isEveningByStartsWith}, Hour value check: {isEveningByHour}");
            }
            
            // Categorized statistics
            var morningSlots = timeSlots.Where(ts => ts.StartTime != null && 
                (ts.StartTime.StartsWith("08") || ts.StartTime.StartsWith("09") || 
                 ts.StartTime.StartsWith("10") || ts.StartTime.StartsWith("11"))).ToList();
                 
            var afternoonSlots = timeSlots.Where(ts => ts.StartTime != null && 
                (ts.StartTime.StartsWith("14") || ts.StartTime.StartsWith("15") || 
                 ts.StartTime.StartsWith("16") || ts.StartTime.StartsWith("17"))).ToList();
                 
            var eveningSlots = timeSlots.Where(ts => ts.StartTime != null && 
                (ts.StartTime.StartsWith("19") || ts.StartTime.StartsWith("20") || ts.StartTime.StartsWith("21"))).ToList();
            
            Console.WriteLine($"Morning time slots: {morningSlots.Count}, Afternoon time slots: {afternoonSlots.Count}, Evening time slots: {eveningSlots.Count}");
            
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
        
        // Add a new API endpoint to generate test scheduling solutions that include evening time slots
        [HttpGet("evening-schedule-test")]
        public IActionResult GenerateEveningScheduleTest()
        {
            // Create simulated scheduling request including evening time slots
            var request = new ScheduleRequestDto
            {
                SemesterId = 1,
                GenerateMultipleSolutions = true,
                SolutionCount = 3,
                
                // Course data
                CourseSectionObjects = new List<Core.DTOs.CourseSectionExtDto>
                {
                    new Core.DTOs.CourseSectionExtDto { Id = 1, CourseCode = "CS101", CourseName = "Introduction to Computer Science", SectionCode = "A" },
                    new Core.DTOs.CourseSectionExtDto { Id = 2, CourseCode = "CS201", CourseName = "Data Structures", SectionCode = "A" },
                    new Core.DTOs.CourseSectionExtDto { Id = 3, CourseCode = "CS301", CourseName = "Algorithm Design", SectionCode = "A" },
                    new Core.DTOs.CourseSectionExtDto { Id = 4, CourseCode = "CS401", CourseName = "Artificial Intelligence", SectionCode = "A" },
                    new Core.DTOs.CourseSectionExtDto { Id = 5, CourseCode = "CS501", CourseName = "Computer Networks", SectionCode = "A" },
                    new Core.DTOs.CourseSectionExtDto { Id = 6, CourseCode = "CS601", CourseName = "Evening Programming Lab", SectionCode = "A" }
                },
                
                // Teacher data
                TeacherObjects = new List<Core.DTOs.TeacherExtDto>
                {
                    new Core.DTOs.TeacherExtDto { Id = 1, Name = "Prof. Smith" },
                    new Core.DTOs.TeacherExtDto { Id = 2, Name = "Dr. Johnson" },
                    new Core.DTOs.TeacherExtDto { Id = 3, Name = "Prof. Williams" }
                },
                
                // Classroom data
                ClassroomObjects = new List<Core.DTOs.ClassroomExtDto>
                {
                    new Core.DTOs.ClassroomExtDto { Id = 1, Name = "101", Building = "Main Building" },
                    new Core.DTOs.ClassroomExtDto { Id = 2, Name = "201", Building = "Science Building" },
                    new Core.DTOs.ClassroomExtDto { Id = 3, Name = "301", Building = "Computer Building" }
                },
                
                // Time slot data, including evening time slots
                TimeSlotObjects = new List<Core.DTOs.TimeSlotExtDto>
                {
                    // Monday
                    new Core.DTOs.TimeSlotExtDto { Id = 1, DayOfWeek = 1, DayName = "Monday", StartTime = "08:00", EndTime = "09:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 2, DayOfWeek = 1, DayName = "Monday", StartTime = "10:00", EndTime = "11:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 3, DayOfWeek = 1, DayName = "Monday", StartTime = "14:00", EndTime = "15:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 4, DayOfWeek = 1, DayName = "Monday", StartTime = "16:00", EndTime = "17:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 21, DayOfWeek = 1, DayName = "Monday", StartTime = "19:00", EndTime = "20:30" },
                    
                    // Tuesday
                    new Core.DTOs.TimeSlotExtDto { Id = 5, DayOfWeek = 2, DayName = "Tuesday", StartTime = "08:00", EndTime = "09:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 6, DayOfWeek = 2, DayName = "Tuesday", StartTime = "10:00", EndTime = "11:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 7, DayOfWeek = 2, DayName = "Tuesday", StartTime = "14:00", EndTime = "15:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 8, DayOfWeek = 2, DayName = "Tuesday", StartTime = "16:00", EndTime = "17:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 22, DayOfWeek = 2, DayName = "Tuesday", StartTime = "19:00", EndTime = "20:30" },
                    
                    // Wednesday
                    new Core.DTOs.TimeSlotExtDto { Id = 9, DayOfWeek = 3, DayName = "Wednesday", StartTime = "08:00", EndTime = "09:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 10, DayOfWeek = 3, DayName = "Wednesday", StartTime = "10:00", EndTime = "11:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 11, DayOfWeek = 3, DayName = "Wednesday", StartTime = "14:00", EndTime = "15:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 12, DayOfWeek = 3, DayName = "Wednesday", StartTime = "16:00", EndTime = "17:30" },
                    new Core.DTOs.TimeSlotExtDto { Id = 23, DayOfWeek = 3, DayName = "Wednesday", StartTime = "19:00", EndTime = "20:30" },
                }
            };
            
            // Use mock-schedule endpoint logic to process request
            return MockSchedule(request);
        }
    }
} 