namespace DevIO.OrderPay.SharedKernel.Events;

// Base for concrete events: `record OrderProcessingEvent(Guid OrderId) : DomainEvent;`
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
