using Microsoft.EntityFrameworkCore;
using RetailInsights.Core.Sales;

namespace RetailInsights.Infrastructure.Persistence;

public sealed class InsightsDbContext(DbContextOptions<InsightsDbContext> options) : DbContext(options)
{
    public DbSet<Store> Stores => Set<Store>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleLine> SaleLines => Set<SaleLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Store>(store =>
        {
            store.ToTable("stores");
            store.HasIndex(s => s.Code).IsUnique();
            store.Property(s => s.Code).HasMaxLength(20);
            store.Property(s => s.Name).HasMaxLength(100);
            store.Property(s => s.City).HasMaxLength(100);
            store.Property(s => s.Region).HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("products");
            product.HasIndex(p => p.Sku).IsUnique();
            product.Property(p => p.Sku).HasMaxLength(20);
            product.Property(p => p.Name).HasMaxLength(150);
            product.Property(p => p.Category).HasMaxLength(60);
            product.Property(p => p.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Sale>(sale =>
        {
            sale.ToTable("sales");
            sale.HasIndex(s => s.TicketNumber).IsUnique();
            // Every KPI filters on the business date, optionally on the store.
            sale.HasIndex(s => new { s.BusinessDate, s.StoreId });
            sale.Property(s => s.TicketNumber).HasMaxLength(40);
            sale.Property(s => s.Channel).HasConversion<string>().HasMaxLength(20);
            sale.Property(s => s.TotalAmount).HasPrecision(12, 2);
            sale.HasOne<Store>().WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
            sale.HasMany(s => s.Lines).WithOne(l => l.Sale).HasForeignKey(l => l.SaleId).OnDelete(DeleteBehavior.Cascade);
            sale.Navigation(s => s.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SaleLine>(line =>
        {
            line.ToTable("sale_lines");
            line.HasIndex(l => l.ProductId);
            line.Property(l => l.UnitPrice).HasPrecision(10, 2);
            line.Property(l => l.Discount).HasPrecision(10, 2);
            line.Property(l => l.NetAmount).HasPrecision(12, 2);
            line.HasOne<Product>().WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
