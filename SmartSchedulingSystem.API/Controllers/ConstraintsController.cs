using Microsoft.AspNetCore.Mvc;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConstraintsController : ControllerBase
    {
        private readonly ISchedulingConstraintService _constraintService;

        public ConstraintsController(ISchedulingConstraintService constraintService)
        {
            _constraintService = constraintService;
        }

        [HttpGet]
        public async Task<ActionResult<List<SchedulingConstraintDto>>> GetAllConstraints()
        {
            try
            {
                var constraints = await _constraintService.GetAllConstraintsAsync();
                return Ok(constraints);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SchedulingConstraintDto>> GetConstraintById(int id)
        {
            try
            {
                var constraint = await _constraintService.GetConstraintByIdAsync(id);
                if (constraint == null)
                    return NotFound();

                return Ok(constraint);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<SchedulingConstraintDto>> CreateConstraint(SchedulingConstraintDto constraintDto)
        {
            try
            {
                var createdConstraint = await _constraintService.CreateConstraintAsync(constraintDto);
                return CreatedAtAction(nameof(GetConstraintById), new { id = createdConstraint.ConstraintId }, createdConstraint);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SchedulingConstraintDto>> UpdateConstraint(int id, SchedulingConstraintDto constraintDto)
        {
            if (id != constraintDto.ConstraintId)
                return BadRequest("ID mismatch");

            try
            {
                var updatedConstraint = await _constraintService.UpdateConstraintAsync(constraintDto);
                return Ok(updatedConstraint);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("settings")]
        public async Task<ActionResult> UpdateConstraintsSettings(List<ConstraintSettingDto> constraintSettings)
        {
            try
            {
                var result = await _constraintService.UpdateConstraintsSettingsAsync(constraintSettings);
                if (result)
                    return Ok();

                return BadRequest(new { message = "Failed to update constraint settings" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteConstraint(int id)
        {
            try
            {
                var result = await _constraintService.DeleteConstraintAsync(id);
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