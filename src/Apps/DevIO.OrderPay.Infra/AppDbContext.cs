using DevIO.OrderPay.Customer.Models;
using DevIO.OrderPay.Order.Models;
using DevIO.OrderPay.Payment.Models;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.Infra;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
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

        // ── Payment context ───────────────────────────────────────────
        builder.Entity<Payment.Models.Payment>(entity =>
        {
            entity.HasKey(p => p.Id);

            // Amount value object → two flat columns.
            entity.OwnsOne(p => p.Amount, amount =>
            {
                amount.Property(a => a.Value)
                      .HasColumnName("AmountValue")
                      .HasColumnType("decimal(18,2)");
                amount.Property(a => a.Currency)
                      .HasColumnName("AmountCurrency")
                      .HasMaxLength(3);
            });
            entity.Navigation(p => p.Amount).IsRequired();

            // PaymentMethod is polymorphic; EF owned types can't be — persist it as
            // a single JSON column via the Infra-side serializer.
            entity.Property(p => p.Method)
                  .HasColumnName("Method")
                  .HasConversion(
                      m => PaymentMethodJson.Serialize(m),
                      s => PaymentMethodJson.Deserialize(s));

            entity.Property(p => p.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.HasIndex(p => p.OrderId);
        });

        builder.Entity<PaymentAttempt>(entity =>
        {
            entity.HasKey(a => a.Id);

            // The idempotency backbone: the unique key is what guarantees
            // at-most-once — a concurrent/replayed charge can't insert a second row.
            entity.HasIndex(a => a.IdempotencyKey).IsUnique();
            entity.Property(a => a.IdempotencyKey).HasMaxLength(80);

            entity.Property(a => a.Outcome)
                  .HasConversion<string>()
                  .HasMaxLength(20);
        });
    }

    // Customer context
    public DbSet<Customer.Models.Customer> Customer { get; set; }
    public DbSet<Customer.Models.Address> Address { get; set; }

    // Order context
    public DbSet<Order.Models.Order> Order { get; set; }
    public DbSet<OrderItem> OrderItem { get; set; }
    public DbSet<Product> Product { get; set; }

    // Payment context
    public DbSet<Payment.Models.Payment> Payment { get; set; }
    public DbSet<PaymentAttempt> PaymentAttempt { get; set; }
}
