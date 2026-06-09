namespace DevIO.OrderPay.Order.Models;

public class Customer(Guid id, string name)
{
    public Guid CustomerId { get; set; } = id;
    public string Name { get; set; } = name;
}
