namespace RetailInsights.Core.Sales;

public sealed class Store
{
    private Store()
    {
    }

    public Store(string code, string name, string city, string region)
    {
        Code = code;
        Name = name;
        City = city;
        Region = region;
    }

    public int Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string Region { get; private set; } = string.Empty;
}
