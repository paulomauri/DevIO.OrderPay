using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Order.Models;

public class OrderItem
{
    public OrderItem () { }
    public OrderItem(Guid orderId, Guid productId, int quantity, decimal price, decimal discount)
    {
        Id = Guid.NewGuid ();
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        Price = new Price(price);
        Discount = new Price(discount);
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public Price Price { get; set; }
    public Price Discount { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
