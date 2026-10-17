using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using RetailInsights.Core.Kpis;

namespace RetailInsights.Infrastructure.Caching;

public sealed class KpiCacheOptions
{
    public const string SectionName = "KpiCache";

    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromMinutes(10);
}

/// <summary>
/// Cache-aside on <see cref="IDistributedCache"/> (Redis). Keys embed a data version:
/// invalidating everything is a single write (new version), no key scan is needed and
/// stale entries simply expire.
/// </summary>
internal sealed class DistributedKpiCache(IDistributedCache cache, IOptions<KpiCacheOptions> options) : IKpiCache
{
    internal const string VersionKey = "kpi:version";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<CachedResult<T>> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        var fullKey = $"kpi:{await VersionAsync(cancellationToken)}:{key}";

        var cached = await cache.GetAsync(fullKey, cancellationToken);
        if (cached is not null)
        {
            return new CachedResult<T>(JsonSerializer.Deserialize<T>(cached, Json)!, FromCache: true);
        }

        var value = await factory(cancellationToken);
        await cache.SetAsync(
            fullKey,
            JsonSerializer.SerializeToUtf8Bytes(value, Json),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = options.Value.TimeToLive },
            cancellationToken);
        return new CachedResult<T>(value, FromCache: false);
    }

    public Task InvalidateAllAsync(CancellationToken cancellationToken) =>
        cache.SetStringAsync(VersionKey, NewVersion(), cancellationToken);

    private async Task<string> VersionAsync(CancellationToken cancellationToken)
    {
        var version = await cache.GetStringAsync(VersionKey, cancellationToken);
        if (version is not null)
        {
            return version;
        }

        version = NewVersion();
        await cache.SetStringAsync(VersionKey, version, cancellationToken);
        return version;
    }

    private static string NewVersion() => Guid.NewGuid().ToString("N")[..12];
}
