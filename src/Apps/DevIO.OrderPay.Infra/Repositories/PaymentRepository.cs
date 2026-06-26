using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Payment.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.Infra.Repositories;

public class PaymentRepository(AppDbContext context)
    : Repository<Payment.Models.Payment>(context), IPaymentRepository
{
    public async Task<Payment.Models.Payment?> GetByOrderIdAsync(Guid orderId) =>
        await DbSet.FirstOrDefaultAsync(p => p.OrderId == orderId);

    // Tracked (no AsNoTracking) — the caller may record the outcome on the attempt and save it.
    public async Task<PaymentAttempt?> GetAttemptByKeyAsync(string idempotencyKey) =>
        await Context.Set<PaymentAttempt>()
                     .FirstOrDefaultAsync(a => a.IdempotencyKey == idempotencyKey);

    // Read-only — only used to compute the next attempt number.
    public async Task<IEnumerable<PaymentAttempt>> GetAttemptsByOrderIdAsync(Guid orderId) =>
        await Context.Set<PaymentAttempt>()
                     .AsNoTracking()
                     .Where(a => a.OrderId == orderId)
                     .ToListAsync();

    public async Task AddAttemptAsync(PaymentAttempt attempt) =>
        await Context.Set<PaymentAttempt>().AddAsync(attempt);

    // Translate the unique-IdempotencyKey violation (a concurrent/replayed charge that
    // raced past the in-app check) into a domain exception, so the Application layer
    // never sees EF/SQL types.
    public override async Task<int> SaveChangesAsync()
    {
        try
        {
            return await Context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            string key = ex.Entries
                .Select(e => e.Entity)
                .OfType<PaymentAttempt>()
                .FirstOrDefault()?.IdempotencyKey ?? "unknown";

            throw new DuplicatePaymentAttemptException(key);
        }
    }
}
