using Microsoft.AspNetCore.Mvc;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeachersController : ControllerBase
    {
        private readonly ITeacherService _teacherService;

        public TeachersController(ITeacherService teacherService)
        {
            _teacherService = teacherService;
        }

        [HttpGet]
        public async Task<ActionResult<List<TeacherDto>>> GetAllTeachers()
        {
            try
            {
                var teachers = await _teacherService.GetAllTeachersAsync();
                return Ok(teachers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("department/{departmentId}")]
        public async Task<ActionResult<List<TeacherDto>>> GetTeachersByDepartment(int departmentId)
        {
            try
            {
                var teachers = await _teacherService.GetTeachersByDepartmentAsync(departmentId);
                return Ok(teachers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TeacherDto>> GetTeacherById(int id)
        {
            try
            {
                var teacher = await _teacherService.GetTeacherByIdAsync(id);
                if (teacher == null)
                    return NotFound();

                return Ok(teacher);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{teacherId}/availability")]
        public async Task<ActionResult<List<TeacherAvailabilityDto>>> GetTeacherAvailability(int teacherId)
        {
            try
            {
                var availabilities = await _teacherService.GetTeacherAvailabilityAsync(teacherId);
                return Ok(availabilities);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{teacherId}/availability")]
        public async Task<ActionResult> UpdateTeacherAvailability(int teacherId, List<TeacherAvailabilityDto> availabilities)
        {
            try
            {
                var result = await _teacherService.UpdateTeacherAvailabilityAsync(teacherId, availabilities);
                if (result)
                    return Ok();

                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TeacherDto>> CreateTeacher(TeacherDto teacherDto)
        {
            try
            {
                var createdTeacher = await _teacherService.CreateTeacherAsync(teacherDto);
                return CreatedAtAction(nameof(GetTeacherById), new { id = createdTeacher.TeacherId }, createdTeacher);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TeacherDto>> UpdateTeacher(int id, TeacherDto teacherDto)
        {
            if (id != teacherDto.TeacherId)
                return BadRequest("ID mismatch");

            try
            {
                var updatedTeacher = await _teacherService.UpdateTeacherAsync(teacherDto);
                return Ok(updatedTeacher);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTeacher(int id)
        {
            try
            {
                var result = await _teacherService.DeleteTeacherAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}