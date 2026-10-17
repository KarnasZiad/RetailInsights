using Microsoft.EntityFrameworkCore;
using RetailInsights.Core.Kpis;
using RetailInsights.Core.Sales;

namespace RetailInsights.Infrastructure.Persistence;

/// <summary>Every query here is a single GROUP BY executed by PostgreSQL (no rows are loaded in memory).</summary>
internal sealed class SalesAggregates(InsightsDbContext db) : ISalesAggregates
{
    public async Task<IReadOnlyList<StoreInfo>> StoresAsync(CancellationToken cancellationToken) =>
        await db.Stores.AsNoTracking()
            .OrderBy(s => s.Code)
            .Select(s => new StoreInfo(s.Id, s.Code, s.Name, s.City))
            .ToListAsync(cancellationToken);

    public async Task<DataRangeInfo> DataRangeAsync(CancellationToken cancellationToken)
    {
        var range = await db.Sales.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new { First = g.Min(s => s.BusinessDate), Last = g.Max(s => s.BusinessDate) })
            .SingleOrDefaultAsync(cancellationToken);
        return range is null ? new DataRangeInfo(null, null) : new DataRangeInfo(range.First, range.Last);
    }

    public async Task<SalesTotals> TotalsAsync(DateRange period, int? storeId, CancellationToken cancellationToken)
    {
        var totals = await Sales(period, storeId)
            .GroupBy(_ => 1)
            .Select(g => new { Revenue = g.Sum(s => s.TotalAmount), Transactions = g.Count(), Units = g.Sum(s => s.TotalUnits) })
            .SingleOrDefaultAsync(cancellationToken);
        return totals is null ? SalesTotals.Empty : new SalesTotals(totals.Revenue, totals.Transactions, totals.Units);
    }

    public async Task<IReadOnlyDictionary<int, SalesTotals>> TotalsByStoreAsync(DateRange period, CancellationToken cancellationToken) =>
        await Sales(period, null)
            .GroupBy(s => s.StoreId)
            .Select(g => new { StoreId = g.Key, Revenue = g.Sum(s => s.TotalAmount), Transactions = g.Count(), Units = g.Sum(s => s.TotalUnits) })
            .ToDictionaryAsync(x => x.StoreId, x => new SalesTotals(x.Revenue, x.Transactions, x.Units), cancellationToken);

    public async Task<IReadOnlyDictionary<DateOnly, SalesTotals>> DailyTotalsAsync(DateRange period, int? storeId, CancellationToken cancellationToken) =>
        await Sales(period, storeId)
            .GroupBy(s => s.BusinessDate)
            .Select(g => new { Day = g.Key, Revenue = g.Sum(s => s.TotalAmount), Transactions = g.Count(), Units = g.Sum(s => s.TotalUnits) })
            .ToDictionaryAsync(x => x.Day, x => new SalesTotals(x.Revenue, x.Transactions, x.Units), cancellationToken);

    public async Task<IReadOnlyList<ProductTotals>> ProductTotalsAsync(DateRange period, int? storeId, ProductRanking ranking, int top, CancellationToken cancellationToken)
    {
        var lines = db.SaleLines.AsNoTracking()
            .Where(l => l.Sale.BusinessDate >= period.From && l.Sale.BusinessDate <= period.To);
        if (storeId is not null)
        {
            lines = lines.Where(l => l.Sale.StoreId == storeId);
        }

        var grouped = lines
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Units = g.Sum(l => l.Quantity), Revenue = g.Sum(l => l.NetAmount) });

        var ordered = ranking == ProductRanking.Units
            ? grouped.OrderByDescending(x => x.Units).ThenByDescending(x => x.Revenue)
            : grouped.OrderByDescending(x => x.Revenue).ThenByDescending(x => x.Units);

        var rows = await ordered
            .Take(top)
            .Join(db.Products, x => x.ProductId, p => p.Id, (x, p) => new ProductTotals(p.Id, p.Sku, p.Name, p.Category, x.Units, x.Revenue))
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<IReadOnlyDictionary<SalesChannel, SalesTotals>> ChannelTotalsAsync(DateRange period, int? storeId, CancellationToken cancellationToken) =>
        await Sales(period, storeId)
            .GroupBy(s => s.Channel)
            .Select(g => new { Channel = g.Key, Revenue = g.Sum(s => s.TotalAmount), Transactions = g.Count(), Units = g.Sum(s => s.TotalUnits) })
            .ToDictionaryAsync(x => x.Channel, x => new SalesTotals(x.Revenue, x.Transactions, x.Units), cancellationToken);

    private IQueryable<Sale> Sales(DateRange period, int? storeId)
    {
        var sales = db.Sales.AsNoTracking().Where(s => s.BusinessDate >= period.From && s.BusinessDate <= period.To);
        return storeId is null ? sales : sales.Where(s => s.StoreId == storeId);
    }
}
