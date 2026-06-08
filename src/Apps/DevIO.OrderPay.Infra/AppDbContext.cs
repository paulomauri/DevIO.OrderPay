using DevIO.OrderPay.Customer.Models;
using DevIO.OrderPay.Order.Models;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.Infra;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Customer context ──────────────────────────────────────────
        builder.Entity<Customer.Models.Customer>(entity =>
        {
            entity.Property(x => x.Email)
                  .HasConversion(
                      email => email.Value,
                      value => new Email(value));
        });

        builder.Entity<Customer.Models.Address>(entity =>
        {
            entity.Property(e => e.Estado)
                  .HasConversion(
                      v => v.ToStringValue().ToUpperInvariant(),
                      v => Enum.Parse<State>(v, true))
                  .HasMaxLength(2);
        });

        // ── Order context ─────────────────────────────────────────────
        builder.Entity<Order.Models.Order>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.TotalPrice)
                  .HasConversion(
                      p => p.Value,
                      v => new Price(v));

            entity.Property(o => o.TotalDiscount)
                  .HasConversion(
                      p => p.Value,
                      v => new Price(v));

            entity.Ignore(o => o.Customer);

            entity.HasMany(o => o.Items)
                  .WithOne()
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Price)
                  .HasConversion(
                      p => p.Value,
                      v => new Price(v));

            entity.Property(i => i.Discount)
                  .HasConversion(
                      p => p.Value,
                      v => new Price(v));
        });

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.SKU).IsUnique();
        });
    }

    // Customer context
    public DbSet<Customer.Models.Customer> Customer { get; set; }
    public DbSet<Customer.Models.Address> Address { get; set; }

    // Order context
    public DbSet<Order.Models.Order> Order { get; set; }
    public DbSet<OrderItem> OrderItem { get; set; }
    public DbSet<Product> Product { get; set; }
}
