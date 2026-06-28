using DevIO.OrderPay.SharedKernel.Events;

namespace DevIO.OrderPay.Order.Events;

// Raised by Order.UpdateStatus when the order enters the matching state. Carry the
// OrderId; downstream consumers load whatever else they need (Phase 9 adds the
// shipping payload to OrderProcessingEvent).
public record PaymentConfirmedEvent(Guid OrderId) : DomainEvent;
public record OrderProcessingEvent(Guid OrderId) : DomainEvent;
public record OrderShippedEvent(Guid OrderId) : DomainEvent;
public record OrderDeliveredEvent(Guid OrderId) : DomainEvent;
public record OrderCancelledEvent(Guid OrderId) : DomainEvent;
