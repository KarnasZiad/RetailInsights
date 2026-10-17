using RetailInsights.Core.Kpis;

namespace RetailInsights.Core.Sales;

public sealed record SaleLineInput(string Sku, int Quantity, decimal? Discount);

public sealed record SaleInput(string TicketNumber, int StoreId, DateTimeOffset SoldAt, SalesChannel Channel, IReadOnlyList<SaleLineInput> Lines);

public interface ISalesWriter
{
    Task<bool> StoreExistsAsync(int storeId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, Product>> ProductsBySkuAsync(IEnumerable<string> skus, CancellationToken cancellationToken);

    Task AddAsync(Sale sale, CancellationToken cancellationToken);
}

/// <summary>Receives a ticket from a till (or an e-commerce order) and invalidates the cached KPIs.</summary>
public sealed class SalesIngestionService(ISalesWriter writer, IKpiCache cache)
{
    public async Task<Sale> IngestAsync(SaleInput input, CancellationToken cancellationToken)
    {
        if (!await writer.StoreExistsAsync(input.StoreId, cancellationToken))
        {
            throw new SalesValidationException($"Unknown store {input.StoreId}.");
        }

        var products = await writer.ProductsBySkuAsync(input.Lines.Select(l => l.Sku), cancellationToken);
        var unknown = input.Lines.Select(l => l.Sku).Where(sku => !products.ContainsKey(sku)).ToList();
        if (unknown.Count > 0)
        {
            throw new SalesValidationException($"Unknown products: {string.Join(", ", unknown)}.");
        }

        var sale = Sale.Create(
            input.TicketNumber,
            input.StoreId,
            input.SoldAt,
            input.Channel,
            input.Lines.Select(l => new SaleLine(products[l.Sku].Id, l.Quantity, products[l.Sku].Price, l.Discount ?? 0m)));

        await writer.AddAsync(sale, cancellationToken);
        await cache.InvalidateAllAsync(cancellationToken);
        return sale;
    }
}
