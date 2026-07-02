using DevIO.OrderPay.Infra;
using DevIO.OrderPay.Order.Events;
using DevIO.OrderPay.SharedKernel.Contracts;
using DevIO.OrderPay.SharedKernel.Events;
using MassTransit;

namespace DevIO.OrderPay.WebApi.Messaging;

// MassTransit consumers are the broker seam (8b): the Outbox worker publishes the event to
// RabbitMQ, MassTransit redelivers on failure, and each consumer delegates to the same
// IDomainEventHandler business logic the in-process dispatcher used in 8a — wrapped in the
// IdempotentConsumer dedup gate so a redelivery never double-applies.

public class PaymentCapturedConsumer(AppDbContext db, IDomainEventHandler<PaymentCapturedEvent> handler)
    : IConsumer<PaymentCapturedEvent>
{
    public Task Consume(ConsumeContext<PaymentCapturedEvent> context) =>
        IdempotentConsumer.HandleOnce(db, handler, context.Message, context.CancellationToken);
}

public class PaymentConfirmedConsumer(AppDbContext db, IDomainEventHandler<PaymentConfirmedEvent> handler)
    : IConsumer<PaymentConfirmedEvent>
{
    public Task Consume(ConsumeContext<PaymentConfirmedEvent> context) =>
        IdempotentConsumer.HandleOnce(db, handler, context.Message, context.CancellationToken);
}

// Phase 9: OrderProcessingEvent was published to an unbound exchange in Phase 8 — this binds it,
// delegating to DispatchOrderOnProcessing which notifies the logistics carrier.
public class OrderProcessingConsumer(AppDbContext db, IDomainEventHandler<OrderProcessingEvent> handler)
    : IConsumer<OrderProcessingEvent>
{
    public Task Consume(ConsumeContext<OrderProcessingEvent> context) =>
        IdempotentConsumer.HandleOnce(db, handler, context.Message, context.CancellationToken);
}
