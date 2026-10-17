using System.Net;
using System.Net.Http.Json;
using RetailInsights.Api.Controllers;
using RetailInsights.Core.Kpis;
using RetailInsights.Core.Sales;

namespace RetailInsights.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class KpiApiTests(InsightsApiFactory factory)
{
    private const string Week = "from=2026-03-02&to=2026-03-08";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Summary_aggregates_the_week_and_compares_with_the_previous_one()
    {
        var summary = await (await _client.GetAsync($"/api/kpis/summary?{Week}")).ReadAsync<KpiSummary>();

        Assert.Equal(155m, summary.Current.Revenue);
        Assert.Equal(4, summary.Current.Transactions);
        Assert.Equal(8, summary.Current.Units);
        Assert.Equal(38.75m, summary.Current.AverageBasket);
        Assert.Equal(2m, summary.Current.UnitsPerTransaction);
        Assert.Equal(100m, summary.Previous.Revenue);
        Assert.Equal(55.0m, summary.Growth.Revenue);
    }

    [Fact]
    public async Task Summary_can_be_restricted_to_one_store()
    {
        var summary = await (await _client.GetAsync($"/api/kpis/summary?{Week}&storeId={factory.ParisId}")).ReadAsync<KpiSummary>();

        Assert.Equal(70m, summary.Current.Revenue);
        Assert.Equal(-30.0m, summary.Growth.Revenue);
    }

    [Fact]
    public async Task Stores_are_ranked_by_revenue()
    {
        var stores = await (await _client.GetAsync($"/api/kpis/stores?{Week}")).ReadAsync<List<StorePerformance>>();

        Assert.Equal(new[] { "LYO", "PAR" }, stores.Select(s => s.Code));
        Assert.Equal(85m, stores[0].Revenue);
        Assert.Equal(54.8m, stores[0].ShareOfRevenue);
        Assert.Null(stores[0].RevenueGrowth);
    }

    [Fact]
    public async Task Top_products_by_revenue_and_by_units_differ()
    {
        var byRevenue = await (await _client.GetAsync($"/api/kpis/top-products?{Week}")).ReadAsync<List<ProductPerformance>>();
        var byUnits = await (await _client.GetAsync($"/api/kpis/top-products?{Week}&by=Units")).ReadAsync<List<ProductPerformance>>();

        Assert.Equal(new[] { "SNK-002", "TSH-001" }, byRevenue.Select(p => p.Sku));
        Assert.Equal(100m, byRevenue[0].Revenue);
        Assert.Equal(new[] { "TSH-001", "SNK-002" }, byUnits.Select(p => p.Sku));
        Assert.Equal(6, byUnits[0].Units);
        Assert.Equal(55m, byUnits[0].Revenue);
    }

    [Fact]
    public async Task Trend_is_continuous_and_supports_weekly_buckets()
    {
        var daily = await (await _client.GetAsync($"/api/kpis/trend?{Week}&granularity=Day")).ReadAsync<List<TrendPoint>>();
        var weekly = await (await _client.GetAsync($"/api/kpis/trend?{Week}&granularity=Week")).ReadAsync<List<TrendPoint>>();

        Assert.Equal(7, daily.Count);
        Assert.Equal(new[] { 70m, 0m, 60m, 0m, 0m, 0m, 25m }, daily.Select(p => p.Revenue));
        var week = Assert.Single(weekly);
        Assert.Equal(new DateOnly(2026, 3, 2), week.Period);
        Assert.Equal(155m, week.Revenue);
    }

    [Fact]
    public async Task Channels_split_the_revenue()
    {
        var channels = await (await _client.GetAsync($"/api/kpis/channels?{Week}")).ReadAsync<List<ChannelPerformance>>();

        Assert.Equal(80m, channels.Single(c => c.Channel == SalesChannel.InStore).Revenue);
        Assert.Equal(50m, channels.Single(c => c.Channel == SalesChannel.ClickAndCollect).Revenue);
        Assert.Equal(25m, channels.Single(c => c.Channel == SalesChannel.Online).Revenue);
    }

    [Fact]
    public async Task Invalid_period_returns_400()
    {
        var response = await _client.GetAsync("/api/kpis/summary?from=2026-03-08&to=2026-03-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Results_are_cached_in_redis_until_new_sales_arrive()
    {
        // A week without seeded data, so this test does not affect the others.
        const string april = "from=2026-04-06&to=2026-04-12";

        var first = await _client.GetAsync($"/api/kpis/summary?{april}");
        var second = await _client.GetAsync($"/api/kpis/summary?{april}");
        Assert.Equal("MISS", first.CacheStatus());
        Assert.Equal("HIT", second.CacheStatus());
        Assert.Equal(0m, (await second.ReadAsync<KpiSummary>()).Current.Revenue);

        var created = await _client.PostAsJsonAsync("/api/sales", new SaleInput(
            "T-APR-1", factory.LyonId, new DateTimeOffset(2026, 4, 7, 11, 0, 0, TimeSpan.Zero), SalesChannel.InStore,
            [new SaleLineInput("SNK-002", 2, 10m)]), InsightsApiFactory.Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(90m, (await created.ReadAsync<SaleReceipt>()).TotalAmount);

        var afterIngestion = await _client.GetAsync($"/api/kpis/summary?{april}");
        Assert.Equal("MISS", afterIngestion.CacheStatus());
        Assert.Equal(90m, (await afterIngestion.ReadAsync<KpiSummary>()).Current.Revenue);
    }

    [Fact]
    public async Task Sale_with_an_unknown_product_returns_422()
    {
        var response = await _client.PostAsJsonAsync("/api/sales", new SaleInput(
            "T-BAD", factory.LyonId, DateTimeOffset.UtcNow, SalesChannel.InStore, [new SaleLineInput("NOPE", 1, null)]), InsightsApiFactory.Json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_checks_postgresql_and_redis()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Reference_data_is_exposed_for_the_filters()
    {
        var stores = await (await _client.GetAsync("/api/stores")).ReadAsync<List<StoreInfo>>();
        var range = await (await _client.GetAsync("/api/data-range")).ReadAsync<DataRangeInfo>();

        Assert.Equal(2, stores.Count);
        Assert.Equal(new DateOnly(2026, 2, 25), range.FirstDay);
    }
}
