using Coding.Challenge.API.DTOs;
using Coding.Challenge.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;

namespace Coding.Challenge.API.Controllers
{
    /// <summary>
    /// Optional controller to accept sensor events via HTTP POST for testing or alternate ingestion.
    /// In production, sensors would push events to a broker; this endpoint allows manual injection.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SensorEventsController : ControllerBase
    {
        private readonly Channel<SensorEvent> _channel;

        public SensorEventsController(Channel<SensorEvent> channel)
        {
            _channel = channel;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorEventDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var evt = new SensorEvent
            {
                Gate = dto.Gate,
                Timestamp = dto.Timestamp,
                NumberOfPeople = dto.NumberOfPeople,
                Type = dto.Type
            };

            await _channel.Writer.WriteAsync(evt);
            return Accepted();
        }
    }
}
