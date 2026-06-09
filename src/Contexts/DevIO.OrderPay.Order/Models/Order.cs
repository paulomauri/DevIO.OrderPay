using System.Diagnostics.CodeAnalysis;
using DevIO.OrderPay.Order.Exceptions;

namespace DevIO.OrderPay.Order.Models;

public class Order
{
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
    public List<OrderItem> Items { get; private set; } = [];
    public required OrderStatus Status { get; set; }
    public Customer? Customer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

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
