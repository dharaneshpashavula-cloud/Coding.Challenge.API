using Coding.Challenge.API.Models;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Threading.Channels;

namespace Coding.Challenge.API.Services
{
    /// <summary>
    /// Background service that simulates sensor events and writes them into a channel asynchronously.
    /// In a real system this could publish to a message broker (Kafka, RabbitMQ, etc.).
    /// </summary>
    public class SensorEventProducer : BackgroundService
    {
        private readonly Channel<SensorEvent> _channel;
        private readonly ILogger<SensorEventProducer> _logger;
        private readonly Random _random = new();
        private readonly string[] _gates = new[] { "Gate A", "Gate B", "Gate C" };
        private readonly string[] _types = new[] { "enter", "leave" };

        public SensorEventProducer(Channel<SensorEvent> channel, ILogger<SensorEventProducer> logger)
        {
            _channel = channel;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Produce one event per second for demo purposes. In real life this would match sensor cadence.
            while (!stoppingToken.IsCancellationRequested)
            {
                var evt = new SensorEvent
                {
                    Gate = _gates[_random.Next(_gates.Length)],
                    Timestamp = DateTime.UtcNow,
                    NumberOfPeople = _random.Next(1, 20),
                    Type = _types[_random.Next(_types.Length)]
                };

                try
                {
                    await _channel.Writer.WriteAsync(evt, stoppingToken);
                    _logger.LogDebug("Produced sensor event: {Event}", JsonSerializer.Serialize(evt));
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // ignore on shutdown
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
