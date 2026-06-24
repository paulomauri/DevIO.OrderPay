using DevIO.OrderPay.Payment.Application.DTOs;

namespace DevIO.OrderPay.Payment.Application.Services;

public interface IPaymentService
{
    Task<PaymentResponse> PayAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse?> GetByOrderIdAsync(Guid orderId);
}
