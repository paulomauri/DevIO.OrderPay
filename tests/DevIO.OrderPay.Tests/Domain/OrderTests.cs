using DevIO.OrderPay.Order.Events;
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

    // ── UpdateStatus (state machine) ──────────────────────────

    [Fact]
    public void UpdateStatus_LegalTransition_Succeeds()
    {
        var order = BuildOrder(); // Pending

        order.UpdateStatus(OrderStatus.PaymentConfirmed);
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]      // Pending → Shipped
    [InlineData(OrderStatus.Delivered)]    // Pending → Delivered
    [InlineData(OrderStatus.Refunding)]    // Pending → Refunding
    public void UpdateStatus_IllegalTransition_Throws(OrderStatus illegal)
    {
        var order = BuildOrder(); // Pending

        var act = () => order.UpdateStatus(illegal);

        act.Should().Throw<InvalidOrderTransitionException>();
    }

    [Fact]
    public void UpdateStatus_SameStatus_IsNoOp()
    {
        var order = BuildOrder();

        var act = () => order.UpdateStatus(OrderStatus.Pending);

        act.Should().NotThrow();
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void UpdateStatus_FromTerminalState_Throws()
    {
        var order = BuildOrder();
        order.UpdateStatus(OrderStatus.PaymentConfirmed);
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        var act = () => order.UpdateStatus(OrderStatus.Processing);

        act.Should().Throw<InvalidOrderTransitionException>();
    }

    [Fact]
    public void MarkDelivered_SetsStatusAndDeliveryFields()
    {
        var order = BuildOrder();
        order.UpdateStatus(OrderStatus.PaymentConfirmed);
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        var when = DateTime.UtcNow;

        order.MarkDelivered(when, "DHL");

        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().Be(when);
        order.DeliveredBy.Should().Be("DHL");
    }

    [Fact]
    public void MarkDelivered_FromPending_Throws()
    {
        var order = BuildOrder(); // Pending → can't jump to Delivered

        var act = () => order.MarkDelivered(DateTime.UtcNow, "DHL");

        act.Should().Throw<InvalidOrderTransitionException>();
    }

    // ── Domain events ─────────────────────────────────────────

    [Fact]
    public void UpdateStatus_ToPaymentConfirmed_RaisesPaymentConfirmedEvent()
    {
        var order = BuildOrder();

        order.UpdateStatus(OrderStatus.PaymentConfirmed);

        order.DomainEvents.OfType<PaymentConfirmedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void UpdateStatus_ToProcessing_RaisesOrderProcessingEvent()
    {
        var order = BuildOrder();
        order.UpdateStatus(OrderStatus.PaymentConfirmed);
        order.ClearDomainEvents();

        order.UpdateStatus(OrderStatus.Processing);

        order.DomainEvents.OfType<OrderProcessingEvent>().Should().ContainSingle(oe => oe.OrderId == order.Id);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheList()
    {
        var order = BuildOrder();
        order.UpdateStatus(OrderStatus.PaymentConfirmed);

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }
}
