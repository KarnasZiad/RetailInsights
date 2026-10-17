using RetailInsights.Core.Sales;

namespace RetailInsights.UnitTests;

public sealed class SaleTests
{
    private static readonly DateTimeOffset SoldAt = new(2026, 3, 2, 23, 30, 0, TimeSpan.FromHours(-2));

    [Fact]
    public void Totals_are_computed_from_the_lines()
    {
        var sale = Sale.Create("T-1", 1, SoldAt, SalesChannel.InStore, [new SaleLine(1, 2, 19.90m), new SaleLine(2, 1, 89.90m, discount: 9.99m)]);

        Assert.Equal(119.71m, sale.TotalAmount);
        Assert.Equal(3, sale.TotalUnits);
    }

    [Fact]
    public void Business_date_is_the_utc_day()
    {
        var sale = Sale.Create("T-1", 1, SoldAt, SalesChannel.InStore, [new SaleLine(1, 1, 10m)]);

        Assert.Equal(new DateOnly(2026, 3, 3), sale.BusinessDate);
        Assert.Equal(TimeSpan.Zero, sale.SoldAt.Offset);
    }

    [Fact]
    public void A_sale_needs_lines_and_a_ticket_number()
    {
        Assert.Throws<SalesValidationException>(() => Sale.Create("T-1", 1, SoldAt, SalesChannel.InStore, []));
        Assert.Throws<SalesValidationException>(() => Sale.Create(" ", 1, SoldAt, SalesChannel.InStore, [new SaleLine(1, 1, 10m)]));
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 10, -1)]
    [InlineData(1, 10, 10.01)]
    public void Invalid_lines_are_rejected(int quantity, decimal price, decimal discount) =>
        Assert.Throws<SalesValidationException>(() => new SaleLine(1, quantity, price, discount));
}
