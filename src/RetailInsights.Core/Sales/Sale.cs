namespace RetailInsights.Core.Sales;

/// <summary>A till receipt (ticket). Totals are denormalised so that KPIs aggregate without joins.</summary>
public sealed class Sale
{
    private readonly List<SaleLine> _lines = [];

    private Sale()
    {
    }

    public long Id { get; private set; }

    public string TicketNumber { get; private set; } = string.Empty;

    public int StoreId { get; private set; }

    public DateOnly BusinessDate { get; private set; }

    public DateTimeOffset SoldAt { get; private set; }

    public SalesChannel Channel { get; private set; }

    public decimal TotalAmount { get; private set; }

    public int TotalUnits { get; private set; }

    public IReadOnlyCollection<SaleLine> Lines => _lines;

    public static Sale Create(string ticketNumber, int storeId, DateTimeOffset soldAt, SalesChannel channel, IEnumerable<SaleLine> lines)
    {
        var sale = new Sale
        {
            TicketNumber = string.IsNullOrWhiteSpace(ticketNumber) ? throw new SalesValidationException("A ticket number is required.") : ticketNumber.Trim(),
            StoreId = storeId,
            SoldAt = soldAt.ToUniversalTime(),
            BusinessDate = DateOnly.FromDateTime(soldAt.UtcDateTime),
            Channel = channel,
        };
        sale._lines.AddRange(lines);

        if (sale._lines.Count == 0)
        {
            throw new SalesValidationException("A sale needs at least one line.");
        }

        sale.TotalAmount = sale._lines.Sum(l => l.NetAmount);
        sale.TotalUnits = sale._lines.Sum(l => l.Quantity);
        return sale;
    }
}

public sealed class SaleLine
{
    private SaleLine()
    {
    }

    public SaleLine(int productId, int quantity, decimal unitPrice, decimal discount = 0)
    {
        if (quantity <= 0)
        {
            throw new SalesValidationException("Quantity must be strictly positive.");
        }

        if (unitPrice < 0 || discount < 0)
        {
            throw new SalesValidationException("Price and discount cannot be negative.");
        }

        if (discount > quantity * unitPrice)
        {
            throw new SalesValidationException("Discount cannot exceed the line amount.");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Discount = decimal.Round(discount, 2);
        NetAmount = decimal.Round((quantity * unitPrice) - discount, 2);
    }

    public long Id { get; private set; }

    public long SaleId { get; private set; }

    public Sale Sale { get; private set; } = null!;

    public int ProductId { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Discount { get; private set; }

    /// <summary>Amount actually paid for the line (quantity x unit price - discount).</summary>
    public decimal NetAmount { get; private set; }
}

public sealed class SalesValidationException(string message) : Exception(message);
