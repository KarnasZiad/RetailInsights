namespace RetailInsights.Core.Kpis;

/// <summary>Raw aggregates computed by the database. Every KPI derives from them.</summary>
public sealed record SalesTotals(decimal Revenue, int Transactions, int Units)
{
    public static readonly SalesTotals Empty = new(0m, 0, 0);

    /// <summary>Panier moyen: revenue per ticket.</summary>
    public decimal AverageBasket => Transactions == 0 ? 0m : decimal.Round(Revenue / Transactions, 2, MidpointRounding.AwayFromZero);

    /// <summary>Indice de vente (UPT): units per ticket.</summary>
    public decimal UnitsPerTransaction => Transactions == 0 ? 0m : decimal.Round((decimal)Units / Transactions, 2, MidpointRounding.AwayFromZero);

    /// <summary>Prix moyen de vente: revenue per unit.</summary>
    public decimal AverageSellingPrice => Units == 0 ? 0m : decimal.Round(Revenue / Units, 2, MidpointRounding.AwayFromZero);

    public static SalesTotals operator +(SalesTotals left, SalesTotals right) =>
        new(left.Revenue + right.Revenue, left.Transactions + right.Transactions, left.Units + right.Units);
}
