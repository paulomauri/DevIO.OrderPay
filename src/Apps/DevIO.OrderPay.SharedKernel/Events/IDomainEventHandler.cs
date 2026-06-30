namespace DevIO.OrderPay.SharedKernel.Events;

// Handles one kind of domain event. In 8a the OutboxWorker resolves these in-process;
// in 8b the same role is played by MassTransit consumers.
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
