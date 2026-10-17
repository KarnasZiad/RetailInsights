using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RetailInsights.Infrastructure.Caching;

/// <summary>Round-trip on the distributed cache (Redis).</summary>
internal sealed class DistributedCacheHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var probe = Guid.NewGuid().ToString("N");
            await cache.SetStringAsync("health:probe", probe, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) }, cancellationToken);
            var read = await cache.GetStringAsync("health:probe", cancellationToken);
            return read == probe ? HealthCheckResult.Healthy() : HealthCheckResult.Degraded("Cache returned an unexpected value.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache unreachable.", ex);
        }
    }
}
