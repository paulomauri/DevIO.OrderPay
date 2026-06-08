using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Order.Models;

public class Product
{
    public Product() { }
    public Product(string name, string sKU, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        SKU = sKU;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }
    public string Name { get; set; } 
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; }
    public DateTime? CreatedAt { get; set; } 
    public DateTime? UpdatedAt { get; set; }

}
