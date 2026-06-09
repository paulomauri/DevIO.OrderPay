using DevIO.OrderPay.Customer.Application.DTOs;
using DevIO.OrderPay.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace DevIO.OrderPay.Tests.WebApi;

public class CustomerControllerIntegrationTests(OrderPayWebApplicationFactory factory)
    : IClassFixture<OrderPayWebApplicationFactory>
{
    private readonly OrderPayWebApplicationFactory _factory = factory;
    private const string BaseUrl = "/api/v1/Customer";

    // ── 401 — no token ───────────────────────────────────────

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_NoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(BaseUrl, ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 403 — wrong role ─────────────────────────────────────

    [Fact]
    public async Task Post_CustomerRole_Returns403()
    {
        var client = _factory.CreateClientWithRoles("customer");

        var response = await client.PostAsJsonAsync(BaseUrl, ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── 201 — admin role ─────────────────────────────────────

    [Fact]
    public async Task Post_AdminRole_Returns201()
    {
        var client = _factory.CreateClientWithRoles("admin");

        var response = await client.PostAsJsonAsync(BaseUrl, ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Helpers ───────────────────────────────────────────────

    private static CustomerRequest ValidRequest() => new()
    {
        Name   = "Integration Test User",
        Email  = $"test-{Guid.NewGuid()}@example.com",
        Cpf    = RandomCpf(),
        Mobile = "11999999999"
    };

    // Generates a unique 11-digit CPF string per test to avoid duplicate conflicts
    private static string RandomCpf() =>
        string.Concat(Guid.NewGuid().ToString("N").Where(char.IsDigit).Take(11));
}
