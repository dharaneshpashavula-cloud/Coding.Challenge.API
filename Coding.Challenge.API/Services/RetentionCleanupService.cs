using Coding.Challenge.API.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace Coding.Challenge.API.Services
{
    public class RetentionOptions
    {
        public int DaysToKeep { get; set; } = 30;
    }

    public class RetentionCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RetentionCleanupService> _logger;
        private readonly int _daysToKeep;

        public RetentionCleanupService(IServiceScopeFactory scopeFactory, ILogger<RetentionCleanupService> logger, IOptions<RetentionOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _daysToKeep = options.Value.DaysToKeep;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run once a day
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();

                    var cutoff = DateTime.UtcNow.AddDays(-_daysToKeep);
                    var old = db.SensorEvents.Where(e => e.Timestamp < cutoff);
                    int removed = 0;
                    if (await old.AnyAsync(stoppingToken))
                    {
                        db.SensorEvents.RemoveRange(old);
                        removed = await db.SaveChangesAsync(stoppingToken);
                    }

                    _logger.LogInformation("RetentionCleanup removed {Removed} old events older than {Cutoff}", removed, cutoff);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during retention cleanup");
                }

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
