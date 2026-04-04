using Coding.Challenge.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Coding.Challenge.API.Data
{
    /// <summary>
    /// EF Core DbContext for storing sensor events.
    /// Uses SQLite in production and tests for simplicity.
    /// </summary>
    public class AnalyticsDbContext : DbContext
    {
        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
        {
        }

        public DbSet<SensorEvent> SensorEvents { get; set; } = null!;
        public DbSet<DeadLetter> DeadLetters { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SensorEvent>()
                .HasIndex(e => new { e.Gate, e.Type });

            modelBuilder.Entity<DeadLetter>()
                .HasIndex(d => d.Timestamp);

            base.OnModelCreating(modelBuilder);
        }
    }
}
