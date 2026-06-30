using DevIO.OrderPay.Infra;
using DevIO.OrderPay.SharedKernel.Contracts;
using DevIO.OrderPay.SharedKernel.Events;
using DevIO.OrderPay.WebApi.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.Tests.Messaging;

// The dedup gate is what turns the Outbox's at-least-once delivery into effectively-once:
// a redelivered event (same EventId) must run the handler exactly once.
public class IdempotentConsumerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class CountingHandler : IDomainEventHandler<PaymentCapturedEvent>
    {
        public int Calls { get; private set; }
        public Task HandleAsync(PaymentCapturedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private static PaymentCapturedEvent Event() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", "ref_1");

    [Fact]
    public async Task HandleOnce_FirstDelivery_RunsHandlerAndRecordsProcessed()
    {
        await using var ctx = NewContext();
        var handler = new CountingHandler();
        var message = Event();

        await IdempotentConsumer.HandleOnce(ctx, handler, message, CancellationToken.None);

        handler.Calls.Should().Be(1);
        (await ctx.ProcessedOutboxMessage.AnyAsync(p => p.Id == message.EventId)).Should().BeTrue();
    }

    [Fact]
    public async Task HandleOnce_Redelivery_SkipsHandler()
    {
        await using var ctx = NewContext();
        var handler = new CountingHandler();
        var message = Event();

        await IdempotentConsumer.HandleOnce(ctx, handler, message, CancellationToken.None);
        await IdempotentConsumer.HandleOnce(ctx, handler, message, CancellationToken.None); // same EventId

        handler.Calls.Should().Be(1); // second delivery deduped
        (await ctx.ProcessedOutboxMessage.CountAsync(p => p.Id == message.EventId)).Should().Be(1);
    }
}
