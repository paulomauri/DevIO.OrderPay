namespace DevIO.OrderPay.Payment.Application.Services;

public record PaymentGatewayResult(bool Approved, string? GatewayReference, string? DeclineReason)
{
    public static PaymentGatewayResult Approve(string gatewayReference) =>
        new(true, gatewayReference, null);

    public static PaymentGatewayResult Decline(string reason) =>
        new(false, null, reason);
}
