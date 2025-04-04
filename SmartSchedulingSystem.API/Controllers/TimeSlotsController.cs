using Microsoft.AspNetCore.Mvc;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Core.Interfaces;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeSlotsController : ControllerBase
    {
        private readonly ITimeSlotService _timeSlotService;

        public TimeSlotsController(ITimeSlotService timeSlotService)
        {
            _timeSlotService = timeSlotService;
        }

        [HttpGet]
        public async Task<ActionResult<List<TimeSlotDto>>> GetAllTimeSlots()
        {
            try
            {
                var timeSlots = await _timeSlotService.GetAllTimeSlotsAsync();
                return Ok(timeSlots);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TimeSlotDto>> GetTimeSlotById(int id)
        {
            try
            {
                var timeSlot = await _timeSlotService.GetTimeSlotByIdAsync(id);
                if (timeSlot == null)
                    return NotFound();

                return Ok(timeSlot);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TimeSlotDto>> CreateTimeSlot(TimeSlotDto timeSlotDto)
        {
            try
            {
                var createdTimeSlot = await _timeSlotService.CreateTimeSlotAsync(timeSlotDto);
                return CreatedAtAction(nameof(GetTimeSlotById), new { id = createdTimeSlot.TimeSlotId }, createdTimeSlot);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TimeSlotDto>> UpdateTimeSlot(int id, TimeSlotDto timeSlotDto)
        {
            if (id != timeSlotDto.TimeSlotId)
                return BadRequest("ID mismatch");

            try
            {
                var updatedTimeSlot = await _timeSlotService.UpdateTimeSlotAsync(timeSlotDto);
                return Ok(updatedTimeSlot);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTimeSlot(int id)
        {
            try
            {
                var result = await _timeSlotService.DeleteTimeSlotAsync(id);
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