using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RetailInsights.Core.Kpis;
using RetailInsights.Core.Sales;
using RetailInsights.Infrastructure.Caching;
using RetailInsights.Infrastructure.Persistence;

namespace RetailInsights.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InsightsDbContext>((sp, options) => options
            .UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("RetailInsights")
                ?? throw new InvalidOperationException("Connection string 'RetailInsights' is not configured."))
            .UseSnakeCaseNamingConvention());

        // Redis when configured (docker compose, Azure); in-process cache otherwise (local run without Redis).
        // Resolved lazily so that the final configuration (environment, tests) decides.
        services.AddSingleton<IDistributedCache>(sp =>
        {
            var redis = sp.GetRequiredService<IConfiguration>().GetConnectionString("Redis");
            return string.IsNullOrWhiteSpace(redis)
                ? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()))
                : new RedisCache(Options.Create(new RedisCacheOptions { Configuration = redis, InstanceName = "retailinsights:" }));
        });

        services.Configure<KpiCacheOptions>(configuration.GetSection(KpiCacheOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        services.AddScoped<ISalesAggregates, SalesAggregates>();
        services.AddScoped<ISalesWriter, SalesWriter>();
        services.AddSingleton<IKpiCache, DistributedKpiCache>();
        services.AddScoped<DatabaseInitializer>();

        services.AddHealthChecks()
            .AddDbContextCheck<InsightsDbContext>("postgresql", tags: ["ready"])
            .AddCheck<DistributedCacheHealthCheck>("cache", HealthStatus.Unhealthy, tags: ["ready"]);

        return services;
    }
}
