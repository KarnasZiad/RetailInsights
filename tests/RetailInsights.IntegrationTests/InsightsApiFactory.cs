using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetailInsights.Core.Sales;
using RetailInsights.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace RetailInsights.IntegrationTests;

/// <summary>
/// Real API against throw-away PostgreSQL and Redis containers, loaded with a small dataset
/// whose KPIs are computed by hand in the tests.
/// </summary>
public sealed class InsightsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder().WithImage("redis:7-alpine").Build();

    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public int ParisId { get; private set; }

    public int LyonId { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        // Starting the server applies the migrations.
        _ = Server;
        await SeedAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:RetailInsights"] = _postgres.GetConnectionString(),
            ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
            ["Seed:Enabled"] = "false",
        }));
    }

    /// <summary>
    /// Week 2026-03-02 (Mon) .. 2026-03-08 (Sun), plus one sale in the previous week.
    /// </summary>
    private async Task SeedAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

        var paris = new Store("PAR", "Paris", "Paris", "IDF");
        var lyon = new Store("LYO", "Lyon", "Lyon", "ARA");
        var tshirt = new Product("TSH-001", "T-shirt", "Textile", 10m);
        var sneakers = new Product("SNK-002", "Sneakers", "Chaussures", 50m);
        db.AddRange(paris, lyon, tshirt, sneakers);
        await db.SaveChangesAsync();

        static DateTimeOffset At(int month, int day) => new(2026, month, day, 14, 0, 0, TimeSpan.Zero);

        db.Sales.AddRange(
            Sale.Create("T-1", paris.Id, At(3, 2), SalesChannel.InStore, [new SaleLine(tshirt.Id, 2, 10m)]),
            Sale.Create("T-2", paris.Id, At(3, 2), SalesChannel.ClickAndCollect, [new SaleLine(sneakers.Id, 1, 50m)]),
            Sale.Create("T-3", lyon.Id, At(3, 4), SalesChannel.InStore, [new SaleLine(tshirt.Id, 1, 10m), new SaleLine(sneakers.Id, 1, 50m)]),
            Sale.Create("T-4", lyon.Id, At(3, 8), SalesChannel.Online, [new SaleLine(tshirt.Id, 3, 10m, discount: 5m)]),
            Sale.Create("T-0", paris.Id, At(2, 25), SalesChannel.InStore, [new SaleLine(sneakers.Id, 2, 50m)]));
        await db.SaveChangesAsync();

        ParisId = paris.Id;
        LyonId = lyon.Id;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<InsightsApiFactory>
{
    public const string Name = "api";
}

internal static class HttpExtensions
{
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<T>(InsightsApiFactory.Json);
        Assert.NotNull(value);
        return value;
    }

    public static string CacheStatus(this HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-Cache", out var values) ? values.Single() : "none";
}
