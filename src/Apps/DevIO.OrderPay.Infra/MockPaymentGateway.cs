using System.Collections.Concurrent;
using DevIO.OrderPay.Core.Gateway;
using DevIO.OrderPay.Payment.Models;

namespace DevIO.OrderPay.Infra;

// In-memory stand-in for a real provider (Stripe, etc.). It caches the result per
// idempotency key, so a retried charge returns the ORIGINAL result instead of
// charging again — the same guarantee a real gateway gives. Register as a singleton
// so the cache survives across requests. Cards ending in 0000 simulate a decline.
public class MockPaymentGateway : IPaymentGateway
{
    private static readonly ConcurrentDictionary<string, PaymentGatewayResult> _processed = new();

    public Task<PaymentGatewayResult> ChargeAsync(
        string idempotencyKey,
        Amount amount,
        PaymentMethod method,
        CancellationToken cancellationToken = default)
    {
        PaymentGatewayResult result = _processed.GetOrAdd(idempotencyKey, _ => Charge(method));
        return Task.FromResult(result);
    }

    private static PaymentGatewayResult Charge(PaymentMethod method)
    {
        if (method is PaymentMethodCard { Last4: "0000" })
            return PaymentGatewayResult.Decline("Card declined.");

        return PaymentGatewayResult.Approve($"mock_{Guid.NewGuid():N}");
    }
}
