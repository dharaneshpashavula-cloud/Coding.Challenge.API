using Coding.Challenge.API.Data;
using Coding.Challenge.API.Models;
using Coding.Challenge.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Coding.Challenge.API.Tests.Repository
{
    public class SensorEventRepositoryTests
    {
        private AnalyticsDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
                .UseInMemoryDatabase(databaseName: "test_db_" + System.Guid.NewGuid())
                .Options;

            return new AnalyticsDbContext(options);
        }

        [Fact]
        public async Task AddAsync_PersistsEvent()
        {
            var db = CreateDbContext();
            var repo = new SensorEventRepository(db);

            var evt = new SensorEvent { Gate = "Gate A", Timestamp = System.DateTime.UtcNow, NumberOfPeople = 5, Type = "enter" };

            await repo.AddAsync(evt);

            var count = await repo.CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task QueryAsync_FiltersByGateAndTypeAndTimeRange()
        {
            var db = CreateDbContext();
            var repo = new SensorEventRepository(db);

            var now = System.DateTime.UtcNow;
            await repo.AddAsync(new SensorEvent { Gate = "Gate A", Timestamp = now.AddMinutes(-10), NumberOfPeople = 2, Type = "enter" });
            await repo.AddAsync(new SensorEvent { Gate = "Gate B", Timestamp = now.AddMinutes(-5), NumberOfPeople = 3, Type = "leave" });
            await repo.AddAsync(new SensorEvent { Gate = "Gate A", Timestamp = now.AddMinutes(-1), NumberOfPeople = 4, Type = "enter" });

            var results = await repo.QueryAsync(gate: "Gate A", type: "enter", start: now.AddMinutes(-6));

            Assert.Single(results);
        }
    }
}
