using Microsoft.AspNetCore.Mvc;

namespace SmartSchedulingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "健康", message = "API服务正常运行", timestamp = DateTime.Now });
        }
    }
} 