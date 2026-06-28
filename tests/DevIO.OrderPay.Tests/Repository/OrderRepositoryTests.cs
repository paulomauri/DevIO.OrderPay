using DevIO.OrderPay.Infra;
using DevIO.OrderPay.Infra.Repositories;
using DevIO.OrderPay.Order.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.Tests.Repository;

public class OrderRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new OrderRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private static Order.Models.Order NewOrder(Guid? customerId = null) => new(customerId ?? Guid.NewGuid())
    {
        Details = string.Empty,
        TotalPrice = new Price(0),
        TotalDiscount = new Price(0)
    };

    // ── GetByIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_OrderWithItems_EagerLoadsItems()
    {
        var order = NewOrder();
        order.AddItem(Guid.NewGuid(), 2, 50m, 5m);
        await _context.Order.AddAsync(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _repository.GetByIdAsync(order.Id);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── Price value object persistence ────────────────────────────────

    [Fact]
    public async Task AddAsync_Order_PersistsPriceValueObjectRoundtrip()
    {
        var order = NewOrder();
        order.TotalPrice = new Price(99.99m);
        order.TotalDiscount = new Price(9.99m);
        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _repository.GetByIdAsync(order.Id);

        loaded!.TotalPrice!.Value.Should().Be(99.99m);
        loaded.TotalDiscount!.Value.Should().Be(9.99m);
    }

    [Fact]
    public async Task AddAsync_OrderItem_PersistsItemPriceValueObjectRoundtrip()
    {
        var order = NewOrder();
        order.AddItem(Guid.NewGuid(), 3, 100m, 10m);
        await _context.Order.AddAsync(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _repository.GetByIdAsync(order.Id);

        var item = loaded!.Items.First();
        item.Price!.Value.Should().Be(100m);
        item.Discount!.Value.Should().Be(10m);
        item.Quantity.Should().Be(3);
    }

    // ── AddOrderItemAsync / RemoveOrderItemAsync ──────────────────────

    [Fact]
    public async Task AddOrderItemAsync_NewItem_IsPersistedToDb()
    {
        var order = NewOrder();
        await _context.Order.AddAsync(order);
        await _context.SaveChangesAsync();

        var newItem = new OrderItem(order.Id, Guid.NewGuid(), 1, 25m, 0m);
        await _repository.AddOrderItemAsync(newItem);
        await _repository.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _repository.GetByIdAsync(order.Id);
        loaded!.Items.Should().ContainSingle(i => i.Id == newItem.Id);
    }

    [Fact]
    public async Task RemoveOrderItemAsync_ExistingItem_IsRemovedFromDb()
    {
        var order = NewOrder();
        order.AddItem(Guid.NewGuid(), 2, 50m, 0m);
        await _context.Order.AddAsync(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loadedOrder = await _repository.GetByIdAsync(order.Id);
        var loadedItem = loadedOrder!.Items.First();
        await _repository.RemoveOrderItemAsync(loadedItem);
        await _repository.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _repository.GetByIdAsync(order.Id);
        reloaded!.Items.Should().BeEmpty();
    }

    // ── UpdateStatusAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ExistingOrder_UpdatesStatus()
    {
        var order = NewOrder();
        await _context.Order.AddAsync(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Pending → PaymentConfirmed is a valid transition (Pending → Shipped is not).
        bool result = await _repository.UpdateStatusAsync(order.Id, OrderStatus.PaymentConfirmed);
        await _repository.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        result.Should().BeTrue();
        var loaded = await _repository.GetByIdAsync(order.Id);
        loaded!.Status.Should().Be(OrderStatus.PaymentConfirmed);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistingOrder_ReturnsFalse()
    {
        bool result = await _repository.UpdateStatusAsync(Guid.NewGuid(), OrderStatus.Delivered);

        result.Should().BeFalse();
    }

    // ── UpdateDeliveryDateAsync ───────────────────────────────────────

    [Fact]
    public async Task UpdateDeliveryDateAsync_ExistingOrder_UpdatesDate()
    {
        var order = NewOrder();
        await _context.Order.AddAsync(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var deliveryDate = DateTime.UtcNow.AddDays(7);
        bool result = await _repository.UpdateDeliveryDateAsync(order.Id, deliveryDate);
        await _repository.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        result.Should().BeTrue();
        var loaded = await _repository.GetByIdAsync(order.Id);
        loaded!.DeliveryDate.Should().BeCloseTo(deliveryDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateDeliveryDateAsync_NonExistingOrder_ReturnsFalse()
    {
        bool result = await _repository.UpdateDeliveryDateAsync(Guid.NewGuid(), DateTime.UtcNow);

        result.Should().BeFalse();
    }

    // ── GetByCustomerIdAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetByCustomerIdAsync_MultipleOrders_ReturnsOnlyMatchingCustomer()
    {
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();

        await _context.Order.AddRangeAsync(
            NewOrder(customerId),
            NewOrder(customerId),
            NewOrder(otherCustomerId));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByCustomerIdAsync(customerId);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.CustomerId == customerId);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_NoOrders_ReturnsEmpty()
    {
        var result = await _repository.GetByCustomerIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    // ── Cascade delete ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_OrderWithItems_CascadeDeletesItems()
    {
        var order = NewOrder();
        order.AddItem(Guid.NewGuid(), 1, 20m, 0m);
        order.AddItem(Guid.NewGuid(), 2, 30m, 5m);
        await _context.Order.AddAsync(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _repository.DeleteAsync(order.Id);
        await _repository.SaveChangesAsync();

        _context.OrderItem.Should().BeEmpty();
        var loaded = await _repository.GetByIdAsync(order.Id);
        loaded.Should().BeNull();
    }
}
