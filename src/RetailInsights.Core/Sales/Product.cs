namespace RetailInsights.Core.Sales;

public sealed class Product
{
    private Product()
    {
    }

    public Product(string sku, string name, string category, decimal price)
    {
        Sku = sku;
        Name = name;
        Category = category;
        Price = price;
    }

    public int Id { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public decimal Price { get; private set; }
}
