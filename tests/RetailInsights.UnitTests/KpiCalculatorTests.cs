using RetailInsights.Core.Kpis;
using RetailInsights.Core.Sales;

namespace RetailInsights.UnitTests;

public sealed class KpiCalculatorTests
{
    private static readonly DateRange Week = DateRange.Create(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 8));

    [Fact]
    public void Derived_figures_are_computed_from_the_totals()
    {
        var totals = new SalesTotals(1_000m, 40, 100);

        Assert.Equal(25m, totals.AverageBasket);
        Assert.Equal(2.5m, totals.UnitsPerTransaction);
        Assert.Equal(10m, totals.AverageSellingPrice);
    }

    [Fact]
    public void Derived_figures_are_zero_without_sales()
    {
        Assert.Equal(0m, SalesTotals.Empty.AverageBasket);
        Assert.Equal(0m, SalesTotals.Empty.UnitsPerTransaction);
        Assert.Equal(0m, SalesTotals.Empty.AverageSellingPrice);
    }

    [Theory]
    [InlineData(155, 100, 55.0)]
    [InlineData(70, 100, -30.0)]
    [InlineData(100, 300, -66.7)]
    public void Growth_is_a_rounded_percentage(decimal current, decimal previous, decimal expected) =>
        Assert.Equal(expected, KpiCalculator.Growth(current, previous));

    [Fact]
    public void Growth_is_undefined_without_a_comparison_basis() =>
        Assert.Null(KpiCalculator.Growth(42m, 0m));

    [Fact]
    public void Summary_compares_with_the_previous_period()
    {
        var summary = KpiCalculator.Summary(Week, new SalesTotals(155m, 4, 8), new SalesTotals(100m, 1, 2));

        Assert.Equal(new DateOnly(2026, 2, 23), summary.PreviousFrom);
        Assert.Equal(38.75m, summary.Current.AverageBasket);
        Assert.Equal(2m, summary.Current.UnitsPerTransaction);
        Assert.Equal(55.0m, summary.Growth.Revenue);
        Assert.Equal(300.0m, summary.Growth.Transactions);
        Assert.Equal(-61.3m, summary.Growth.AverageBasket);
    }

    [Fact]
    public void Stores_are_ranked_by_revenue_with_their_share_and_growth()
    {
        var stores = new[] { new StoreInfo(1, "PAR", "Paris", "Paris"), new StoreInfo(2, "LYO", "Lyon", "Lyon"), new StoreInfo(3, "LIL", "Lille", "Lille") };
        var current = new Dictionary<int, SalesTotals> { [1] = new(70m, 2, 3), [2] = new(85m, 2, 5) };
        var previous = new Dictionary<int, SalesTotals> { [1] = new(100m, 1, 2) };

        var ranking = KpiCalculator.Stores(stores, current, previous);

        Assert.Equal(new[] { "LYO", "PAR", "LIL" }, ranking.Select(s => s.Code));
        Assert.Equal(new[] { 1, 2, 3 }, ranking.Select(s => s.Rank));
        Assert.Equal(54.8m, ranking[0].ShareOfRevenue);
        Assert.Null(ranking[0].RevenueGrowth);
        Assert.Equal(-30.0m, ranking[1].RevenueGrowth);
        Assert.Equal(0m, ranking[2].Revenue);
    }

    [Fact]
    public void Daily_trend_has_no_holes()
    {
        var daily = new Dictionary<DateOnly, SalesTotals>
        {
            [new DateOnly(2026, 3, 2)] = new(70m, 2, 3),
            [new DateOnly(2026, 3, 8)] = new(25m, 1, 3),
        };

        var trend = KpiCalculator.Trend(Week, daily, TrendGranularity.Day);

        Assert.Equal(7, trend.Count);
        Assert.Equal(70m, trend[0].Revenue);
        Assert.All(trend.Skip(1).Take(5), p => Assert.Equal(0m, p.Revenue));
        Assert.Equal(25m, trend[^1].Revenue);
    }

    [Fact]
    public void Weekly_trend_buckets_on_iso_weeks()
    {
        var period = DateRange.Create(new DateOnly(2026, 3, 4), new DateOnly(2026, 3, 10)); // Wednesday -> Tuesday
        var daily = new Dictionary<DateOnly, SalesTotals>
        {
            [new DateOnly(2026, 3, 4)] = new(10m, 1, 1),
            [new DateOnly(2026, 3, 8)] = new(20m, 1, 1),
            [new DateOnly(2026, 3, 9)] = new(30m, 2, 2),
        };

        var trend = KpiCalculator.Trend(period, daily, TrendGranularity.Week);

        Assert.Equal(new[] { new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 9) }, trend.Select(p => p.Period));
        Assert.Equal(new[] { 30m, 30m }, trend.Select(p => p.Revenue));
        Assert.Equal(15m, trend[1].AverageBasket);
    }

    [Theory]
    [InlineData("2026-03-01", TrendGranularity.Week, "2026-02-23")] // Sunday belongs to the week started on Monday
    [InlineData("2026-03-02", TrendGranularity.Week, "2026-03-02")]
    [InlineData("2026-03-17", TrendGranularity.Month, "2026-03-01")]
    [InlineData("2026-03-17", TrendGranularity.Day, "2026-03-17")]
    public void Bucket_start(string day, TrendGranularity granularity, string expected) =>
        Assert.Equal(DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), KpiCalculator.BucketStart(DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture), granularity));

    [Theory]
    [InlineData(7, TrendGranularity.Day)]
    [InlineData(62, TrendGranularity.Day)]
    [InlineData(90, TrendGranularity.Week)]
    [InlineData(365, TrendGranularity.Month)]
    public void Default_granularity_depends_on_the_period_length(int days, TrendGranularity expected)
    {
        var period = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1).AddDays(days - 1));

        Assert.Equal(expected, KpiCalculator.DefaultGranularity(period));
    }

    [Fact]
    public void Top_products_can_be_ranked_by_revenue_or_units()
    {
        var rows = new[]
        {
            new ProductTotals(1, "TSH", "T-shirt", "Textile", 6, 55m),
            new ProductTotals(2, "SNK", "Sneakers", "Chaussures", 2, 100m),
            new ProductTotals(3, "CAS", "Casquette", "Accessoires", 1, 5m),
        };

        var byRevenue = KpiCalculator.Products(rows, 160m, ProductRanking.Revenue, top: 2);
        var byUnits = KpiCalculator.Products(rows, 160m, ProductRanking.Units, top: 3);

        Assert.Equal(new[] { "SNK", "TSH" }, byRevenue.Select(p => p.Sku));
        Assert.Equal(62.5m, byRevenue[0].ShareOfRevenue);
        Assert.Equal(new[] { "TSH", "SNK", "CAS" }, byUnits.Select(p => p.Sku));
        Assert.Equal(new[] { 1, 2, 3 }, byUnits.Select(p => p.Rank));
    }

    [Fact]
    public void Every_channel_is_reported_even_without_sales()
    {
        var channels = KpiCalculator.Channels(new Dictionary<SalesChannel, SalesTotals>
        {
            [SalesChannel.InStore] = new(80m, 2, 3),
            [SalesChannel.Online] = new(20m, 1, 1),
        });

        Assert.Equal(3, channels.Count);
        Assert.Equal(80.0m, channels.Single(c => c.Channel == SalesChannel.InStore).ShareOfRevenue);
        Assert.Equal(0m, channels.Single(c => c.Channel == SalesChannel.ClickAndCollect).Revenue);
        Assert.Equal(100m, channels.Sum(c => c.ShareOfRevenue));
    }
}
