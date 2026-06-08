namespace DevIO.OrderPay.Order.Application.DTOs;

public class OrderRequest
{
    public Guid CustomerId { get; set; }
    public string Details { get; set; } = string.Empty;
    public List<OrderItemRequest> Items { get; set; } = [];
}
