using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Application.EventHandlers;
using DevIO.OrderPay.Order.Application.Services;
using DevIO.OrderPay.Order.Events;
using DevIO.OrderPay.Order.Models;
using DevIO.OrderPay.SharedKernel.Contracts;
using Moq;

namespace DevIO.OrderPay.Tests.Application;

public class OrderEventHandlerTests
{
    private readonly Mock<IOrderService> _orders = new();

    public OrderEventHandlerTests() =>
        _orders.Setup(o => o.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<OrderStatus>()))
               .ReturnsAsync((OrderResponse?)null);

    [Fact]
    public async Task ConfirmOrderOnPaymentCaptured_AdvancesOrderToPaymentConfirmed()
    {
        var orderId = Guid.NewGuid();
        var handler = new ConfirmOrderOnPaymentCaptured(_orders.Object);

        await handler.HandleAsync(new PaymentCapturedEvent(Guid.NewGuid(), orderId, 100m, "USD", "ref"));

        _orders.Verify(o => o.UpdateStatusAsync(orderId, OrderStatus.PaymentConfirmed), Times.Once);
    }

    [Fact]
    public async Task StartProcessingOnOrderConfirmed_AdvancesOrderToProcessing()
    {
        var orderId = Guid.NewGuid();
        var handler = new StartProcessingOnOrderConfirmed(_orders.Object);

        await handler.HandleAsync(new PaymentConfirmedEvent(orderId));

        _orders.Verify(o => o.UpdateStatusAsync(orderId, OrderStatus.Processing), Times.Once);
    }
}
