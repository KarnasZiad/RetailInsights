using RetailInsights.Core.Sales;

namespace RetailInsights.Core.Kpis;

public enum TrendGranularity
{
    Day,
    Week,
    Month,
}

public enum ProductRanking
{
    Revenue,
    Units,
}

/// <summary>Scope of every KPI query. <see cref="CacheKey"/> identifies it in the cache.</summary>
public sealed record KpiFilter(DateRange Period, int? StoreId)
{
    public string CacheKey => $"{Period}:{StoreId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "all"}";
}

public sealed record KpiFigures(
    decimal Revenue,
    int Transactions,
    int Units,
    decimal AverageBasket,
    decimal UnitsPerTransaction,
    decimal AverageSellingPrice)
{
    public static KpiFigures From(SalesTotals t) =>
        new(t.Revenue, t.Transactions, t.Units, t.AverageBasket, t.UnitsPerTransaction, t.AverageSellingPrice);
}

/// <summary>Growth in percent versus the previous period, null when there is nothing to compare with.</summary>
public sealed record KpiGrowth(
    decimal? Revenue,
    decimal? Transactions,
    decimal? AverageBasket,
    decimal? UnitsPerTransaction,
    decimal? AverageSellingPrice);

public sealed record KpiSummary(
    DateOnly From,
    DateOnly To,
    DateOnly PreviousFrom,
    DateOnly PreviousTo,
    KpiFigures Current,
    KpiFigures Previous,
    KpiGrowth Growth);

public sealed record StoreInfo(int Id, string Code, string Name, string City);

public sealed record StorePerformance(
    int StoreId,
    string Code,
    string Name,
    string City,
    decimal Revenue,
    int Transactions,
    decimal AverageBasket,
    decimal UnitsPerTransaction,
    decimal ShareOfRevenue,
    decimal? RevenueGrowth,
    int Rank);

public sealed record TrendPoint(DateOnly Period, decimal Revenue, int Transactions, decimal AverageBasket);

public sealed record ProductTotals(int ProductId, string Sku, string Name, string Category, int Units, decimal Revenue);

public sealed record ProductPerformance(
    int Rank,
    int ProductId,
    string Sku,
    string Name,
    string Category,
    int Units,
    decimal Revenue,
    decimal ShareOfRevenue);

public sealed record ChannelPerformance(SalesChannel Channel, decimal Revenue, int Transactions, decimal AverageBasket, decimal ShareOfRevenue);

public sealed record DataRangeInfo(DateOnly? FirstDay, DateOnly? LastDay);
