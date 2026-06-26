using DevIO.OrderPay.Payment.Models;

namespace DevIO.OrderPay.Core.Gateway;

// Port over a payment provider (Stripe / mock). Lives in Core — like the repository
// interfaces — so the Infrastructure adapter can implement it without the Application
// layer, and the Application layer can depend on it without touching Infrastructure.
// The idempotency key is passed through so the provider can dedupe a retried charge
// and never bill twice.
public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ChargeAsync(
        string idempotencyKey,
        Amount amount,
        PaymentMethod method,
        CancellationToken cancellationToken = default);
}
