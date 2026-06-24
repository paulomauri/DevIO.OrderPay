using DevIO.OrderPay.Payment.Models;

namespace DevIO.OrderPay.Core.Repository;

public interface IPaymentRepository : IRepository<Payment.Models.Payment>
{
    Task<Payment.Models.Payment?> GetByOrderIdAsync(Guid orderId);

    Task<PaymentAttempt?> GetAttemptByKeyAsync(string idempotencyKey);
    Task<IEnumerable<PaymentAttempt>> GetAttemptsByOrderIdAsync(Guid orderId);
    Task AddAttemptAsync(PaymentAttempt attempt);
}
