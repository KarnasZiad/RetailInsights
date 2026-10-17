using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RetailInsights.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }

    public int Days { get; set; } = 180;
}

public sealed partial class DatabaseInitializer(
    InsightsDbContext db,
    IOptions<SeedOptions> options,
    TimeProvider clock,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (!options.Value.Enabled)
        {
            return;
        }

        var yesterday = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-1);
        await DemoDataGenerator.SeedAsync(db, options.Value.Days, yesterday, cancellationToken);
        LogSeeded(options.Value.Days, yesterday);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo sales history ready: {Days} days up to {LastDay}")]
    private partial void LogSeeded(int days, DateOnly lastDay);
}
