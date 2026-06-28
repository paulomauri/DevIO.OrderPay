namespace DevIO.OrderPay.SharedKernel.Events;

// Something that happened in the domain. EventId is the stable identity used by the
// Outbox for at-least-once delivery + idempotent (dedup) consumption.
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
