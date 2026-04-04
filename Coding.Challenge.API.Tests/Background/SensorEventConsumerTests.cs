using System.Threading.Channels;
using Coding.Challenge.API.Models;
using Coding.Challenge.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Coding.Challenge.API.Tests.Background
{
    public class SensorEventConsumerTests
    {
        [Fact]
        public async Task Consumer_PersistsEventsFromChannel()
        {
            var options = new DbContextOptionsBuilder<Coding.Challenge.API.Data.AnalyticsDbContext>()
                .UseInMemoryDatabase(databaseName: "consumer_test_db_")
                .Options;

            using var db = new Coding.Challenge.API.Data.AnalyticsDbContext(options);
            var repo = new SensorEventRepository(db);

            var channel = Channel.CreateUnbounded<SensorEvent>();
            var logger = NullLogger<SensorEventConsumer>.Instance;

            // Build a real DI container so that the consumer can create scopes and resolve the repository
            var services = new ServiceCollection();
            services.AddDbContext<Coding.Challenge.API.Data.AnalyticsDbContext>(options => options.UseInMemoryDatabase("consumer_test_db_"));
            services.AddScoped<ISensorEventRepository, SensorEventRepository>();
            var provider = services.BuildServiceProvider();
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

            var consumer = new SensorEventConsumer(channel, scopeFactory, logger);

            var cts = new System.Threading.CancellationTokenSource();

            // Start consumer
            var consumerTask = consumer.StartAsync(cts.Token);

            // Write an event
            await channel.Writer.WriteAsync(new SensorEvent { Gate = "Gate X", Timestamp = System.DateTime.UtcNow, NumberOfPeople = 7, Type = "enter" });

            // Give consumer a moment to process
            await Task.Delay(200);

            // Stop consumer
            cts.Cancel();
            await consumer.StopAsync(System.Threading.CancellationToken.None);

            var count = await repo.CountAsync();
            Assert.Equal(1, count);
        }
    }
}
