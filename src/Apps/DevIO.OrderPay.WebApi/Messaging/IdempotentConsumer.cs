using DevIO.OrderPay.Infra;
using DevIO.OrderPay.Infra.Outbox;
using DevIO.OrderPay.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.WebApi.Messaging;

// Dedup gate shared by every consumer: an event whose Id was already processed is skipped,
// so the at-least-once Outbox delivery becomes effectively-once. The dedup key is the event's
// EventId — the same GUID the interceptor stamped on the OutboxMessage the worker published.
public static class IdempotentConsumer
{
    public static async Task HandleOnce<TEvent>(
        AppDbContext db,
        IDomainEventHandler<TEvent> handler,
        TEvent message,
        CancellationToken cancellationToken) where TEvent : IDomainEvent
    {
        if (await db.ProcessedOutboxMessage.AnyAsync(p => p.Id == message.EventId, cancellationToken))
            return;

        // The handler advances the order, which raises the next event → the interceptor writes
        // it to the Outbox in the handler's own SaveChanges. Recording the processed Id here is a
        // second save on the same scoped DbContext.
        await handler.HandleAsync(message, cancellationToken);

        db.ProcessedOutboxMessage.Add(new ProcessedOutboxMessage
        {
            Id = message.EventId,
            ProcessedOn = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
