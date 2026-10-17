using RetailInsights.Core.Sales;

namespace RetailInsights.Core.Kpis;

/// <summary>Aggregations executed by the database (SUM / COUNT / GROUP BY).</summary>
public interface ISalesAggregates
{
    Task<IReadOnlyList<StoreInfo>> StoresAsync(CancellationToken cancellationToken);

    Task<DataRangeInfo> DataRangeAsync(CancellationToken cancellationToken);

    Task<SalesTotals> TotalsAsync(DateRange period, int? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, SalesTotals>> TotalsByStoreAsync(DateRange period, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<DateOnly, SalesTotals>> DailyTotalsAsync(DateRange period, int? storeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductTotals>> ProductTotalsAsync(DateRange period, int? storeId, ProductRanking ranking, int top, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<SalesChannel, SalesTotals>> ChannelTotalsAsync(DateRange period, int? storeId, CancellationToken cancellationToken);
}

public sealed record CachedResult<T>(T Value, bool FromCache);

/// <summary>Cache-aside store for computed KPIs (Redis in production).</summary>
public interface IKpiCache
{
    Task<CachedResult<T>> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken);

    /// <summary>Makes every cached KPI stale (new sales were ingested).</summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken);
}
