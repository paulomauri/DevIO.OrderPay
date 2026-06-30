using DevIO.OrderPay.Core.Gateway;
using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Payment.Application.DTOs;
using DevIO.OrderPay.Payment.Application.Services;
using DevIO.OrderPay.Payment.Models;
using DevIO.OrderPay.SharedKernel.Contracts;
using FluentAssertions;
using Moq;

namespace DevIO.OrderPay.Tests.Application;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _repo = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly PaymentService _sut;
    private Payment.Models.Payment? _savedPayment; // captured from UpdateAsync to inspect raised events

    public PaymentServiceTests()
    {
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Payment.Models.Payment>()))
             .Callback<Payment.Models.Payment>(p => _savedPayment = p)
             .Returns(Task.CompletedTask);
        _sut = new PaymentService(_repo.Object, _gateway.Object);
    }

    private static PaymentRequest Request(Guid orderId, int? attemptNumber = null) => new()
    {
        OrderId = orderId,
        Amount = 100m,
        Currency = "USD",
        Type = PaymentType.CREDIT,
        CardBrand = "Visa",
        Last4 = "4242",
        Expiry = "12/27",
        AttemptNumber = attemptNumber,
    };

    private static PaymentMethod Card() =>
        new PaymentMethodCard("Visa", "4242", "12/27", PaymentType.CREDIT);

    private void SetupNoExisting(Guid orderId)
    {
        _repo.Setup(r => r.GetAttemptsByOrderIdAsync(orderId)).ReturnsAsync([]);
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync((Payment.Models.Payment?)null);
        _repo.Setup(r => r.GetAttemptByKeyAsync(It.IsAny<string>())).ReturnsAsync((PaymentAttempt?)null);
    }

    private void SetupGateway(PaymentGatewayResult result) =>
        _gateway.Setup(g => g.ChargeAsync(
                    It.IsAny<string>(), It.IsAny<Amount>(), It.IsAny<PaymentMethod>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

    // ── Happy path ────────────────────────────────────────────

    [Fact]
    public async Task PayAsync_Approved_CapturesAndRaisesCapturedEvent()
    {
        var orderId = Guid.NewGuid();
        SetupNoExisting(orderId);
        SetupGateway(PaymentGatewayResult.Approve("ref_1"));

        var result = await _sut.PayAsync(Request(orderId));

        result.Status.Should().Be("Captured");
        result.AttemptOutcome.Should().Be("Succeeded");
        result.GatewayReference.Should().Be("ref_1");
        // Capture raises the integration event; the Outbox interceptor persists it on save.
        _savedPayment!.DomainEvents.OfType<PaymentCapturedEvent>()
            .Should().ContainSingle(e => e.OrderId == orderId);
    }

    // ── Idempotency: the core guarantee ───────────────────────

    [Fact]
    public async Task PayAsync_AttemptAlreadyResolved_DoesNotChargeAgain()
    {
        var orderId = Guid.NewGuid();
        var resolved = new PaymentAttempt(orderId, 1);
        resolved.RecordSuccess("ref_1");

        var payment = new Payment.Models.Payment(orderId, new Amount(100m, "USD"), Card());
        payment.BeginProcessing();
        payment.Authorize("ref_1");
        payment.Capture();

        _repo.Setup(r => r.GetAttemptsByOrderIdAsync(orderId)).ReturnsAsync([resolved]);
        _repo.Setup(r => r.GetByOrderIdAsync(orderId)).ReturnsAsync(payment);
        _repo.Setup(r => r.GetAttemptByKeyAsync($"{orderId}:1")).ReturnsAsync(resolved);

        var result = await _sut.PayAsync(Request(orderId, attemptNumber: 1));

        // Replayed from the stored attempt — no charge.
        _gateway.Verify(g => g.ChargeAsync(
            It.IsAny<string>(), It.IsAny<Amount>(), It.IsAny<PaymentMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Status.Should().Be("Captured");
    }

    // ── Decline path ──────────────────────────────────────────

    [Fact]
    public async Task PayAsync_Declined_MarksAttemptFailed_PaymentStaysRetryable_NoEvent()
    {
        var orderId = Guid.NewGuid();
        SetupNoExisting(orderId);
        SetupGateway(PaymentGatewayResult.Decline("Card declined."));

        var result = await _sut.PayAsync(Request(orderId));

        result.AttemptOutcome.Should().Be("Failed");
        result.Status.Should().Be("Pending");      // Decline() returns the payment to Pending
        _savedPayment!.DomainEvents.OfType<PaymentCapturedEvent>().Should().BeEmpty(); // no capture event on decline
    }

    // ── At-most-once for a single attempt ─────────────────────

    [Fact]
    public async Task PayAsync_SameAttemptNumber_ChargesGatewayOnce()
    {
        var orderId = Guid.NewGuid();
        SetupNoExisting(orderId);
        SetupGateway(PaymentGatewayResult.Approve("ref_1"));

        await _sut.PayAsync(Request(orderId, attemptNumber: 1));

        _gateway.Verify(g => g.ChargeAsync(
                $"{orderId}:1", It.IsAny<Amount>(), It.IsAny<PaymentMethod>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
