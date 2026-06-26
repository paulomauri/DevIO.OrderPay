using DevIO.OrderPay.Core.Gateway;
using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Payment.Application.DTOs;
using DevIO.OrderPay.Payment.Application.Integration;
using DevIO.OrderPay.Payment.Application.Services;
using DevIO.OrderPay.Payment.Models;
using FluentAssertions;
using Moq;

namespace DevIO.OrderPay.Tests.Application;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _repo = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IPaymentCapturedHandler> _captured = new();
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _sut = new PaymentService(_repo.Object, _gateway.Object, _captured.Object);
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
        _captured.Verify(h => h.HandleAsync(
            It.Is<PaymentCapturedEvent>(e => e.OrderId == orderId), It.IsAny<CancellationToken>()), Times.Once);
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

        // Replayed from the stored attempt — no charge, no event.
        _gateway.Verify(g => g.ChargeAsync(
            It.IsAny<string>(), It.IsAny<Amount>(), It.IsAny<PaymentMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        _captured.Verify(h => h.HandleAsync(
            It.IsAny<PaymentCapturedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _captured.Verify(h => h.HandleAsync(
            It.IsAny<PaymentCapturedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
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
