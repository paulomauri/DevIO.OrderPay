using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Payment.Application.DTOs;
using DevIO.OrderPay.Payment.Models;
using DevIO.OrderPay.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace DevIO.OrderPay.Tests.WebApi;

public class PaymentControllerIntegrationTests(OrderPayWebApplicationFactory factory)
    : IClassFixture<OrderPayWebApplicationFactory>
{
    private readonly OrderPayWebApplicationFactory _factory = factory;
    private const string PaymentUrl = "/api/v1/Payment";
    private const string OrderUrl = "/api/v1/Order";

    // ── 401 — no token ───────────────────────────────────────

    [Fact]
    public async Task Pay_NoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(PaymentUrl, PayRequest(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 200 — capture advances the order ─────────────────────

    [Fact]
    public async Task Pay_ValidRequest_CapturesPayment()
    {
        var client = _factory.CreateClientWithRoles("admin");
        Guid orderId = await CreateOrderAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(PaymentUrl, PayRequest(orderId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PaymentResponse? payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        payment!.Status.Should().Be("Captured");
        payment.AttemptOutcome.Should().Be("Succeeded");
        // The order advance to PaymentConfirmed now happens asynchronously via the Outbox
        // (covered by ConfirmOrderOnPaymentCaptured handler tests + the outbox interceptor tests).
    }

    // ── Idempotency: same attempt → no re-charge ─────────────

    [Fact]
    public async Task Pay_SameAttemptTwice_ReturnsSameGatewayReference()
    {
        var client = _factory.CreateClientWithRoles("admin");
        Guid orderId = await CreateOrderAsync(client);
        PaymentRequest request = PayRequest(orderId);

        PaymentResponse? first = await (await client.PostAsJsonAsync(PaymentUrl, request))
            .Content.ReadFromJsonAsync<PaymentResponse>();
        PaymentResponse? second = await (await client.PostAsJsonAsync(PaymentUrl, request))
            .Content.ReadFromJsonAsync<PaymentResponse>();

        second!.GatewayReference.Should().Be(first!.GatewayReference);
    }

    // ── Decline: failed attempt, payment stays retryable ─────

    [Fact]
    public async Task Pay_DeclinedCard_ReturnsFailedOutcome_PaymentPending()
    {
        var client = _factory.CreateClientWithRoles("admin");
        Guid orderId = await CreateOrderAsync(client);

        PaymentResponse? payment = await (await client.PostAsJsonAsync(PaymentUrl, PayRequest(orderId, last4: "0000")))
            .Content.ReadFromJsonAsync<PaymentResponse>();

        payment!.AttemptOutcome.Should().Be("Failed");
        payment.Status.Should().Be("Pending");
    }

    // ── 400 — validation ─────────────────────────────────────

    [Fact]
    public async Task Pay_InvalidLast4_Returns400()
    {
        var client = _factory.CreateClientWithRoles("admin");
        PaymentRequest request = PayRequest(Guid.NewGuid(), last4: "12");

        var response = await client.PostAsJsonAsync(PaymentUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── helpers ──────────────────────────────────────────────

    private static async Task<Guid> CreateOrderAsync(HttpClient client)
    {
        var request = new OrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Details = "integration",
            Items = [new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, Price = 100, Discount = 0 }],
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(OrderUrl, request);
        response.EnsureSuccessStatusCode();
        OrderResponse? order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        return order!.Id;
    }

    private static PaymentRequest PayRequest(Guid orderId, string last4 = "4242") => new()
    {
        OrderId = orderId,
        Amount = 100m,
        Currency = "USD",
        Type = PaymentType.CREDIT,
        CardBrand = "Visa",
        Last4 = last4,
        Expiry = "12/27",
        AttemptNumber = 1,
    };
}
