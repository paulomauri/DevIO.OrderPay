namespace DevIO.OrderPay.Payment.Application.Integration;

// The seam between Payment and Order. PaymentService calls this after a capture;
// the WebApi composition root provides an implementation that advances the order
// (keeping the two contexts decoupled). Phase 8 turns this into an Outbox dispatch.
public interface IPaymentCapturedHandler
{
    Task HandleAsync(PaymentCapturedEvent capturedEvent, CancellationToken cancellationToken = default);
}
