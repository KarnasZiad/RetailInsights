using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RetailInsights.Core.Kpis;
using RetailInsights.Core.Sales;
using RetailInsights.Infrastructure.Caching;

namespace RetailInsights.UnitTests;

internal sealed class CountingAggregates : ISalesAggregates
{
    public int Calls { get; private set; }

    public SalesTotals Totals { get; set; } = new(155m, 4, 8);

    public DateOnly? LastDay { get; set; } = new DateOnly(2026, 3, 8);

    public Task<IReadOnlyList<StoreInfo>> StoresAsync(CancellationToken cancellationToken) =>
        Count<IReadOnlyList<StoreInfo>>([new StoreInfo(1, "PAR", "Paris", "Paris")]);

    public Task<DataRangeInfo> DataRangeAsync(CancellationToken cancellationToken) => Task.FromResult(new DataRangeInfo(null, LastDay));

    public Task<SalesTotals> TotalsAsync(DateRange period, int? storeId, CancellationToken cancellationToken) => Count(Totals);

    public Task<IReadOnlyDictionary<int, SalesTotals>> TotalsByStoreAsync(DateRange period, CancellationToken cancellationToken) =>
        Count<IReadOnlyDictionary<int, SalesTotals>>(new Dictionary<int, SalesTotals> { [1] = Totals });

    public Task<IReadOnlyDictionary<DateOnly, SalesTotals>> DailyTotalsAsync(DateRange period, int? storeId, CancellationToken cancellationToken) =>
        Count<IReadOnlyDictionary<DateOnly, SalesTotals>>(new Dictionary<DateOnly, SalesTotals> { [period.From] = Totals });

    public Task<IReadOnlyList<ProductTotals>> ProductTotalsAsync(DateRange period, int? storeId, ProductRanking ranking, int top, CancellationToken cancellationToken) =>
        Count<IReadOnlyList<ProductTotals>>([new ProductTotals(1, "TSH", "T-shirt", "Textile", 8, 155m)]);

    public Task<IReadOnlyDictionary<SalesChannel, SalesTotals>> ChannelTotalsAsync(DateRange period, int? storeId, CancellationToken cancellationToken) =>
        Count<IReadOnlyDictionary<SalesChannel, SalesTotals>>(new Dictionary<SalesChannel, SalesTotals> { [SalesChannel.InStore] = Totals });

    private Task<T> Count<T>(T value)
    {
        Calls++;
        return Task.FromResult(value);
    }
}

public sealed class KpiServiceTests
{
    private static readonly KpiFilter Filter = new(DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 8)), null);

    private readonly CountingAggregates _aggregates = new();
    private readonly IKpiCache _cache = new DistributedKpiCache(
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
        Options.Create(new KpiCacheOptions()));

    private KpiService Service => new(_aggregates, _cache, TimeProvider.System);

    [Fact]
    public async Task Second_identical_query_is_served_from_the_cache()
    {
        var first = await Service.SummaryAsync(Filter, CancellationToken.None);
        var callsAfterFirst = _aggregates.Calls;
        var second = await Service.SummaryAsync(Filter, CancellationToken.None);

        Assert.False(first.FromCache);
        Assert.True(second.FromCache);
        Assert.Equal(callsAfterFirst, _aggregates.Calls);
        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public async Task Cache_entries_are_scoped_to_the_query()
    {
        await Service.SummaryAsync(Filter, CancellationToken.None);

        var otherStore = await Service.SummaryAsync(Filter with { StoreId = 2 }, CancellationToken.None);
        var otherPeriod = await Service.SummaryAsync(Filter with { Period = Filter.Period.Previous() }, CancellationToken.None);
        var otherKind = await Service.ChannelsAsync(Filter, CancellationToken.None);

        Assert.False(otherStore.FromCache);
        Assert.False(otherPeriod.FromCache);
        Assert.False(otherKind.FromCache);
    }

    [Fact]
    public async Task Invalidation_makes_every_entry_stale()
    {
        await Service.SummaryAsync(Filter, CancellationToken.None);
        await Service.TrendAsync(Filter, TrendGranularity.Day, CancellationToken.None);
        _aggregates.Totals = new SalesTotals(200m, 5, 10);

        await _cache.InvalidateAllAsync(CancellationToken.None);
        var summary = await Service.SummaryAsync(Filter, CancellationToken.None);
        var trend = await Service.TrendAsync(Filter, TrendGranularity.Day, CancellationToken.None);

        Assert.False(summary.FromCache);
        Assert.False(trend.FromCache);
        Assert.Equal(200m, summary.Value.Current.Revenue);
    }

    [Fact]
    public async Task Default_period_is_the_last_30_days_of_data()
    {
        var period = await Service.ResolvePeriodAsync(null, null, CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 3, 8), period.To);
        Assert.Equal(30, period.Days);
    }

    [Fact]
    public async Task Explicit_period_is_used_as_is()
    {
        var period = await Service.ResolvePeriodAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), CancellationToken.None);

        Assert.Equal(31, period.Days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task Top_must_stay_within_bounds(int top) =>
        await Assert.ThrowsAsync<InvalidKpiQueryException>(() => Service.TopProductsAsync(Filter, ProductRanking.Revenue, top, CancellationToken.None));

    [Fact]
    public async Task Cached_values_survive_serialization()
    {
        await Service.StoresAsync(Filter.Period, CancellationToken.None);
        var cached = await Service.StoresAsync(Filter.Period, CancellationToken.None);

        Assert.True(cached.FromCache);
        Assert.Equal("PAR", Assert.Single(cached.Value).Code);
    }
}

public sealed class SalesIngestionTests
{
    private sealed class FakeWriter : ISalesWriter
    {
        public List<Sale> Added { get; } = [];

        public Task<bool> StoreExistsAsync(int storeId, CancellationToken cancellationToken) => Task.FromResult(storeId == 1);

        public Task<IReadOnlyDictionary<string, Product>> ProductsBySkuAsync(IEnumerable<string> skus, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, Product>>(new Dictionary<string, Product> { ["TSH-001"] = new("TSH-001", "T-shirt", "Textile", 19.90m) });

        public Task AddAsync(Sale sale, CancellationToken cancellationToken)
        {
            Added.Add(sale);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingCache : IKpiCache
    {
        public int Invalidations { get; private set; }

        public Task<CachedResult<T>> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task InvalidateAllAsync(CancellationToken cancellationToken)
        {
            Invalidations++;
            return Task.CompletedTask;
        }
    }

    private readonly FakeWriter _writer = new();
    private readonly RecordingCache _cache = new();

    private static SaleInput Input(int store = 1, string sku = "TSH-001") =>
        new("T-42", store, DateTimeOffset.UtcNow, SalesChannel.InStore, [new SaleLineInput(sku, 2, null)]);

    [Fact]
    public async Task Ingested_sale_uses_catalogue_prices_and_invalidates_the_cache()
    {
        var sale = await new SalesIngestionService(_writer, _cache).IngestAsync(Input(), CancellationToken.None);

        Assert.Equal(39.80m, sale.TotalAmount);
        Assert.Single(_writer.Added);
        Assert.Equal(1, _cache.Invalidations);
    }

    [Fact]
    public async Task Unknown_store_or_product_is_rejected_without_side_effect()
    {
        var service = new SalesIngestionService(_writer, _cache);

        await Assert.ThrowsAsync<SalesValidationException>(() => service.IngestAsync(Input(store: 99), CancellationToken.None));
        await Assert.ThrowsAsync<SalesValidationException>(() => service.IngestAsync(Input(sku: "NOPE"), CancellationToken.None));
        Assert.Empty(_writer.Added);
        Assert.Equal(0, _cache.Invalidations);
    }
}
