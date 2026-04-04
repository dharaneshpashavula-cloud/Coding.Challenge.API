using System.Net;
using System.Net.Http.Json;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Coding.Challenge.API.Data;
using Coding.Challenge.API.Models;
using Coding.Challenge.API.Services;

namespace Coding.Challenge.API.Tests.Controllers
{
    public class AnalyticsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AnalyticsControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // Use in-memory database for tests via configuration override
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration and replace with InMemory for isolation
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AnalyticsDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<AnalyticsDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("test_db_integration");
                    });

                    // Register repository as scoped so the consumer resolves a scoped repo with its own DbContext
                    services.AddScoped<ISensorEventRepository, SensorEventRepository>();
                });
            });
        }

        [Fact]
        public async Task EndToEnd_PostIngested_PersistsAndAggregates()
        {
            var client = _factory.CreateClient();

            var payload = new
            {
                gate = "GateEndToEnd",
                timestamp = DateTime.UtcNow,
                numberOfPeople = 7,
                type = "enter"
            };

            // Post event - should be accepted and queued to the in-memory channel
            var postResponse = await client.PostAsJsonAsync("/api/sensorevents", payload);
            Assert.Equal(HttpStatusCode.Accepted, postResponse.StatusCode);

            // Poll the analytics endpoint until the consumer has persisted the event and aggregation appears
            int attempts = 0;
            const int maxAttempts = 50;
            int aggregatedSum = 0;

            while (attempts++ < maxAttempts)
            {
                var getResponse = await client.GetAsync($"/api/analytics?gate={Uri.EscapeDataString("GateEndToEnd")}&type=enter");
                getResponse.EnsureSuccessStatusCode();

                var arr = await getResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement[]>();
                if (arr != null && arr.Length > 0)
                {
                    var elem = arr[0];
                    if (elem.TryGetProperty("numberOfPeople", out var numProp) && numProp.TryGetInt32(out aggregatedSum))
                        break;
                }

                await Task.Delay(100);
            }

            Assert.Equal(7, aggregatedSum);
        }

        [Fact]
        public async Task Get_ReturnsOk()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/analytics");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_ReturnsAggregatedResults_WhenDataExists()
        {
            // Arrange - seed in-memory DB
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
                db.SensorEvents.AddRange(
                    new SensorEvent { Gate = "Gate A", Type = "enter", NumberOfPeople = 2, Timestamp = DateTime.UtcNow.AddMinutes(-5) },
                    new SensorEvent { Gate = "Gate A", Type = "enter", NumberOfPeople = 3, Timestamp = DateTime.UtcNow.AddMinutes(-1) },
                    new SensorEvent { Gate = "Gate B", Type = "leave", NumberOfPeople = 4, Timestamp = DateTime.UtcNow.AddMinutes(-2) }
                );
                await db.SaveChangesAsync();
            }

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetFromJsonAsync<object[]>("/api/analytics?gate=Gate A&type=enter");

            // Assert
            Assert.NotNull(response);
            Assert.Single(response);

            // Basic check - response contains the expected aggregated sum (2 + 3 = 5)
            var json = System.Text.Json.JsonSerializer.Serialize(response);
            Assert.Contains("5", json);
        }
    }
}
