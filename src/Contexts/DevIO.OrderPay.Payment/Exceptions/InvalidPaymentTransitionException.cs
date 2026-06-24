using DevIO.OrderPay.Payment.Models;

namespace DevIO.OrderPay.Payment.Exceptions
{
    public class InvalidPaymentTransitionException(PaymentStatus status, PaymentStatus next)
        : Exception($"Invalid payment transition from -> {PaymentStatusConverter.ToStringValue(status)} " +
            $"to -> {PaymentStatusConverter.ToStringValue(next)}")
    { }
}
