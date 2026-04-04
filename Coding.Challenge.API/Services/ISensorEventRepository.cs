using Coding.Challenge.API.Models;
using Coding.Challenge.API.DTOs;

namespace Coding.Challenge.API.Services
{
    /// <summary>
    /// Repository abstraction for sensor events to allow easier testing and separation of concerns.
    /// </summary>
    public interface ISensorEventRepository
    {
        Task AddAsync(SensorEvent sensorEvent, CancellationToken cancellationToken = default);

        Task<IEnumerable<SensorEvent>> QueryAsync(string? gate = null, string? type = null, DateTime? start = null, DateTime? end = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Query aggregated results grouped by gate and type and summed by number of people. This is projected at the database level.
        /// </summary>
        Task<IEnumerable<AggregatedResultDto>> QueryAggregatedAsync(string? gate = null, string? type = null, DateTime? start = null, DateTime? end = null, CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);
    }
}
