using DevIO.OrderPay.SharedKernel.Events;

namespace DevIO.OrderPay.SharedKernel.Contracts;

// Integration event: raised by the Payment context when a payment is captured, consumed
// by the Order context to advance the order. Lives in SharedKernel so neither context
// references the other — they only share this contract.
public record PaymentCapturedEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string GatewayReference) : DomainEvent;
