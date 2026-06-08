using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Order.Models;

public class Customer
{
    public Customer(Guid id,  string name)
    {
        CustomerId = id;
        Name = name;
    }
    public Guid CustomerId { get; set; }
    public string Name { get; set; }
}
