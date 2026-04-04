using Coding.Challenge.API.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Coding.Challenge.API.Services
{
    /// <summary>
    /// Background service that consumes sensor events from the channel and persists them using the repository.
    /// In production this could be a message consumer.
    /// </summary>
    public class SensorEventConsumer : BackgroundService
    {
        private readonly Channel<SensorEvent> _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SensorEventConsumer> _logger;
        private readonly int _maxRetries = 3;
        private readonly TimeSpan _baseDelay = TimeSpan.FromMilliseconds(200);

        public SensorEventConsumer(Channel<SensorEvent> channel, IServiceScopeFactory scopeFactory, ILogger<SensorEventConsumer> logger)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var evt in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                // Process with retry/backoff and on persistent failure, move to dead-letter table
                var success = false;
                Exception? lastEx = null;

                for (int attempt = 1; attempt <= _maxRetries && !success; attempt++)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<ISensorEventRepository>();

                        // Idempotency: check if an identical event was already processed recently (simple example)
                        var exists = (await repo.QueryAsync(evt.Gate, evt.Type, evt.Timestamp.AddSeconds(-1), evt.Timestamp.AddSeconds(1), stoppingToken)).Any(e => e.NumberOfPeople == evt.NumberOfPeople);
                        if (exists)
                        {
                            _logger.LogInformation("Skipping duplicate event for {Gate} {Timestamp}", evt.Gate, evt.Timestamp);
                            success = true;
                            break;
                        }

                        await repo.AddAsync(evt, stoppingToken);
                        _logger.LogDebug("Consumed and saved sensor event {Gate} {Type} {Count}", evt.Gate, evt.Type, evt.NumberOfPeople);
                        success = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                        _logger.LogWarning(ex, "Error saving sensor event (attempt {Attempt}/{Max})", attempt, _maxRetries);
                        await Task.Delay(TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)), stoppingToken);
                    }
                }

                if (!success)
                {
                    // Move to dead-letter store
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<Coding.Challenge.API.Data.AnalyticsDbContext>();
                        db.DeadLetters.Add(new Coding.Challenge.API.Models.DeadLetter
                        {
                            Gate = evt.Gate,
                            Timestamp = evt.Timestamp,
                            NumberOfPeople = evt.NumberOfPeople,
                            Type = evt.Type,
                            Error = lastEx?.ToString()
                        });
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogError(lastEx, "Event moved to dead-letter store for gate {Gate} at {Timestamp}", evt.Gate, evt.Timestamp);
                    }
                    catch (Exception dex)
                    {
                        _logger.LogError(dex, "Failed to persist dead-letter for event {Gate} {Timestamp}", evt.Gate, evt.Timestamp);
                    }
                }
            }
        }
    }
}
