using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Customer.Models;

public class Address
{
    public Address() { }
    public Guid Id { get; set; }
    public string CEP { get; set; } = string.Empty;
    public string Rua { get; set; } = string.Empty;
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string Municipio { get; set; } = string.Empty;
    public State? Estado { get; set; }
    public Guid ClienteId { get; set; }
    public Customer? Cliente { get; set; } 
}
