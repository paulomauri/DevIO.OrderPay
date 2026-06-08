using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Application.Services;
using DevIO.OrderPay.Order.Exceptions;
using DevIO.OrderPay.Order.Models;
using DevIO.OrderPay.WebApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DevIO.OrderPay.Tests.WebApi;

public class OrderControllerTests
{
    private readonly Mock<IOrderService> _serviceMock = new();
    private readonly OrderController _sut;

    public OrderControllerTests()
    {
        _sut = new OrderController(_serviceMock.Object);
    }

    // ── GET /{id} ─────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_Returns200()
    {
        var response = BuildResponse();
        _serviceMock.Setup(s => s.GetByIdAsync(response.Id)).ReturnsAsync(response);

        var result = await _sut.GetById(response.Id);

        result.Should().BeOfType<OkObjectResult>()
              .Which.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync((OrderResponse?)null);

        var result = await _sut.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── POST ─────────────────────────────────────────────────

    [Fact]
    public async Task Post_ValidRequest_Returns201()
    {
        var request  = ValidRequest();
        var response = BuildResponse();
        _serviceMock.Setup(s => s.AddAsync(request)).ReturnsAsync(response);

        var result = await _sut.Post(request);

        result.Should().BeOfType<CreatedAtActionResult>()
              .Which.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Post_DuplicateItem_Returns409()
    {
        var request = ValidRequest();
        _serviceMock.Setup(s => s.AddAsync(request))
                    .ThrowsAsync(new DuplicateOrderItemException(Guid.NewGuid().ToString()));

        var result = await _sut.Post(request);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Post_NegativePrice_Returns400()
    {
        var request = ValidRequest();
        _serviceMock.Setup(s => s.AddAsync(request))
                    .ThrowsAsync(new ValueLowerThanZeroException(-1m));

        var result = await _sut.Post(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── DELETE ────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingId_Returns200()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _sut.Delete(id);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_NonExistingId_Returns404()
    {
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _sut.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── PATCH /status ─────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidStatus_Returns200()
    {
        var response = BuildResponse();
        _serviceMock.Setup(s => s.UpdateStatusAsync(response.Id, OrderStatus.Confirmed))
                    .ReturnsAsync(response);

        var result = await _sut.UpdateStatus(response.Id, "Confirmed");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatusString_Returns400()
    {
        var result = await _sut.UpdateStatus(Guid.NewGuid(), "InvalidStatus");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_NonExistingId_Returns404()
    {
        _serviceMock.Setup(s => s.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<OrderStatus>()))
                    .ReturnsAsync((OrderResponse?)null);

        var result = await _sut.UpdateStatus(Guid.NewGuid(), "Confirmed");

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── POST /{id}/items ──────────────────────────────────────

    [Fact]
    public async Task AddItem_ExistingOrder_Returns200()
    {
        var response = BuildResponse();
        var item     = new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, Price = 10m, Discount = 0m };
        _serviceMock.Setup(s => s.AddItemAsync(response.Id, item)).ReturnsAsync(response);

        var result = await _sut.AddItem(response.Id, item);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddItem_DuplicateProduct_Returns409()
    {
        var item = new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, Price = 10m, Discount = 0m };
        _serviceMock.Setup(s => s.AddItemAsync(It.IsAny<Guid>(), item))
                    .ThrowsAsync(new DuplicateOrderItemException(item.ProductId.ToString()));

        var result = await _sut.AddItem(Guid.NewGuid(), item);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── DELETE /{id}/items/{itemId} ───────────────────────────

    [Fact]
    public async Task RemoveItem_ExistingItem_Returns200()
    {
        var orderId  = Guid.NewGuid();
        var itemId   = Guid.NewGuid();
        var response = BuildResponse();
        _serviceMock.Setup(s => s.RemoveItemAsync(orderId, itemId)).ReturnsAsync(response);

        var result = await _sut.RemoveItem(orderId, itemId);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveItem_NonExistingItem_Returns404()
    {
        _serviceMock.Setup(s => s.RemoveItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                    .ReturnsAsync((OrderResponse?)null);

        var result = await _sut.RemoveItem(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── Helpers ───────────────────────────────────────────────

    private static OrderRequest ValidRequest() => new()
    {
        CustomerId = Guid.NewGuid(),
        Details    = "Test order",
        Items      = [new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, Price = 10m, Discount = 0m }]
    };

    private static OrderResponse BuildResponse() => new()
    {
        Id         = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Status     = "Pending",
        Items      = []
    };
}
