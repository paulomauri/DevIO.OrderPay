using System.Text.Json;
using DevIO.OrderPay.Infra;
using DevIO.OrderPay.Infra.Outbox;
using DevIO.OrderPay.SharedKernel.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.WebApi.Outbox;

// Drains the outbox: claims a batch, deserializes each message back to its event, publishes it
// to the broker (RabbitMQ via MassTransit), and marks it processed. At-least-once — a crash
// before marking leaves the row for the next poll, and a redelivery before the consumer's dedup
// record leaves it for MassTransit to retry; consumers are idempotent.
//
// Single-claim: on SQL Server each poll atomically stamps a unique claim token on a batch of
// unclaimed rows (UPDATE … WITH (ROWLOCK, READPAST)), so with multiple replicas a row is
// published by exactly one worker. A claim older than the lease is reclaimable (crash recovery).
public class OutboxWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private const int BatchSize = 20;
    private const int LeaseSeconds = 60;
    private static readonly string WorkerId = Environment.MachineName; // pod/container name in K8s

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox poll failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken ct)
    {
        List<Guid> pendingIds;
        using (IServiceScope scope = scopeFactory.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            pendingIds = db.Database.IsSqlServer()
                ? await ClaimBatchAsync(db, ct)
                : await db.OutboxMessage // InMemory (tests): no concurrency, so a plain scan
                    .Where(m => m.ProcessedOn == null)
                    .OrderBy(m => m.OccurredOn)
                    .Take(BatchSize)
                    .Select(m => m.Id)
                    .ToListAsync(ct);
        }

        // Each message in its own scope/DbContext: publish to the broker, then mark processed.
        // Publishing by the event's runtime type routes it to the matching IConsumer<TEvent>.
        foreach (Guid id in pendingIds)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            IPublishEndpoint publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            // The claim already made this row ours, but re-assert ProcessedOn == null: after a
            // failed publish the lease can expire and another worker may reclaim it — then the DB
            // returns null here and we skip.
            OutboxMessage? message = await db.OutboxMessage
                .FirstOrDefaultAsync(m => m.Id == id && m.ProcessedOn == null, ct);
            if (message is null) continue;

            try
            {
                Type type = Type.GetType(message.Type)
                    ?? throw new InvalidOperationException($"Unknown outbox event type '{message.Type}'.");
                var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(message.Content, type)!;

                await publishEndpoint.Publish(domainEvent, type, ct);

                message.ProcessedOn = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                logger.LogError(ex, "Failed to process outbox message {OutboxId}", id);
            }

            await db.SaveChangesAsync(ct);
        }
    }

    // Atomically claims a batch on SQL Server: one UPDATE stamps a unique token on the oldest
    // unclaimed (or lease-expired) rows; READPAST lets a concurrent worker skip rows this one is
    // locking instead of blocking. Returns the Ids we now own, isolated by the token.
    private static async Task<List<Guid>> ClaimBatchAsync(AppDbContext db, CancellationToken ct)
    {
        string token = $"{WorkerId}/{Guid.NewGuid():N}";

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE OutboxMessage
            SET ClaimedAt = SYSUTCDATETIME(), ClaimedBy = {token}
            WHERE Id IN (
                SELECT TOP ({BatchSize}) Id FROM OutboxMessage WITH (ROWLOCK, READPAST)
                WHERE ProcessedOn IS NULL
                  AND (ClaimedAt IS NULL OR ClaimedAt < DATEADD(SECOND, {-LeaseSeconds}, SYSUTCDATETIME()))
                ORDER BY OccurredOn)", ct);

        return await db.OutboxMessage
            .Where(m => m.ClaimedBy == token && m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .Select(m => m.Id)
            .ToListAsync(ct);
    }
}
