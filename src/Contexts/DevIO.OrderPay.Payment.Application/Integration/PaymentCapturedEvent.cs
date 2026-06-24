namespace DevIO.OrderPay.Payment.Application.Integration;

// Raised after a payment is captured. Phase 8 replaces the in-process handler
// dispatch below with an Outbox message; the event shape stays the same.
public record PaymentCapturedEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string GatewayReference);
