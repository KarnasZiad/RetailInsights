using RetailInsights.Core.Sales;

namespace RetailInsights.Core.Kpis;

/// <summary>
/// Pure business rules of the performance indicators. The database only sums and counts;
/// ratios, comparisons, rankings and time buckets are computed here (fully unit-tested).
/// </summary>
public static class KpiCalculator
{
    public static decimal? Growth(decimal current, decimal previous) =>
        previous == 0m ? null : decimal.Round((current - previous) / previous * 100m, 1, MidpointRounding.AwayFromZero);

    public static decimal Share(decimal part, decimal total) =>
        total == 0m ? 0m : decimal.Round(part / total * 100m, 1, MidpointRounding.AwayFromZero);

    public static KpiSummary Summary(DateRange period, SalesTotals current, SalesTotals previous)
    {
        var previousPeriod = period.Previous();
        return new KpiSummary(
            period.From,
            period.To,
            previousPeriod.From,
            previousPeriod.To,
            KpiFigures.From(current),
            KpiFigures.From(previous),
            new KpiGrowth(
                Growth(current.Revenue, previous.Revenue),
                Growth(current.Transactions, previous.Transactions),
                Growth(current.AverageBasket, previous.AverageBasket),
                Growth(current.UnitsPerTransaction, previous.UnitsPerTransaction),
                Growth(current.AverageSellingPrice, previous.AverageSellingPrice)));
    }

    /// <summary>Store ranking by revenue. Stores without sales are listed with zero.</summary>
    public static IReadOnlyList<StorePerformance> Stores(
        IReadOnlyCollection<StoreInfo> stores,
        IReadOnlyDictionary<int, SalesTotals> current,
        IReadOnlyDictionary<int, SalesTotals> previous)
    {
        var total = current.Values.Sum(t => t.Revenue);
        return stores
            .Select(s => (Store: s, Current: current.GetValueOrDefault(s.Id, SalesTotals.Empty), Previous: previous.GetValueOrDefault(s.Id, SalesTotals.Empty)))
            .OrderByDescending(x => x.Current.Revenue)
            .ThenBy(x => x.Store.Code, StringComparer.Ordinal)
            .Select((x, index) => new StorePerformance(
                x.Store.Id,
                x.Store.Code,
                x.Store.Name,
                x.Store.City,
                x.Current.Revenue,
                x.Current.Transactions,
                x.Current.AverageBasket,
                x.Current.UnitsPerTransaction,
                Share(x.Current.Revenue, total),
                Growth(x.Current.Revenue, x.Previous.Revenue),
                index + 1))
            .ToList();
    }

    /// <summary>Continuous time series: days without sales are zeros, never holes.</summary>
    public static IReadOnlyList<TrendPoint> Trend(DateRange period, IReadOnlyDictionary<DateOnly, SalesTotals> daily, TrendGranularity granularity)
    {
        var buckets = new SortedDictionary<DateOnly, SalesTotals>();
        for (var day = period.From; day <= period.To; day = day.AddDays(1))
        {
            var bucket = BucketStart(day, granularity);
            var totals = daily.GetValueOrDefault(day, SalesTotals.Empty);
            buckets[bucket] = buckets.TryGetValue(bucket, out var existing) ? existing + totals : totals;
        }

        return buckets.Select(b => new TrendPoint(b.Key, b.Value.Revenue, b.Value.Transactions, b.Value.AverageBasket)).ToList();
    }

    public static DateOnly BucketStart(DateOnly day, TrendGranularity granularity) => granularity switch
    {
        // ISO weeks start on Monday
        TrendGranularity.Week => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
        TrendGranularity.Month => new DateOnly(day.Year, day.Month, 1),
        _ => day,
    };

    public static IReadOnlyList<ProductPerformance> Products(IEnumerable<ProductTotals> rows, decimal totalRevenue, ProductRanking ranking, int top)
    {
        var ordered = ranking == ProductRanking.Units
            ? rows.OrderByDescending(r => r.Units).ThenByDescending(r => r.Revenue)
            : rows.OrderByDescending(r => r.Revenue).ThenByDescending(r => r.Units);

        return ordered
            .ThenBy(r => r.Sku, StringComparer.Ordinal)
            .Take(top)
            .Select((r, index) => new ProductPerformance(index + 1, r.ProductId, r.Sku, r.Name, r.Category, r.Units, r.Revenue, Share(r.Revenue, totalRevenue)))
            .ToList();
    }

    public static IReadOnlyList<ChannelPerformance> Channels(IReadOnlyDictionary<SalesChannel, SalesTotals> totals)
    {
        var total = totals.Values.Sum(t => t.Revenue);
        return Enum.GetValues<SalesChannel>()
            .Select(channel => (Channel: channel, Totals: totals.GetValueOrDefault(channel, SalesTotals.Empty)))
            .Select(x => new ChannelPerformance(x.Channel, x.Totals.Revenue, x.Totals.Transactions, x.Totals.AverageBasket, Share(x.Totals.Revenue, total)))
            .ToList();
    }

    /// <summary>Default granularity: daily up to two months, weekly up to six months, monthly beyond.</summary>
    public static TrendGranularity DefaultGranularity(DateRange period) => period.Days switch
    {
        <= 62 => TrendGranularity.Day,
        <= 186 => TrendGranularity.Week,
        _ => TrendGranularity.Month,
    };
}
