using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// Load environment-specific configuration (appsettings.{Environment}.json is loaded by default by the Host)
var env = builder.Environment.EnvironmentName;
Console.WriteLine("Environment: " + env);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health checks and metrics
builder.Services.AddHealthChecks();

// Note: Db context health check is supported via AddDbContextCheck extension in Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore package.
// For simplicity we rely on a basic probe here. You can add the package and call AddDbContextCheck for richer checks.

// Configure DbContext based on configuration
var dbConfig = builder.Configuration.GetSection("Database");
var useInMemory = dbConfig.GetValue<bool?>("UseInMemory") ?? false;
var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:Sqlite") ?? "Data Source=analytics.db";

if (useInMemory)
{
    builder.Services.AddDbContext<Coding.Challenge.API.Data.AnalyticsDbContext>(options =>
        options.UseInMemoryDatabase("analytics_inmemory"));
}
else
{
    builder.Services.AddDbContext<Coding.Challenge.API.Data.AnalyticsDbContext>(options =>
        options.UseSqlite(connectionString));
}

// Repository
builder.Services.AddScoped<Coding.Challenge.API.Services.ISensorEventRepository, Coding.Challenge.API.Services.SensorEventRepository>();

// In-memory channel to simulate message broker
var channel = System.Threading.Channels.Channel.CreateUnbounded<Coding.Challenge.API.Models.SensorEvent>();
builder.Services.AddSingleton(channel);

// Background producer and consumer
builder.Services.AddHostedService<Coding.Challenge.API.Services.SensorEventProducer>();
builder.Services.AddHostedService<Coding.Challenge.API.Services.SensorEventConsumer>();

// Retention cleanup and options
builder.Services.Configure<Coding.Challenge.API.Services.RetentionOptions>(builder.Configuration.GetSection("Retention"));
builder.Services.AddHostedService<Coding.Challenge.API.Services.RetentionCleanupService>();

WebApplication? app = null;
try
{
    app = builder.Build();
}
catch (Exception ex)
{
    // Emit full exception to console to help diagnose startup failures when running locally.
    Console.Error.WriteLine("Error building WebApplication: " + ex);
    throw;
}

// Ensure database exists and apply migrations if enabled via config
var enableMigrations = builder.Configuration.GetSection("Startup").GetValue<bool?>("EnableMigrationsOnStartup") ?? false;
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<Coding.Challenge.API.Data.AnalyticsDbContext>();

        if (enableMigrations && !useInMemory)
        {
            try
            {
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Migrations failed: " + ex + " - falling back to EnsureCreated");
                db.Database.EnsureCreated();
            }
        }
        else
        {
            // For non-production / in-memory scenarios we ensure DB is created for convenience in tests/dev
            db.Database.EnsureCreated();
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error initializing database: " + ex);
    throw;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

app.Run();
