using DevIO.OrderPay.Infra;
using DevIO.OrderPay.Infra.Outbox;
using DevIO.OrderPay.Order.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderModel = DevIO.OrderPay.Order.Models.Order;

namespace DevIO.OrderPay.Tests.Infrastructure;

public class OutboxInterceptorTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new ConvertDomainEventsToOutboxInterceptor())
            .Options);

    [Fact]
    public async Task SaveChanges_AggregateWithDomainEvents_WritesOutboxRowAndClearsEvents()
    {
        await using var ctx = NewContext();
        var order = new OrderModel(Guid.NewGuid());
        ctx.Order.Add(order);
        await ctx.SaveChangesAsync(); // creation raises no events

        order.UpdateStatus(OrderStatus.PaymentConfirmed); // raises PaymentConfirmedEvent
        await ctx.SaveChangesAsync();

        var messages = await ctx.OutboxMessage.AsNoTracking().ToListAsync();
        messages.Should().ContainSingle();
        messages[0].Id.Should().NotBeEmpty();
        messages[0].Type.Should().Contain("PaymentConfirmedEvent");
        messages[0].ProcessedOn.Should().BeNull();
        order.DomainEvents.Should().BeEmpty(); // interceptor cleared them after converting
    }

    [Fact]
    public async Task SaveChanges_NoDomainEvents_WritesNoOutboxRows()
    {
        await using var ctx = NewContext();
        ctx.Order.Add(new OrderModel(Guid.NewGuid()));

        await ctx.SaveChangesAsync();

        (await ctx.OutboxMessage.CountAsync()).Should().Be(0);
    }
}
