using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace DevIO.OrderPay.Tests.WebApi;

public class ProductControllerIntegrationTests : IClassFixture<OrderPayWebApplicationFactory>
{
    private readonly OrderPayWebApplicationFactory _factory;
    private const string BaseUrl = "/api/v1/Product";

    public ProductControllerIntegrationTests(OrderPayWebApplicationFactory factory)
    {
        _factory = factory;
    }

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

    [Fact]
    public async Task Delete_CustomerRole_Returns403()
    {
        var client = _factory.CreateClientWithRoles("customer");

        var response = await client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── 400 — validation ─────────────────────────────────────

    [Fact]
    public async Task Post_EmptyName_Returns400()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var request = ValidRequest();
        request.Name = "";

        var response = await client.PostAsJsonAsync(BaseUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_InvalidSkuChars_Returns400()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var request = ValidRequest();
        request.SKU = "INVALID SKU!";

        var response = await client.PostAsJsonAsync(BaseUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 201 — create ─────────────────────────────────────────

    [Fact]
    public async Task Post_AdminRole_Returns201WithBody()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var request = ValidRequest();

        var response = await client.PostAsJsonAsync(BaseUrl, request);
        var body     = await response.Content.ReadFromJsonAsync<ProductResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Name.Should().Be(request.Name);
        body.SKU.Should().Be(request.SKU);
    }

    // ── 200 — read ───────────────────────────────────────────

    [Fact]
    public async Task GetAll_CustomerRole_Returns200()
    {
        var client = _factory.CreateClientWithRoles("customer");

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ExistingProduct_Returns200()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var created = await CreateProductAsync(client);

        var response = await client.GetAsync($"{BaseUrl}/{created!.Id}");
        var body     = await response.Content.ReadFromJsonAsync<ProductResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        var client = _factory.CreateClientWithRoles("admin");

        var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── 200 — update ─────────────────────────────────────────

    [Fact]
    public async Task Put_ExistingProduct_Returns200WithUpdatedData()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var created = await CreateProductAsync(client);
        var update  = new ProductRequest { Name = "Updated Name", SKU = $"UPD-{Guid.NewGuid():N}"[..10], Description = "Updated desc" };

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{created!.Id}", update);
        var body     = await response.Content.ReadFromJsonAsync<ProductResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Put_NonExistingId_Returns404()
    {
        var client = _factory.CreateClientWithRoles("admin");

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}", ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── 200 — delete ─────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingProduct_Returns200()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var created = await CreateProductAsync(client);

        var response = await client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_NonExistingId_Returns404()
    {
        var client = _factory.CreateClientWithRoles("admin");

        var response = await client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ───────────────────────────────────────────────

    private async Task<ProductResponse?> CreateProductAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(BaseUrl, ValidRequest());
        return await response.Content.ReadFromJsonAsync<ProductResponse>();
    }

    private static ProductRequest ValidRequest() => new()
    {
        Name        = "Integration Product",
        SKU         = $"SKU-{Guid.NewGuid():N}"[..12],
        Description = "Integration test product."
    };
}
