using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;

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

        public SchedulingController(
            ISchedulingService schedulingService,
            ISemesterService semesterService,
            ITeacherService teacherService,
            IClassroomService classroomService,
            ITimeSlotService timeSlotService,
            ISchedulingConstraintService constraintService,
            ICourseService courseService,
            ICourseSectionService courseSectionService,
            ILogger<SchedulingController> logger)
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
        }

        [HttpPost("generate")]
        public async Task<ActionResult<ScheduleResultDto>> GenerateSchedule([FromBody] ScheduleRequestDto request)
        {
            try
            {
                // 详细日志
                _logger.LogInformation("接收到排课请求: {@Request}", request);

                // 验证输入
                if (request.SemesterId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "无效的学期ID",
                        semesterId = request.SemesterId
                    });
                }

                var result = await _schedulingService.GenerateScheduleAsync(request);
                return Ok(result);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "排课问题准备失败：{Message}", ex.Message);
                return BadRequest(new
                {
                    message = "准备排课问题时发生错误",
                    details = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "无法生成排课方案：{Message}", ex.Message);
                return BadRequest(new
                {
                    message = "无法生成排课方案",
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成排课时发生未知错误");
                return StatusCode(500, new
                {
                    message = "服务器内部错误",
                    details = ex.Message
                });
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
    }
}