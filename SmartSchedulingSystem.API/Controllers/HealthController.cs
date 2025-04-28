using Microsoft.AspNetCore.Mvc;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult CheckHealth()
        {
            return Ok(new { status = "Healthy", message = "API service is running normally", timestamp = DateTime.Now });
        }
    }
} 