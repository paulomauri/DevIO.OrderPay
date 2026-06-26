using DevIO.OrderPay.Payment.Exceptions;
using DevIO.OrderPay.Payment.Models;
using FluentAssertions;

namespace DevIO.OrderPay.Tests.Domain;

public class PaymentTests
{
    private static Payment.Models.Payment NewPayment() =>
        new(Guid.NewGuid(),
            new Amount(100m, "USD"),
            new PaymentMethodCard("Visa", "4242", "12/27", PaymentType.CREDIT));

    [Fact]
    public void NewPayment_StartsInPending() =>
        NewPayment().Status.Should().Be(PaymentStatus.Pending);

    [Fact]
    public void HappyPath_PendingToCaptured_RecordsGatewayReference()
    {
        var payment = NewPayment();

        payment.BeginProcessing();
        payment.Authorize("ref_1");
        payment.Capture();

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.GatewayReference.Should().Be("ref_1");
    }

    [Fact]
    public void Capture_FromPending_ThrowsInvalidTransition()
    {
        var payment = NewPayment();

        var act = payment.Capture;

        act.Should().Throw<InvalidPaymentTransitionException>();
    }

    [Fact]
    public void Authorize_FromPending_ThrowsInvalidTransition()
    {
        var payment = NewPayment();

        var act = () => payment.Authorize("ref");

        act.Should().Throw<InvalidPaymentTransitionException>();
    }

    [Fact]
    public void Decline_FromProcessing_ReturnsToPending_Retryable()
    {
        var payment = NewPayment();
        payment.BeginProcessing();

        payment.Decline();

        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Abandon_IsTerminal_NoFurtherTransitions()
    {
        var payment = NewPayment();

        payment.Abandon();
        payment.Status.Should().Be(PaymentStatus.Failed);

        var act = payment.BeginProcessing;
        act.Should().Throw<InvalidPaymentTransitionException>();
    }

    [Fact]
    public void Refund_FromCaptured_Succeeds()
    {
        var payment = NewPayment();
        payment.BeginProcessing();
        payment.Authorize("ref");
        payment.Capture();

        payment.Refund();

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }
}
