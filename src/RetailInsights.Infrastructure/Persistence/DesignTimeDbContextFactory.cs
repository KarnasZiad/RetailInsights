using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RetailInsights.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> to create migrations without starting the API.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<InsightsDbContext>
{
    public InsightsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<InsightsDbContext>()
            .UseNpgsql("Host=localhost;Database=retailinsights;Username=retailinsights;Password=retailinsights")
            .UseSnakeCaseNamingConvention()
            .Options);
}
