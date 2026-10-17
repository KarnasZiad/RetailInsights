using Microsoft.EntityFrameworkCore;
using RetailInsights.Core.Sales;

namespace RetailInsights.Infrastructure.Persistence;

internal sealed class SalesWriter(InsightsDbContext db) : ISalesWriter
{
    public Task<bool> StoreExistsAsync(int storeId, CancellationToken cancellationToken) =>
        db.Stores.AnyAsync(s => s.Id == storeId, cancellationToken);

    public async Task<IReadOnlyDictionary<string, Product>> ProductsBySkuAsync(IEnumerable<string> skus, CancellationToken cancellationToken)
    {
        var wanted = skus.Distinct().ToList();
        return await db.Products.AsNoTracking().Where(p => wanted.Contains(p.Sku)).ToDictionaryAsync(p => p.Sku, cancellationToken);
    }

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken)
    {
        db.Sales.Add(sale);
        await db.SaveChangesAsync(cancellationToken);
    }
}
