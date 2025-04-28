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
                _logger.LogInformation("Basic scheduling request received");
                
                // Validate request
                if (request == null)
                {
                    return BadRequest(new { error = "Request cannot be empty" });
                }

                if (request.CourseSectionObjects == null || !request.CourseSectionObjects.Any())
                {
                    return BadRequest(new { error = "Course information missing in request data" });
                }
                
                if (request.TeacherObjects == null || !request.TeacherObjects.Any())
                {
                    return BadRequest(new { error = "Teacher information missing in request data" });
                }
                
                if (request.ClassroomObjects == null || !request.ClassroomObjects.Any())
                {
                    return BadRequest(new { error = "Classroom information missing in request data" });
                }
                
                if (request.TimeSlotObjects == null || !request.TimeSlotObjects.Any())
                {
                    return BadRequest(new { error = "Time slot information missing in request data" });
                }

                // Print time slot information for debugging
                _logger.LogInformation("Input time slot information:");
                foreach (var ts in request.TimeSlotObjects)
                {
                    _logger.LogInformation($"  ID: {ts.Id}, Week {ts.DayOfWeek} {ts.DayName}, Time: {ts.StartTime}-{ts.EndTime}");
                }

                // Convert DTO to SchedulingProblem (Basic mode - without availability constraints)
                var problem = ConvertToBasicSchedulingProblem(request);
                
                // Set scheduling parameters - using basic constraints (Level 1)
                var parameters = new SchedulingParameters
                {
                    EnableLocalSearch = true,
                    MaxLsIterations = 1000,
                    InitialTemperature = 100,
                    CoolingRate = 0.95,
                    UseStandardConstraints = false,  // Do not enable Level 2 constraints
                    UseBasicConstraints = true     // Use basic Level 1 constraints
                };
                
                // Use simplified mode, only including Level 1 constraints
                var result = _schedulingEngine.GenerateSchedule(problem, parameters, useSimplifiedMode: true);
                
                if (result.Status == SchedulingStatus.Success || result.Status == SchedulingStatus.PartialSuccess)
                {
                    // Convert scheduling results to frontend-compatible format
                    var response = ConvertToApiResult(result, request.SemesterId);
                    return Ok(response);
                }
                else
                {
                    // If no scheduling solution was successfully generated, return error message
                    return StatusCode(500, new
                    {
                        error = "Scheduling failed",
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
                _logger.LogError(ex, "Exception occurred while processing scheduling request");
                return StatusCode(500, new
                {
                    error = "Scheduling execution failed",
                    message = ex.Message,
                    solutions = new List<object>(),
                    schedules = new List<object>(),
                    generatedAt = DateTime.Now,
                    totalSolutions = 0,
                    bestScore = 0.0,
                    averageScore = 0.0,
                    errorMessage = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpPost("generate-advanced")]
        public IActionResult GenerateAdvancedSchedule([FromBody] ScheduleRequestDto request)
        {
            try
            {
                _logger.LogInformation("Advanced scheduling request received");
                
                // Validate request
                if (request == null)
                {
                    return BadRequest(new { error = "Request cannot be empty" });
                }

                if (request.CourseSectionObjects == null || !request.CourseSectionObjects.Any())
                {
                    return BadRequest(new { error = "Course information missing in request data" });
                }
                
                if (request.TeacherObjects == null || !request.TeacherObjects.Any())
                {
                    return BadRequest(new { error = "Teacher information missing in request data" });
                }
                
                if (request.ClassroomObjects == null || !request.ClassroomObjects.Any())
                {
                    return BadRequest(new { error = "Classroom information missing in request data" });
                }
                
                if (request.TimeSlotObjects == null || !request.TimeSlotObjects.Any())
                {
                    return BadRequest(new { error = "Time slot information missing in request data" });
                }

                // Print time slot information for debugging
                _logger.LogInformation("Input time slot information (Advanced mode):");
                foreach (var ts in request.TimeSlotObjects)
                {
                    _logger.LogInformation($"  ID: {ts.Id}, Week {ts.DayOfWeek} {ts.DayName}, Time: {ts.StartTime}-{ts.EndTime}");
                }

                // Convert DTO to SchedulingProblem (Advanced mode - with availability constraints)
                var problem = ConvertToAdvancedSchedulingProblem(request);
                
                // Set scheduling parameters - using advanced constraints (Level 2)
                var parameters = new SchedulingParameters
                {
                    EnableLocalSearch = true,
                    MaxLsIterations = 1000,
                    InitialTemperature = 100,
                    CoolingRate = 0.95,
                    UseStandardConstraints = true,   // Enable standard Level 2 constraints
                    UseBasicConstraints = false    // Do not use basic constraints
                };
                
                // Use complete mode, including Level 2 constraints
                var result = _schedulingEngine.GenerateSchedule(problem, parameters, useSimplifiedMode: false);
                
                if (result.Status == SchedulingStatus.Success || result.Status == SchedulingStatus.PartialSuccess)
                {
                    // Convert scheduling results to frontend-compatible format
                    var response = ConvertToApiResult(result, request.SemesterId);
                    return Ok(response);
                }
                else
                {
                    // If no scheduling solution was successfully generated, return error message
                    return StatusCode(500, new
                    {
                        error = "Advanced scheduling failed",
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
                _logger.LogError(ex, "Exception occurred while processing advanced scheduling request");
                return StatusCode(500, new
                {
                    error = "Advanced scheduling execution failed",
                    message = ex.Message,
                    solutions = new List<object>(),
                    schedules = new List<object>(),
                    generatedAt = DateTime.Now,
                    totalSolutions = 0,
                    bestScore = 0.0,
                    averageScore = 0.0,
                    errorMessage = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpPost("generate-enhanced")]
        public IActionResult GenerateEnhancedSchedule([FromBody] ScheduleRequestDto request)
        {
            try
            {
                _logger.LogInformation("Enhanced level scheduling request received");
                
                // Validate request
                if (request == null)
                {
                    return BadRequest(new { error = "Request cannot be empty" });
                }

                if (request.CourseSectionObjects == null || !request.CourseSectionObjects.Any())
                {
                    return BadRequest(new { error = "Course information missing in request data" });
                }
                
                if (request.TeacherObjects == null || !request.TeacherObjects.Any())
                {
                    return BadRequest(new { error = "Teacher information missing in request data" });
                }
                
                if (request.ClassroomObjects == null || !request.ClassroomObjects.Any())
                {
                    return BadRequest(new { error = "Classroom information missing in request data" });
                }
                
                if (request.TimeSlotObjects == null || !request.TimeSlotObjects.Any())
                {
                    return BadRequest(new { error = "Time slot information missing in request data" });
                }

                // Print time slot information for debugging
                _logger.LogInformation("Input time slot information (Enhanced level mode):");
                foreach (var ts in request.TimeSlotObjects)
                {
                    _logger.LogInformation($"  ID: {ts.Id}, Week {ts.DayOfWeek} {ts.DayName}, Time: {ts.StartTime}-{ts.EndTime}");
                }

                // Convert DTO to SchedulingProblem (Enhanced mode - with availability and resource constraints)
                var problem = ConvertToEnhancedSchedulingProblem(request);
                
                // Set scheduling parameters - using enhanced constraints (Level 3)
                var parameters = new SchedulingParameters
                {
                    EnableLocalSearch = true,
                    MaxLsIterations = 1000,
                    InitialTemperature = 100,
                    CoolingRate = 0.95,
                    UseStandardConstraints = true,   // Enable standard Level 2 constraints
                    UseBasicConstraints = false,     // Do not use basic constraints
                    UseEnhancedConstraints = true,   // Enable enhanced constraints
                    ResourceConstraintLevel = ConstraintApplicationLevel.Enhanced
                };
                
                // Use enhanced mode
                var result = _schedulingEngine.GenerateSchedule(problem, parameters, useSimplifiedMode: false);
                
                if (result.Status == SchedulingStatus.Success || result.Status == SchedulingStatus.PartialSuccess)
                {
                    // Convert scheduling results to frontend-compatible format
                    var response = ConvertToApiResult(result, request.SemesterId);
                    return Ok(response);
                }
                else
                {
                    // If no scheduling solution was successfully generated, return error message
                    return StatusCode(500, new
                    {
                        error = "Enhanced level scheduling failed",
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
                _logger.LogError(ex, "Exception occurred while processing enhanced level scheduling request");
                return StatusCode(500, new
                {
                    error = "Enhanced level scheduling execution failed",
                    message = ex.Message,
                    solutions = new List<object>(),
                    schedules = new List<object>(),
                    generatedAt = DateTime.Now,
                    totalSolutions = 0,
                    bestScore = 0.0,
                    averageScore = 0.0,
                    errorMessage = $"Internal server error: {ex.Message}"
                });
            }
        }

        // Convert DTO to basic SchedulingProblem (without availability constraints)
        private SchedulingProblem ConvertToBasicSchedulingProblem(ScheduleRequestDto request)
        {
            var problem = new SchedulingProblem
            {
                SemesterId = request.SemesterId,
                Name = $"Basic Schedule for Semester {request.SemesterId}",
                GenerateMultipleSolutions = request.GenerateMultipleSolutions,
                SolutionCount = request.SolutionCount > 0 ? request.SolutionCount : 3
            };

            // Convert course information
            problem.CourseSections = request.CourseSectionObjects.Select(cs => new CourseSectionInfo
            {
                Id = cs.Id,
                CourseId = cs.Id, // If there is no separate CourseId, SectionId can be used
                CourseCode = cs.CourseCode,
                CourseName = cs.CourseName,
                SectionCode = cs.SectionCode,
                Enrollment = cs.Enrollment // If available in DTO
            }).ToList();

            // Convert teacher information
            problem.Teachers = request.TeacherObjects.Select(t => new TeacherInfo
            {
                Id = t.Id,
                Name = t.Name,
                Title = t.Title, // If available in DTO
                DepartmentId = t.DepartmentId // If available in DTO
            }).ToList();

            // Convert classroom information
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

            // Convert time slot information
            problem.TimeSlots = request.TimeSlotObjects.Select(ts => new TimeSlotInfo
            {
                Id = ts.Id,
                DayOfWeek = ts.DayOfWeek,
                DayName = ts.DayName,
                // Parse time string to TimeSpan
                StartTime = ParseTimeString(ts.StartTime),
                EndTime = ParseTimeString(ts.EndTime),
                IsAvailable = true, // Default available
                Type = "Regular" // Default type
            }).ToList();

            // In basic mode, do not add teacher and classroom availability constraints
            _logger.LogInformation("Basic mode: Not adding teacher and classroom availability constraints");

            return problem;
        }

        // Convert DTO to advanced SchedulingProblem (with availability constraints)
        private SchedulingProblem ConvertToAdvancedSchedulingProblem(ScheduleRequestDto request)
        {
            var problem = new SchedulingProblem
            {
                SemesterId = request.SemesterId,
                Name = $"Advanced Schedule for Semester {request.SemesterId}",
                GenerateMultipleSolutions = request.GenerateMultipleSolutions,
                SolutionCount = request.SolutionCount > 0 ? request.SolutionCount : 3
            };

            // Convert course information
            problem.CourseSections = request.CourseSectionObjects.Select(cs => new CourseSectionInfo
            {
                Id = cs.Id,
                CourseId = cs.Id, // If there is no separate CourseId, SectionId can be used
                CourseCode = cs.CourseCode,
                CourseName = cs.CourseName,
                SectionCode = cs.SectionCode,
                Enrollment = cs.Enrollment // If available in DTO
            }).ToList();

            // Convert teacher information
            problem.Teachers = request.TeacherObjects.Select(t => new TeacherInfo
            {
                Id = t.Id,
                Name = t.Name,
                Title = t.Title, // If available in DTO
                DepartmentId = t.DepartmentId // If available in DTO
            }).ToList();

            // Convert classroom information
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

            // Convert time slot information
            problem.TimeSlots = request.TimeSlotObjects.Select(ts => new TimeSlotInfo
            {
                Id = ts.Id,
                DayOfWeek = ts.DayOfWeek,
                DayName = ts.DayName,
                // Parse time string to TimeSpan
                StartTime = ParseTimeString(ts.StartTime),
                EndTime = ParseTimeString(ts.EndTime),
                IsAvailable = true, // Default available
                Type = "Regular" // Default type
            }).ToList();

            // Add teacher availability (Advanced mode includes availability constraints)
            _logger.LogInformation("Advanced mode: Adding teacher and classroom availability constraints");
            foreach (var teacher in problem.Teachers)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // Set specific times as unavailable for specific teachers (simulated data)
                    bool isAvailable = true;
                    string unavailableReason = null;

                    // Smith professor is only available on Tuesday and Wednesday mornings
                    if (teacher.Id == 1)
                    {
                        // Only available on Tuesday and Wednesday mornings, other times unavailable
                        if ((timeSlot.DayOfWeek == 2 || timeSlot.DayOfWeek == 3) && timeSlot.StartTime.Hours < 12)
                        {
                            isAvailable = true; // Tuesday and Wednesday mornings available
                        }
                        else
                        {
                            isAvailable = false;
                            unavailableReason = "Only available on Tuesday and Wednesday mornings";
                        }
                    }
                    // Johnson professor is unavailable Wednesday afternoons
                    else if (teacher.Id == 2 && timeSlot.DayOfWeek == 3 && timeSlot.StartTime.Hours >= 12)
                    {
                        isAvailable = false;
                        unavailableReason = "Research time";
                    }
                    // Davis professor is unavailable all day on Tuesday
                    else if (teacher.Id == 3 && timeSlot.DayOfWeek == 2)
                    {
                        isAvailable = false;
                        unavailableReason = "Teaching at another institution";
                    }
                    // Prof. Wilson is unavailable Thursday afternoons and Friday mornings.
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
                        PreferenceLevel = 3, // Default medium preference
                        ApplicableWeeks = new List<int>() // Default empty list
                    });
                }
            }

            // Add classroom availability
            foreach (var classroom in problem.Classrooms)
            {
                foreach (var timeSlot in problem.TimeSlots)
                {
                    // Set specific times as unavailable for specific classrooms (simulated data)
                    bool isAvailable = true;
                    string unavailableReason = null;

                    // A-101 maintenance on Monday morning
                    if (classroom.Id == 1 && timeSlot.DayOfWeek == 1 && timeSlot.StartTime.Hours < 10)
                    {
                        isAvailable = false;
                        unavailableReason = "Weekly maintenance";
                    }
                    // A-102 reserved for activities on Tuesday afternoon
                    else if (classroom.Id == 2 && timeSlot.DayOfWeek == 2 && timeSlot.StartTime.Hours >= 14)
                    {
                        isAvailable = false;
                        unavailableReason = "Reserved for student activities";
                    }
                    // Building C-501 unavailable on Friday afternoon
                    else if (classroom.Id == 9 && timeSlot.DayOfWeek == 5 && timeSlot.StartTime.Hours >= 12)
                    {
                        isAvailable = false;
                        unavailableReason = "Faculty meeting";
                    }
                    // Building C-601 under renovation on Thursday
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
                        ApplicableWeeks = new List<int>() // Default empty list
                    });
                }
            }

            return problem;
        }

        // Parse time string to TimeSpan
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
                _logger.LogWarning(ex, $"Error parsing time string '{timeString}'");
            }

            return TimeSpan.Zero;
        }

        // Convert scheduling results to frontend-compatible format
        private object ConvertToApiResult(SchedulingResult result, int semesterId)
        {
            var solutions = new List<object>();
            var schedules = new List<object>();
            
            // Ensure each solution has a unique ID, starting from 1
            for (int index = 0; index < result.Solutions.Count; index++)
            {
                var solution = result.Solutions[index];
                
                // Ensure each solution has a unique ID, starting from 1
                int uniqueId = index + 1;
                
                // Generate unique name for each solution
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
                
                // Add differentiated scores for each solution
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
                
                // Convert to frontend-expected schedules format
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
            
            // Calculate best score and average score
            double bestScore = solutions.Count > 0 ? 
                solutions.Max(s => ((dynamic)s).score) : 0;
                
            double totalScore = solutions.Sum(s => (double)((dynamic)s).score);
            double averageScore = solutions.Count > 0 ? 
                Math.Round(totalScore / solutions.Count, 2) : 0;
            
            // Create final result
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

        // Convert DTO to enhanced SchedulingProblem (with availability and resource constraints)
        private SchedulingProblem ConvertToEnhancedSchedulingProblem(ScheduleRequestDto request)
        {
            // First build the basic advanced problem model (with availability constraints)
            var problem = ConvertToAdvancedSchedulingProblem(request);
            
            // Set issue name to enhancement level
            problem.Name = $"Enhanced Resource Schedule for Semester {request.SemesterId}";
            
            // Add classroom resource constraints
            _logger.LogInformation("Enhanced mode: Adding classroom resource constraints");
            
            // Build course-classroom type matching relationship
            foreach (var course in problem.CourseSections)
            {
                // Simulate getting course subject type
                string courseType = "Computer Science"; // Default value
                
                // Determine type based on course code prefix
                if (course.CourseCode.StartsWith("CS"))
                {
                    courseType = "Computer Science";
                }
                else if (course.CourseCode.StartsWith("MATH"))
                {
                    courseType = "Mathematics";
                }
                else if (course.CourseCode.StartsWith("PHYS"))
                {
                    courseType = "Physics";
                }
                else if (course.CourseCode.StartsWith("FIN"))
                {
                    courseType = "Business";
                }
                else if (course.CourseCode.StartsWith("MKT"))
                {
                    courseType = "Business";
                }
                else if (course.CourseCode.StartsWith("ECON"))
                {
                    courseType = "Economics";
                }
                else if (course.CourseCode.StartsWith("BUS"))
                {
                    courseType = "Business";
                }
                
                // Add course resource requirements
                problem.CourseResourceRequirements.Add(new CourseResourceRequirement
                {
                    CourseSectionId = course.Id,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    ResourceTypes = new List<string>(), // Will be added below
                    PreferredRoomTypes = new List<string>(), // Will be added below
                    RequiredCapacity = course.Enrollment,
                    RequiresComputers = courseType == "Computer Science",
                    RequiresProjector = true, // Most courses need a projector
                    RequiresLaboratoryEquipment = courseType == "Physics" || courseType == "Chemistry",
                    ResourceMatchingWeight = 0.8
                });
                
                // Set preferred room types based on course type
                var requirement = problem.CourseResourceRequirements.Last();
                
                // Add preferred room types
                switch (courseType)
                {
                    case "Computer Science":
                        requirement.PreferredRoomTypes.Add("ComputerLab");
                        requirement.PreferredRoomTypes.Add("Lecture");
                        requirement.ResourceTypes.Add("Computers");
                        requirement.ResourceTypes.Add("Projector");
                        break;
                    case "Mathematics":
                        requirement.PreferredRoomTypes.Add("Lecture");
                        requirement.PreferredRoomTypes.Add("LargeHall");
                        requirement.ResourceTypes.Add("Whiteboard");
                        requirement.ResourceTypes.Add("Projector");
                        break;
                    case "Physics":
                        requirement.PreferredRoomTypes.Add("Laboratory");
                        requirement.PreferredRoomTypes.Add("Lecture");
                        requirement.ResourceTypes.Add("Lab Equipment");
                        break;
                    case "Chemistry":
                        requirement.PreferredRoomTypes.Add("Laboratory");
                        requirement.ResourceTypes.Add("Lab Equipment");
                        requirement.ResourceTypes.Add("Safety Facilities");
                        break;
                    case "Business":
                        requirement.PreferredRoomTypes.Add("LargeHall");
                        requirement.PreferredRoomTypes.Add("Lecture");
                        requirement.ResourceTypes.Add("Projector");
                        requirement.ResourceTypes.Add("Audio System");
                        break;
                    case "Economics":
                        requirement.PreferredRoomTypes.Add("Lecture");
                        requirement.PreferredRoomTypes.Add("LargeHall");
                        requirement.ResourceTypes.Add("Projector");
                        break;
                    default:
                        requirement.PreferredRoomTypes.Add("Lecture");
                        requirement.ResourceTypes.Add("Projector");
                        break;
                }
            }
            
            // Add classroom resource information
            foreach (var classroom in problem.Classrooms)
            {
                // Add resources based on classroom type
                var resources = new List<string>();
                
                // Add standard equipment based on classroom type
                switch (classroom.Type)
                {
                    case "ComputerLab":
                        resources.Add("Computers");
                        resources.Add("Projector");
                        resources.Add("Interactive Whiteboard");
                        resources.Add("Network Ports");
                        resources.Add("Power Outlets");
                        classroom.HasComputers = true;
                        classroom.HasProjector = true;
                        break;
                    case "Lecture":
                        resources.Add("Projector");
                        resources.Add("Whiteboard");
                        resources.Add("Audio System");
                        classroom.HasProjector = true;
                        break;
                    case "LargeHall":
                        resources.Add("Dual Projector");
                        resources.Add("Advanced Audio");
                        resources.Add("Whiteboard");
                        classroom.HasProjector = true;
                        break;
                    case "Laboratory":
                        resources.Add("Lab Equipment");
                        resources.Add("Safety Facilities");
                        resources.Add("Projector");
                        classroom.HasProjector = true;
                        break;
                }
                
                // Add classroom resource information
                problem.ClassroomResources.Add(new ClassroomResource
                {
                    ClassroomId = classroom.Id,
                    ClassroomName = classroom.Name,
                    Building = classroom.Building,
                    ResourceTypes = resources,
                    RoomType = classroom.Type,
                    Capacity = classroom.Capacity,
                    ResourceUtilizationWeight = 0.7,
                    CapacityUtilizationWeight = 0.3
                });
            }
            
            // Add room type matching scores with course types
            problem.RoomTypeMatchingScores = new Dictionary<string, Dictionary<string, double>>
            {
                {
                    "Computer Science", new Dictionary<string, double>
                    {
                        { "ComputerLab", 1.0 },
                        { "Lecture", 0.7 },
                        { "Laboratory", 0.5 },
                        { "LargeHall", 0.3 }
                    }
                },
                {
                    "Mathematics", new Dictionary<string, double>
                    {
                        { "Lecture", 1.0 },
                        { "LargeHall", 0.8 },
                        { "ComputerLab", 0.5 },
                        { "Laboratory", 0.3 }
                    }
                },
                {
                    "Physics", new Dictionary<string, double>
                    {
                        { "Laboratory", 1.0 },
                        { "Lecture", 0.6 },
                        { "ComputerLab", 0.5 },
                        { "LargeHall", 0.4 }
                    }
                },
                {
                    "Business", new Dictionary<string, double>
                    {
                        { "LargeHall", 1.0 },
                        { "Lecture", 0.8 },
                        { "ComputerLab", 0.5 },
                        { "Laboratory", 0.2 }
                    }
                },
                {
                    "Economics", new Dictionary<string, double>
                    {
                        { "Lecture", 0.9 },
                        { "LargeHall", 0.8 },
                        { "ComputerLab", 0.4 },
                        { "Laboratory", 0.1 }
                    }
                }
            };
            
            return problem;
        }
    }
} 