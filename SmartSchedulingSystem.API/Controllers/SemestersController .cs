using Microsoft.AspNetCore.Mvc;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemestersController : ControllerBase
    {
        private readonly ISemesterService _semesterService;

        public SemestersController(ISemesterService semesterService)
        {
            _semesterService = semesterService;
        }

        [HttpGet]
        public async Task<ActionResult<List<SemesterDto>>> GetAllSemesters()
        {
            try
            {
                var semesters = await _semesterService.GetAllSemestersAsync();
                return Ok(semesters);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SemesterDto>> GetSemesterById(int id)
        {
            try
            {
                var semester = await _semesterService.GetSemesterByIdAsync(id);
                if (semester == null)
                    return NotFound();

                return Ok(semester);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<SemesterDto>> CreateSemester(SemesterDto semesterDto)
        {
            try
            {
                var createdSemester = await _semesterService.CreateSemesterAsync(semesterDto);
                return CreatedAtAction(nameof(GetSemesterById), new { id = createdSemester.SemesterId }, createdSemester);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SemesterDto>> UpdateSemester(int id, SemesterDto semesterDto)
        {
            if (id != semesterDto.SemesterId)
                return BadRequest("ID mismatch");

            try
            {
                var updatedSemester = await _semesterService.UpdateSemesterAsync(semesterDto);
                return Ok(updatedSemester);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSemester(int id)
        {
            try
            {
                var result = await _semesterService.DeleteSemesterAsync(id);
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