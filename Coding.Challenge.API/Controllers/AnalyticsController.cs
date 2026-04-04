using Coding.Challenge.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Coding.Challenge.API.Controllers
{
    /// <summary>
    /// API endpoints to retrieve analytics grouped by gate and type.
    /// Supports optional filtering by gate, type and time range.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ISensorEventRepository _repo;

        public AnalyticsController(ISensorEventRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Returns aggregated sensor results grouped by gate and type.
        /// Example response item: { gate: 'Gate A', type: 'enter', numberOfPeople: 100 }
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? gate, [FromQuery] string? type, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            var aggregated = await _repo.QueryAggregatedAsync(gate, type, start, end);

            var result = aggregated.Select(a => new { gate = a.Gate, type = a.Type, numberOfPeople = a.NumberOfPeople });

            return Ok(result);
        }
    }
}
