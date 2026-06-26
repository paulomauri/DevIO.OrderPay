using DevIO.OrderPay.Order.Application.Services;
using DevIO.OrderPay.Order.Models;
using DevIO.OrderPay.Payment.Application.Integration;

namespace DevIO.OrderPay.WebApi.Integration;

// The Payment → Order seam. Lives in the WebApi composition root so it can bridge
// the two contexts (Payment raises the event; this advances the order) without
// either Application layer depending on the other. Phase 8 replaces this in-process
// call with an Outbox message.
public class OrderPaymentCapturedHandler(IOrderService orderService) : IPaymentCapturedHandler
{
    private readonly IOrderService _orderService = orderService;

    public async Task HandleAsync(PaymentCapturedEvent capturedEvent, CancellationToken cancellationToken = default)
    {
        await _orderService.UpdateStatusAsync(capturedEvent.OrderId, OrderStatus.PaymentConfirmed);
    }
}
