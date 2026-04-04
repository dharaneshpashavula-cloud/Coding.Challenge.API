using Coding.Challenge.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coding.Challenge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly AnalyticsDbContext _db;

        public MetricsController(AnalyticsDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var totalEvents = await _db.SensorEvents.CountAsync();
            var deadLetters = await _db.DeadLetters.CountAsync();

            return Ok(new { totalEvents, deadLetters });
        }
    }
}
