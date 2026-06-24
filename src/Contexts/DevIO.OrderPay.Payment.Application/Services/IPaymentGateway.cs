using DevIO.OrderPay.Payment.Models;

namespace DevIO.OrderPay.Payment.Application.Services;

// Port over a payment provider (Stripe / mock). The idempotency key is passed
// through so the provider can dedupe a retried charge and never bill twice.
public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ChargeAsync(
        string idempotencyKey,
        Amount amount,
        PaymentMethod method,
        CancellationToken cancellationToken = default);
}
