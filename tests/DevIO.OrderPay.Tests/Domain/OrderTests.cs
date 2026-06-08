using DevIO.OrderPay.Order.Exceptions;
using DevIO.OrderPay.Order.Models;
using FluentAssertions;
using OrderModel = DevIO.OrderPay.Order.Models.Order;

namespace DevIO.OrderPay.Tests.Domain;

public class OrderTests
{
    private static OrderModel BuildOrder() => new(Guid.NewGuid());

    // ── AddItem ───────────────────────────────────────────────

    [Fact]
    public void AddItem_NewProduct_AddsItemToList()
    {
        var order = BuildOrder();
        var productId = Guid.NewGuid();

        order.AddItem(productId, 2, 10.00m, 0m);

        order.Items.Should().HaveCount(1);
        order.Items[0].ProductId.Should().Be(productId);
        order.Items[0].Quantity.Should().Be(2);
    }

    [Fact]
    public void AddItem_DuplicateProduct_ThrowsDuplicateOrderItemException()
    {
        var order = BuildOrder();
        var productId = Guid.NewGuid();

        order.AddItem(productId, 1, 10.00m, 0m);
        var act = () => order.AddItem(productId, 1, 10.00m, 0m);

        act.Should().Throw<DuplicateOrderItemException>();
    }

    [Fact]
    public void AddItem_NegativePrice_ThrowsValueLowerThanZeroException()
    {
        var order = BuildOrder();
        var act = () => order.AddItem(Guid.NewGuid(), 1, -5.00m, 0m);

        act.Should().Throw<ValueLowerThanZeroException>();
    }

    [Fact]
    public void AddItem_MultipleDistinctProducts_AddsAll()
    {
        var order = BuildOrder();

        order.AddItem(Guid.NewGuid(), 1, 10m, 0m);
        order.AddItem(Guid.NewGuid(), 2, 20m, 0m);
        order.AddItem(Guid.NewGuid(), 3, 30m, 0m);

        order.Items.Should().HaveCount(3);
    }

    // ── RemoveItem ────────────────────────────────────────────

    [Fact]
    public void RemoveItem_OneOfManyItems_RemovesIt()
    {
        var order = BuildOrder();
        order.AddItem(Guid.NewGuid(), 1, 10m, 0m);
        order.AddItem(Guid.NewGuid(), 1, 20m, 0m);
        var itemToRemove = order.Items[0];

        order.RemoveItem(itemToRemove);

        order.Items.Should().HaveCount(1);
        order.Items.Should().NotContain(itemToRemove);
    }

    [Fact]
    public void RemoveItem_LastRemainingItem_DoesNotRemoveIt()
    {
        var order = BuildOrder();
        order.AddItem(Guid.NewGuid(), 1, 10m, 0m);
        var onlyItem = order.Items[0];

        order.RemoveItem(onlyItem);

        order.Items.Should().HaveCount(1);
    }

    // ── Constructor ───────────────────────────────────────────

    [Fact]
    public void Order_NewInstance_HasPendingStatusAndEmptyItems()
    {
        var customerId = Guid.NewGuid();
        var order = new OrderModel(customerId);

        order.Id.Should().NotBeEmpty();
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().BeEmpty();
        order.OrderDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
