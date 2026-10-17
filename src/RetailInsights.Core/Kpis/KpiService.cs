namespace RetailInsights.Core.Kpis;

/// <summary>Performance indicators of the store network, cached (cache-aside) per query.</summary>
public sealed class KpiService(ISalesAggregates aggregates, IKpiCache cache, TimeProvider clock)
{
    public const int DefaultPeriodDays = 30;
    public const int MaxTop = 50;

    /// <summary>Missing bounds default to the last 30 days of available data.</summary>
    public async Task<DateRange> ResolvePeriodAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (from is not null && to is not null)
        {
            return DateRange.Create(from.Value, to.Value);
        }

        var end = to ?? (await aggregates.DataRangeAsync(cancellationToken)).LastDay ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var start = from ?? end.AddDays(-(DefaultPeriodDays - 1));
        return DateRange.Create(start, end);
    }

    public Task<CachedResult<KpiSummary>> SummaryAsync(KpiFilter filter, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            $"summary:{filter.CacheKey}",
            async ct =>
            {
                var current = await aggregates.TotalsAsync(filter.Period, filter.StoreId, ct);
                var previous = await aggregates.TotalsAsync(filter.Period.Previous(), filter.StoreId, ct);
                return KpiCalculator.Summary(filter.Period, current, previous);
            },
            cancellationToken);

    public Task<CachedResult<IReadOnlyList<StorePerformance>>> StoresAsync(DateRange period, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            $"stores:{period}",
            async ct =>
            {
                var stores = await aggregates.StoresAsync(ct);
                var current = await aggregates.TotalsByStoreAsync(period, ct);
                var previous = await aggregates.TotalsByStoreAsync(period.Previous(), ct);
                return KpiCalculator.Stores(stores, current, previous);
            },
            cancellationToken);

    public Task<CachedResult<IReadOnlyList<TrendPoint>>> TrendAsync(KpiFilter filter, TrendGranularity? granularity, CancellationToken cancellationToken)
    {
        var bucket = granularity ?? KpiCalculator.DefaultGranularity(filter.Period);
        return cache.GetOrCreateAsync(
            $"trend:{bucket}:{filter.CacheKey}",
            async ct => KpiCalculator.Trend(filter.Period, await aggregates.DailyTotalsAsync(filter.Period, filter.StoreId, ct), bucket),
            cancellationToken);
    }

    public Task<CachedResult<IReadOnlyList<ProductPerformance>>> TopProductsAsync(KpiFilter filter, ProductRanking ranking, int top, CancellationToken cancellationToken)
    {
        if (top is < 1 or > MaxTop)
        {
            throw new InvalidKpiQueryException($"'top' must be between 1 and {MaxTop}.");
        }

        return cache.GetOrCreateAsync(
            $"products:{ranking}:{top}:{filter.CacheKey}",
            async ct =>
            {
                var rows = await aggregates.ProductTotalsAsync(filter.Period, filter.StoreId, ranking, top, ct);
                var total = await aggregates.TotalsAsync(filter.Period, filter.StoreId, ct);
                return KpiCalculator.Products(rows, total.Revenue, ranking, top);
            },
            cancellationToken);
    }

    public Task<CachedResult<IReadOnlyList<ChannelPerformance>>> ChannelsAsync(KpiFilter filter, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            $"channels:{filter.CacheKey}",
            async ct => KpiCalculator.Channels(await aggregates.ChannelTotalsAsync(filter.Period, filter.StoreId, ct)),
            cancellationToken);
}
