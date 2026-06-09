using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Application.Services;
using DevIO.OrderPay.Order.Exceptions;
using DevIO.OrderPay.Order.Models;
using FluentAssertions;
using Moq;

namespace DevIO.OrderPay.Tests.Application;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _repoMock = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_repoMock.Object);
    }

    // ── GetByIdAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMappedResponse()
    {
        var order = BuildOrder();
        _repoMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.GetByIdAsync(order.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.CustomerId.Should().Be(order.CustomerId);
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Order.Models.Order?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── AddAsync ─────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidRequest_CreatesOrderWithCorrectTotals()
    {
        var request = ValidRequest();

        var result = await _sut.AddAsync(request);

        result.Should().NotBeNull();
        result.CustomerId.Should().Be(request.CustomerId);
        result.TotalPrice.Should().Be(20.00m);    // 2 × 10.00
        result.TotalDiscount.Should().Be(2.00m);  // 2 × 1.00
        result.Items.Should().HaveCount(1);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Order.Models.Order>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddAsync_DuplicateProductId_ThrowsDuplicateOrderItemException()
    {
        var productId = Guid.NewGuid();
        var request = new OrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items =
            [
                new OrderItemRequest { ProductId = productId, Quantity = 1, Price = 10m, Discount = 0m },
                new OrderItemRequest { ProductId = productId, Quantity = 1, Price = 10m, Discount = 0m }
            ]
        };

        var act = async () => await _sut.AddAsync(request);

        await act.Should().ThrowAsync<DuplicateOrderItemException>();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Order.Models.Order>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_MultipleItemsDifferentDiscounts_TotalDiscountIsSumNotMax()
    {
        var request = new OrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items =
            [
                new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 2, Price = 10m, Discount = 1m },
                new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 3, Price = 8m,  Discount = 3m }
            ]
        };

        var result = await _sut.AddAsync(request);

        // Sum(1×2, 3×3) = 2+9 = 11 — Max would be 9
        result.TotalDiscount.Should().Be(11m);
        // Sum(10×2, 8×3) = 20+24 = 44 — Max would be 24
        result.TotalPrice.Should().Be(44m);
    }

    // ── DeleteAsync ───────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndCallsRepository()
    {
        var order = BuildOrder();
        _repoMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        bool result = await _sut.DeleteAsync(order.Id);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.DeleteAsync(order.Id), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Order.Models.Order?)null);

        bool result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── UpdateStatusAsync ─────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Order.Models.Order?)null);

        var result = await _sut.UpdateStatusAsync(Guid.NewGuid(), OrderStatus.Confirmed);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ExistingId_ReturnsUpdatedStatus()
    {
        var order = BuildOrder();
        _repoMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _repoMock.Setup(r => r.UpdateStatusAsync(order.Id, OrderStatus.Confirmed))
                 .ReturnsAsync(true);

        var result = await _sut.UpdateStatusAsync(order.Id, OrderStatus.Confirmed);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Confirmed");
        _repoMock.Verify(r => r.UpdateStatusAsync(order.Id, OrderStatus.Confirmed), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── UpdateDeliveryDateAsync ───────────────────────────────

    [Fact]
    public async Task UpdateDeliveryDateAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Order.Models.Order?)null);

        var result = await _sut.UpdateDeliveryDateAsync(Guid.NewGuid(), DateTime.UtcNow);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateDeliveryDateAsync_ExistingId_ReturnsUpdatedDateAndCallsRepository()
    {
        var order = BuildOrder();
        var deliveryDate = DateTime.UtcNow.AddDays(5);
        _repoMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _repoMock.Setup(r => r.UpdateDeliveryDateAsync(order.Id, deliveryDate))
                 .ReturnsAsync(true);

        var result = await _sut.UpdateDeliveryDateAsync(order.Id, deliveryDate);

        result.Should().NotBeNull();
        result!.DeliveryDate.Should().Be(deliveryDate);
        _repoMock.Verify(r => r.UpdateDeliveryDateAsync(order.Id, deliveryDate), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── AddItemAsync ──────────────────────────────────────────

    [Fact]
    public async Task AddItemAsync_ExistingOrder_AddsItemAndRecalculatesTotals()
    {
        var order = BuildOrder();
        _repoMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _repoMock.Setup(r => r.AddOrderItemAsync(It.IsAny<OrderItem>())).ReturnsAsync(true);

        var newItem = new OrderItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity  = 3,
            Price     = 5.00m,
            Discount  = 0m
        };

        var result = await _sut.AddItemAsync(order.Id, newItem);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalPrice.Should().Be(35.00m);    // (2×10) + (3×5) = 20+15 — Max would be 20
        result.TotalDiscount.Should().Be(2.00m);  // (2×1) + (3×0)
        _repoMock.Verify(r => r.AddOrderItemAsync(It.IsAny<OrderItem>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_NonExistingOrder_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Order.Models.Order?)null);

        var result = await _sut.AddItemAsync(Guid.NewGuid(), new OrderItemRequest
        {
            ProductId = Guid.NewGuid(), Quantity = 1, Price = 10m, Discount = 0m
        });

        result.Should().BeNull();
    }

    // ── RemoveItemAsync ───────────────────────────────────────

    [Fact]
    public async Task RemoveItemAsync_ExistingItem_RemovesAndRecalculatesTotals()
    {
        var order = BuildOrder();
        order.AddItem(Guid.NewGuid(), 3, 5.00m, 0m); // 2nd item
        var itemToRemove = order.Items[0];

        _repoMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _repoMock.Setup(r => r.RemoveOrderItemAsync(It.IsAny<OrderItem>())).ReturnsAsync(true);

        var result = await _sut.RemoveItemAsync(order.Id, itemToRemove.Id);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.TotalPrice.Should().Be(15.00m);    // only 3×5
        result.TotalDiscount.Should().Be(0m);
        _repoMock.Verify(r => r.RemoveOrderItemAsync(itemToRemove), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveItemAsync_ThreeItems_TotalsAreSumOfRemainingTwo()
    {
        // 3 items — remove 1 — 2 remain so Sum ≠ Max is provable
        var order = BuildOrder();                                    // Item0: qty=2 price=10 discount=1
        order.AddItem(Guid.NewGuid(), 3, 7.00m, 3.00m);             // Item1: qty=3 price=7  discount=3
        order.AddItem(Guid.NewGuid(), 1, 5.00m, 1.00m);             // Item2: qty=1 price=5  discount=1 (to remove)
        var itemToRemove = order.Items[2];

        _repoMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _repoMock.Setup(r => r.RemoveOrderItemAsync(It.IsAny<OrderItem>())).ReturnsAsync(true);

        var result = await _sut.RemoveItemAsync(order.Id, itemToRemove.Id);

        // Remaining: Item0(2×10=20) + Item1(3×7=21) → Sum=41, Max would be 21
        result!.TotalPrice.Should().Be(41.00m);
        // Remaining: Item0(2×1=2) + Item1(3×3=9) → Sum=11, Max would be 9
        result.TotalDiscount.Should().Be(11.00m);
    }

    [Fact]
    public async Task RemoveItemAsync_NonExistingItem_ReturnsNull()
    {
        var order = BuildOrder();
        _repoMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.RemoveItemAsync(order.Id, Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────

    private static OrderRequest ValidRequest() => new()
    {
        CustomerId = Guid.NewGuid(),
        Details    = "Test order",
        Items      =
        [
            new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 2, Price = 10.00m, Discount = 1.00m }
        ]
    };

    private static Order.Models.Order BuildOrder()
    {
        var order = new Order.Models.Order(Guid.NewGuid());
        order.AddItem(Guid.NewGuid(), 2, 10.00m, 1.00m);
        order.TotalPrice    = new Price(20.00m);
        order.TotalDiscount = new Price(2.00m);
        return order;
    }
}
