using System.Diagnostics.CodeAnalysis;
using DevIO.OrderPay.Order.Events;
using DevIO.OrderPay.Order.Exceptions;
using DevIO.OrderPay.SharedKernel;

namespace DevIO.OrderPay.Order.Models;

public class Order : AggregateRoot
{
    // The aggregate is the ONLY place Status changes — every move is validated
    // against this map so illegal transitions can't reach the database.
    // (Pending → PaymentConfirmed is allowed because an order is paid straight from
    // Pending; AwaitingPayment is an optional intermediate.)
    private static readonly Dictionary<OrderStatus, OrderStatus[]> _allowedTransitions = new()
    {
        [OrderStatus.Pending]               = [OrderStatus.AwaitingPayment, OrderStatus.PaymentConfirmed, OrderStatus.Cancelled],
        [OrderStatus.AwaitingPayment]       = [OrderStatus.PaymentConfirmed, OrderStatus.Cancelled],
        [OrderStatus.PaymentConfirmed]      = [OrderStatus.Processing, OrderStatus.CancellationRequested],
        [OrderStatus.Processing]            = [OrderStatus.Shipped, OrderStatus.CancellationRequested],
        [OrderStatus.Shipped]               = [OrderStatus.Delivered, OrderStatus.CancellationRequested],
        [OrderStatus.Delivered]             = [],
        [OrderStatus.CancellationRequested] = [OrderStatus.Refunding, OrderStatus.Cancelled],
        [OrderStatus.Refunding]             = [OrderStatus.Cancelled],
        [OrderStatus.Cancelled]             = [],
    };

    public Order() { }

    [SetsRequiredMembers]
    public Order(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        OrderDate = DateTime.UtcNow;
        Status = OrderStatus.Pending;
        Items = [];
    }

    public required Guid Id { get; set; }
    public required Guid CustomerId { get; set; }
    public required DateTime OrderDate { get; set; }
    public string Details { get; set; } = string.Empty;
    public Price TotalPrice { get; set; } = new(0);
    public Price TotalDiscount { get; set; } = new(0);
    public DateTime? DeliveryDate { get; set; }
    public DateTime? DeliveredAt { get; private set; }
    public string? DeliveredBy { get; private set; }
    public List<OrderItem> Items { get; private set; } = [];
    public OrderStatus Status { get; private set; }
    public Customer? Customer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public void UpdateStatus(OrderStatus next)
    {
        if (Status == next) return; // idempotent — a replayed event re-applying the same status is a no-op

        if (!_allowedTransitions[Status].Contains(next))
            throw new InvalidOrderTransitionException(Status, next);

        Status = next;
        UpdatedAt = DateTime.UtcNow;

        RaiseStatusEvent(next);
    }

    private void RaiseStatusEvent(OrderStatus status)
    {
        switch (status)
        {
            case OrderStatus.PaymentConfirmed: RaiseEvent(new PaymentConfirmedEvent(Id)); break;
            case OrderStatus.Processing:       RaiseEvent(new OrderProcessingEvent(Id));  break;
            case OrderStatus.Shipped:          RaiseEvent(new OrderShippedEvent(Id));     break;
            case OrderStatus.Delivered:        RaiseEvent(new OrderDeliveredEvent(Id));   break;
            case OrderStatus.Cancelled:        RaiseEvent(new OrderCancelledEvent(Id));   break;
        }
    }

    // Transitions to Delivered and records who/when (used by the logistics webhook in Phase 9).
    public void MarkDelivered(DateTime deliveredAt, string? deliveredBy)
    {
        UpdateStatus(OrderStatus.Delivered);
        DeliveredAt = deliveredAt;
        DeliveredBy = deliveredBy;
    }

    public OrderItem AddItem(OrderItem orderItem)
    {
        Items ??= [];
        Items.Add(orderItem);
        return orderItem;
    }

    public void RemoveItem(OrderItem item) 
    {
        if (Items.Count > 1 && Items.Contains(item))  
            Items.Remove(item);
    }

    public OrderItem AddItem(Guid productId, int quantity, decimal price, decimal discount)
    {
        if (Items.Exists(p => p.ProductId == productId))
            throw new DuplicateOrderItemException(productId.ToString());

        var orderItem = new OrderItem(Id, productId, quantity, price, discount);
        return AddItem(orderItem);
    }
}
