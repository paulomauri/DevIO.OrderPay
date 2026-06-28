using System.Text.Json;
using DevIO.OrderPay.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DevIO.OrderPay.Infra.Outbox;

// The transactional outbox's heart: just before SaveChanges commits, turn every tracked
// aggregate's domain events into OutboxMessage rows. Because they're added to the same
// DbContext, they persist in the SAME transaction as the state change — no dual-write.
public sealed class ConvertDomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null) ConvertEvents(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null) ConvertEvents(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ConvertEvents(DbContext context)
    {
        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        var messages = aggregates
            .SelectMany(a => a.DomainEvents)
            .Select(e => new OutboxMessage
            {
                Id = e.EventId,
                OccurredOn = e.OccurredOn,
                Type = e.GetType().AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(e, e.GetType(), JsonOptions),
            })
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        if (messages.Count > 0)
            context.Set<OutboxMessage>().AddRange(messages);
    }
}
