using DevIO.OrderPay.Core.Gateway;
using DevIO.OrderPay.Core.Repository;
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

// Order entered Processing → notify the logistics carrier it's ready to ship (Phase 9,
// outbound). Loads the order (with items) + the customer's shipping address and hands a
// LogisticsDispatch to the carrier port. Throws if the order/address is missing so the
// Outbox retries; the dispatch IdempotencyKey makes a retry a no-op at the carrier.
public class DispatchOrderOnProcessing(
    IOrderRepository orders,
    ICustomerRepository customers,
    ILogisticsClient logistics) : IDomainEventHandler<OrderProcessingEvent>
{
    public async Task HandleAsync(OrderProcessingEvent e, CancellationToken cancellationToken = default)
    {
        Order.Models.Order order = await orders.GetByIdAsync(e.OrderId)
            ?? throw new InvalidOperationException($"Order {e.OrderId} not found for logistics dispatch.");

        Customer.Models.Customer? customer = await customers.GetByIdWithAddressAsync(order.CustomerId);
        Customer.Models.Address address = customer?.Enderecos.FirstOrDefault()
            ?? throw new InvalidOperationException($"Order {e.OrderId} customer has no shipping address.");

        var dispatch = new LogisticsDispatch(
            OrderId: order.Id,
            IdempotencyKey: $"dispatch:{order.Id}",
            CreatedAt: order.CreatedAt,
            ShippingAddress: new LogisticsAddress(
                PostalCode: address.CEP,
                Street: address.Rua,
                Number: address.Numero,
                Complement: address.Complemento,
                District: address.Bairro,
                City: address.Municipio,
                State: address.Estado?.ToString()),
            Items: [.. order.Items.Select(i => new LogisticsItem(i.ProductId, i.Quantity))]);

        await logistics.NotifyOrderAsync(dispatch, cancellationToken);
    }
}
