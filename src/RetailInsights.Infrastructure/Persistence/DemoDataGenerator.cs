using Microsoft.EntityFrameworkCore;
using RetailInsights.Core.Sales;

namespace RetailInsights.Infrastructure.Persistence;

/// <summary>
/// Deterministic, realistic-looking sales history for the demo: store sizes, weekly seasonality
/// (busy Saturdays, quiet Sundays), a slow upward trend, multi-line tickets, promotions and channels.
/// </summary>
public static class DemoDataGenerator
{
    private static readonly (string Code, string Name, string City, string Region, double Traffic)[] DemoStores =
    [
        ("PAR-RIV", "Paris Rivoli", "Paris", "Ile-de-France", 1.6),
        ("PAR-DEF", "La Defense", "Puteaux", "Ile-de-France", 1.3),
        ("LYO-PDI", "Lyon Part-Dieu", "Lyon", "Auvergne-Rhone-Alpes", 1.1),
        ("MRS-TDP", "Marseille Terrasses", "Marseille", "Provence-Alpes-Cote d'Azur", 0.9),
        ("BDX-STC", "Bordeaux Sainte-Catherine", "Bordeaux", "Nouvelle-Aquitaine", 0.8),
        ("LIL-EUR", "Lille Euralille", "Lille", "Hauts-de-France", 0.7),
    ];

    private static readonly (string Sku, string Name, string Category, decimal Price, int Popularity)[] DemoProducts =
    [
        ("TSH-001", "T-shirt coton bio", "Textile", 19.90m, 30),
        ("JEA-002", "Jean slim brut", "Textile", 59.90m, 18),
        ("SWE-003", "Sweat a capuche", "Textile", 44.90m, 14),
        ("VES-004", "Veste en jean", "Textile", 79.90m, 7),
        ("CHE-005", "Chemise en lin", "Textile", 49.90m, 10),
        ("PUL-006", "Pull col rond merinos", "Textile", 69.90m, 8),
        ("ROB-007", "Robe midi fluide", "Textile", 64.90m, 9),
        ("SNK-008", "Sneakers cuir blanc", "Chaussures", 89.90m, 12),
        ("BOT-009", "Bottines chelsea", "Chaussures", 119.00m, 5),
        ("SAN-010", "Sandales cuir", "Chaussures", 54.90m, 6),
        ("SAC-011", "Sac cabas toile", "Accessoires", 34.90m, 11),
        ("CEI-012", "Ceinture cuir", "Accessoires", 29.90m, 9),
        ("CAS-013", "Casquette brodee", "Accessoires", 24.90m, 10),
        ("ECH-014", "Echarpe laine", "Accessoires", 32.90m, 6),
        ("BOU-015", "Bougie parfumee", "Maison", 18.90m, 8),
        ("PLA-016", "Plaid coton", "Maison", 49.90m, 4),
    ];

    public static async Task SeedAsync(InsightsDbContext db, int days, DateOnly lastDay, CancellationToken cancellationToken)
    {
        if (await db.Stores.AnyAsync(cancellationToken))
        {
            return;
        }

        var stores = DemoStores.Select(s => new Store(s.Code, s.Name, s.City, s.Region)).ToList();
        var products = DemoProducts.Select(p => new Product(p.Sku, p.Name, p.Category, p.Price)).ToList();
        db.Stores.AddRange(stores);
        db.Products.AddRange(products);
        await db.SaveChangesAsync(cancellationToken);

        var random = new Random(20261017);
        var weightedProducts = DemoProducts
            .SelectMany((p, index) => Enumerable.Repeat(products[index], p.Popularity))
            .ToArray();

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        var firstDay = lastDay.AddDays(-(days - 1));
        var ticket = 0;

        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            var progress = (day.DayNumber - firstDay.DayNumber) / (double)days;
            var dayFactor = day.DayOfWeek switch
            {
                DayOfWeek.Saturday => 1.7,
                DayOfWeek.Sunday => 0.35,
                DayOfWeek.Friday => 1.2,
                DayOfWeek.Wednesday => 1.05,
                _ => 0.9,
            };

            for (var s = 0; s < stores.Count; s++)
            {
                var tickets = (int)Math.Round(26 * DemoStores[s].Traffic * dayFactor * (0.9 + (0.25 * progress)) * (0.85 + (random.NextDouble() * 0.3)));
                for (var t = 0; t < tickets; t++)
                {
                    var lineCount = random.NextDouble() switch
                    {
                        < 0.55 => 1,
                        < 0.85 => 2,
                        < 0.96 => 3,
                        _ => 4,
                    };

                    var lines = weightedProducts
                        .OrderBy(_ => random.Next())
                        .DistinctBy(p => p.Id)
                        .Take(lineCount)
                        .Select(p =>
                        {
                            var quantity = random.NextDouble() < 0.85 ? 1 : 2;
                            var discount = random.NextDouble() < 0.12 ? decimal.Round(p.Price * quantity * 0.2m, 2) : 0m;
                            return new SaleLine(p.Id, quantity, p.Price, discount);
                        })
                        .ToList();

                    var channel = random.NextDouble() switch
                    {
                        < 0.78 => SalesChannel.InStore,
                        < 0.92 => SalesChannel.ClickAndCollect,
                        _ => SalesChannel.Online,
                    };

                    var soldAt = new DateTimeOffset(day.ToDateTime(new TimeOnly(9, 30)), TimeSpan.Zero).AddMinutes(random.Next(0, 600));
                    db.Sales.Add(Sale.Create($"T{day:yyMMdd}-{stores[s].Code}-{++ticket:D6}", stores[s].Id, soldAt, channel, lines));
                }
            }

            // Flush one day at a time to keep the change tracker small.
            db.ChangeTracker.DetectChanges();
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }

        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }
}
