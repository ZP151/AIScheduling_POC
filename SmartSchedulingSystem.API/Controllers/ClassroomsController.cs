using Microsoft.AspNetCore.Mvc;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassroomsController : ControllerBase
    {
        private readonly IClassroomService _classroomService;

        public ClassroomsController(IClassroomService classroomService)
        {
            _classroomService = classroomService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ClassroomDto>>> GetAllClassrooms()
        {
            try
            {
                var classrooms = await _classroomService.GetAllClassroomsAsync();
                return Ok(classrooms);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClassroomDto>> GetClassroomById(int id)
        {
            try
            {
                var classroom = await _classroomService.GetClassroomByIdAsync(id);
                if (classroom == null)
                    return NotFound();

                return Ok(classroom);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{classroomId}/availability")]
        public async Task<ActionResult<List<ClassroomAvailabilityDto>>> GetClassroomAvailability(int classroomId)
        {
            try
            {
                var availabilities = await _classroomService.GetClassroomAvailabilityAsync(classroomId);
                return Ok(availabilities);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{classroomId}/availability")]
        public async Task<ActionResult> UpdateClassroomAvailability(int classroomId, List<ClassroomAvailabilityDto> availabilities)
        {
            try
            {
                var result = await _classroomService.UpdateClassroomAvailabilityAsync(classroomId, availabilities);
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
        public async Task<ActionResult<ClassroomDto>> CreateClassroom(ClassroomDto classroomDto)
        {
            try
            {
                var createdClassroom = await _classroomService.CreateClassroomAsync(classroomDto);
                return CreatedAtAction(nameof(GetClassroomById), new { id = createdClassroom.ClassroomId }, createdClassroom);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ClassroomDto>> UpdateClassroom(int id, ClassroomDto classroomDto)
        {
            if (id != classroomDto.ClassroomId)
                return BadRequest("ID mismatch");

            try
            {
                var updatedClassroom = await _classroomService.UpdateClassroomAsync(classroomDto);
                return Ok(updatedClassroom);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteClassroom(int id)
        {
            try
            {
                var result = await _classroomService.DeleteClassroomAsync(id);
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