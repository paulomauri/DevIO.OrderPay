using DevIO.OrderPay.SharedKernel.Events;

namespace DevIO.OrderPay.SharedKernel;

// Base for aggregate roots that record domain events. The Outbox interceptor reads
// DomainEvents at SaveChanges time and persists them in the same transaction.
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
