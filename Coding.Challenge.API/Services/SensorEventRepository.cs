using Coding.Challenge.API.Data;
using Coding.Challenge.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Coding.Challenge.API.Services
{
    /// <summary>
    /// EF Core implementation of repository for sensor events.
    /// </summary>
    public class SensorEventRepository : ISensorEventRepository
    {
        private readonly AnalyticsDbContext _db;

        public SensorEventRepository(AnalyticsDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(SensorEvent sensorEvent, CancellationToken cancellationToken = default)
        {
            _db.SensorEvents.Add(sensorEvent);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<SensorEvent>> QueryAsync(string? gate = null, string? type = null, DateTime? start = null, DateTime? end = null, CancellationToken cancellationToken = default)
        {
            var q = _db.SensorEvents.AsQueryable();

            if (!string.IsNullOrWhiteSpace(gate))
                q = q.Where(x => x.Gate == gate);

            if (!string.IsNullOrWhiteSpace(type))
                q = q.Where(x => x.Type == type);

            if (start.HasValue)
                q = q.Where(x => x.Timestamp >= start.Value);

            if (end.HasValue)
                q = q.Where(x => x.Timestamp <= end.Value);

            return await q.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<DTOs.AggregatedResultDto>> QueryAggregatedAsync(string? gate = null, string? type = null, DateTime? start = null, DateTime? end = null, CancellationToken cancellationToken = default)
        {
            var q = _db.SensorEvents.AsQueryable();

            if (!string.IsNullOrWhiteSpace(gate))
                q = q.Where(x => x.Gate == gate);

            if (!string.IsNullOrWhiteSpace(type))
                q = q.Where(x => x.Type == type);

            if (start.HasValue)
                q = q.Where(x => x.Timestamp >= start.Value);

            if (end.HasValue)
                q = q.Where(x => x.Timestamp <= end.Value);

            // Project to a client-translatable anonymous type first, then materialize and map to DTOs.
            var projected = await q.AsNoTracking()
                .GroupBy(e => new { e.Gate, e.Type })
                .Select(g => new
                {
                    Gate = g.Key.Gate,
                    Type = g.Key.Type,
                    NumberOfPeople = g.Sum(x => x.NumberOfPeople)
                })
                .ToListAsync(cancellationToken);

            var result = projected
                .OrderBy(x => x.Gate)
                .ThenBy(x => x.Type)
                .Select(x => new DTOs.AggregatedResultDto(x.Gate, x.Type, x.NumberOfPeople))
                .ToList();

            return result;
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return _db.SensorEvents.CountAsync(cancellationToken);
        }
    }
}
