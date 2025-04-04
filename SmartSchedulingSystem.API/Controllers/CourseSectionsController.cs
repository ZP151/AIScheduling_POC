using Microsoft.AspNetCore.Mvc;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseSectionsController : ControllerBase
    {
        private readonly ICourseSectionService _courseSectionService;

        public CourseSectionsController(ICourseSectionService courseSectionService)
        {
            _courseSectionService = courseSectionService;
        }

        [HttpGet]
        public async Task<ActionResult<List<CourseSectionDto>>> GetAllCourseSections()
        {
            try
            {
                var sections = await _courseSectionService.GetAllCourseSectionsAsync();
                return Ok(sections);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("semester/{semesterId}")]
        public async Task<ActionResult<List<CourseSectionDto>>> GetCourseSectionsBySemester(int semesterId)
        {
            try
            {
                var sections = await _courseSectionService.GetCourseSectionsBySemesterAsync(semesterId);
                return Ok(sections);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<List<CourseSectionDto>>> GetCourseSectionsByCourse(int courseId)
        {
            try
            {
                var sections = await _courseSectionService.GetCourseSectionsByCourseAsync(courseId);
                return Ok(sections);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CourseSectionDto>> GetCourseSectionById(int id)
        {
            try
            {
                var section = await _courseSectionService.GetCourseSectionByIdAsync(id);
                if (section == null)
                    return NotFound();

                return Ok(section);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<CourseSectionDto>> CreateCourseSection(CourseSectionDto sectionDto)
        {
            try
            {
                var createdSection = await _courseSectionService.CreateCourseSectionAsync(sectionDto);
                return CreatedAtAction(nameof(GetCourseSectionById), new { id = createdSection.CourseSectionId }, createdSection);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CourseSectionDto>> UpdateCourseSection(int id, CourseSectionDto sectionDto)
        {
            if (id != sectionDto.CourseSectionId)
                return BadRequest("ID mismatch");

            try
            {
                var updatedSection = await _courseSectionService.UpdateCourseSectionAsync(sectionDto);
                return Ok(updatedSection);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCourseSection(int id)
        {
            try
            {
                var result = await _courseSectionService.DeleteCourseSectionAsync(id);
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