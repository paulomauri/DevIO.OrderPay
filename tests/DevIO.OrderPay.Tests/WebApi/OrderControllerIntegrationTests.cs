using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace DevIO.OrderPay.Tests.WebApi;

public class OrderControllerIntegrationTests : IClassFixture<OrderPayWebApplicationFactory>
{
    private readonly OrderPayWebApplicationFactory _factory;
    private const string BaseUrl = "/api/v1/Order";

    public OrderControllerIntegrationTests(OrderPayWebApplicationFactory factory)
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
    public async Task GetAll_CustomerRole_Returns403()
    {
        var client = _factory.CreateClientWithRoles("customer");

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_CustomerRole_Returns403()
    {
        var client = _factory.CreateClientWithRoles("customer");

        var response = await client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateStatus_CustomerRole_Returns403()
    {
        var client = _factory.CreateClientWithRoles("customer");

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}/status", "Confirmed");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── 400 — validation ─────────────────────────────────────

    [Fact]
    public async Task Post_NoItems_Returns400()
    {
        var client  = _factory.CreateClientWithRoles("customer");
        var request = ValidRequest();
        request.Items = [];

        var response = await client.PostAsJsonAsync(BaseUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_EmptyCustomerId_Returns400()
    {
        var client  = _factory.CreateClientWithRoles("customer");
        var request = ValidRequest();
        request.CustomerId = Guid.Empty;

        var response = await client.PostAsJsonAsync(BaseUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ItemQuantityZero_Returns400()
    {
        var client  = _factory.CreateClientWithRoles("customer");
        var request = ValidRequest();
        request.Items[0].Quantity = 0;

        var response = await client.PostAsJsonAsync(BaseUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatusString_Returns400()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var created = await CreateOrderAsync(client);

        var response = await client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}/status", "NotAValidStatus");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 201 — create ─────────────────────────────────────────

    [Fact]
    public async Task Post_CustomerRole_Returns201WithBody()
    {
        var client  = _factory.CreateClientWithRoles("customer");
        var request = ValidRequest();

        var response = await client.PostAsJsonAsync(BaseUrl, request);
        var body     = await response.Content.ReadFromJsonAsync<OrderResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Status.Should().Be("Pending");
        body.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Post_AdminRole_Returns201()
    {
        var client   = _factory.CreateClientWithRoles("admin");
        var response = await client.PostAsJsonAsync(BaseUrl, ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── 409 — duplicate item ──────────────────────────────────

    [Fact]
    public async Task Post_DuplicateProductId_Returns409()
    {
        var client    = _factory.CreateClientWithRoles("customer");
        var productId = Guid.NewGuid();
        var request   = new OrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items =
            [
                new OrderItemRequest { ProductId = productId, Quantity = 1, Price = 10m, Discount = 0m },
                new OrderItemRequest { ProductId = productId, Quantity = 2, Price = 10m, Discount = 0m }
            ]
        };

        var response = await client.PostAsJsonAsync(BaseUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── 200 — read ───────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingOrder_Returns200()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var created = await CreateOrderAsync(client);

        var response = await client.GetAsync($"{BaseUrl}/{created!.Id}");
        var body     = await response.Content.ReadFromJsonAsync<OrderResponse>();

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

    [Fact]
    public async Task GetByCustomer_Returns200()
    {
        var client     = _factory.CreateClientWithRoles("customer");
        var customerId = Guid.NewGuid();
        var request    = ValidRequest();
        request.CustomerId = customerId;
        await client.PostAsJsonAsync(BaseUrl, request);

        var response = await client.GetAsync($"{BaseUrl}/customer/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── status + delivery date ────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidStatus_Returns200()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var created = await CreateOrderAsync(client);

        var response = await client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}/status", "Confirmed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateDeliveryDate_ExistingOrder_Returns200()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var created = await CreateOrderAsync(client);

        var response = await client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}/delivery-date", DateTime.UtcNow.AddDays(3));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── items ─────────────────────────────────────────────────

    [Fact]
    public async Task AddItem_ExistingOrder_Returns200WithUpdatedItems()
    {
        var client  = _factory.CreateClientWithRoles("customer");
        var created = await CreateOrderAsync(client);
        var newItem = new OrderItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity  = 2,
            Price     = 5.00m,
            Discount  = 0m
        };

        var response = await client.PostAsJsonAsync($"{BaseUrl}/{created!.Id}/items", newItem);
        var body     = await response.Content.ReadFromJsonAsync<OrderResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveItem_ExistingItem_Returns200()
    {
        var client  = _factory.CreateClientWithRoles("customer");
        var created = await CreateOrderAsync(client);

        // Add a second item first (RemoveItem requires count > 1)
        var secondItem = new OrderItemRequest
        {
            ProductId = Guid.NewGuid(), Quantity = 1, Price = 5m, Discount = 0m
        };
        var addResponse = await client.PostAsJsonAsync($"{BaseUrl}/{created!.Id}/items", secondItem);
        var updated     = await addResponse.Content.ReadFromJsonAsync<OrderResponse>();
        var itemToRemove = updated!.Items[0];

        var response = await client.DeleteAsync($"{BaseUrl}/{created.Id}/items/{itemToRemove.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── delete ────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingOrder_Returns200()
    {
        var client  = _factory.CreateClientWithRoles("admin");
        var created = await CreateOrderAsync(client);

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

    private async Task<OrderResponse?> CreateOrderAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(BaseUrl, ValidRequest());
        return await response.Content.ReadFromJsonAsync<OrderResponse>();
    }

    private static OrderRequest ValidRequest() => new()
    {
        CustomerId = Guid.NewGuid(),
        Details    = "Integration test order",
        Items      =
        [
            new OrderItemRequest
            {
                ProductId = Guid.NewGuid(),
                Quantity  = 2,
                Price     = 10.00m,
                Discount  = 1.00m
            }
        ]
    };

}
