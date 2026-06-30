using DevIO.OrderPay.Order.Application.Services;
using DevIO.OrderPay.Order.Events;
using DevIO.OrderPay.Order.Models;
using DevIO.OrderPay.SharedKernel.Contracts;
using DevIO.OrderPay.SharedKernel.Events;

namespace DevIO.OrderPay.Order.Application.EventHandlers;

// Payment captured (cross-context event) → confirm the order.
// Replaces the Phase 7 in-process OrderPaymentCapturedHandler.
public class ConfirmOrderOnPaymentCaptured(IOrderService orders) : IDomainEventHandler<PaymentCapturedEvent>
{
    public Task HandleAsync(PaymentCapturedEvent e, CancellationToken cancellationToken = default)
        => orders.UpdateStatusAsync(e.OrderId, OrderStatus.PaymentConfirmed);
}

// Order confirmed → start processing it (the advance deferred from Phase 7).
public class StartProcessingOnOrderConfirmed(IOrderService orders) : IDomainEventHandler<PaymentConfirmedEvent>
{
    public Task HandleAsync(PaymentConfirmedEvent e, CancellationToken cancellationToken = default)
        => orders.UpdateStatusAsync(e.OrderId, OrderStatus.Processing);
}
