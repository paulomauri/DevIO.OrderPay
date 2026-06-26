namespace DevIO.OrderPay.Core.Gateway;

// Outcome of a single gateway charge. GatewayReference identifies the charge at
// the provider (used for later capture/refund and for the idempotent replay).
public record PaymentGatewayResult(bool Approved, string? GatewayReference, string? DeclineReason)
{
    public static PaymentGatewayResult Approve(string gatewayReference) =>
        new(true, gatewayReference, null);

    public static PaymentGatewayResult Decline(string reason) =>
        new(false, null, reason);
}
