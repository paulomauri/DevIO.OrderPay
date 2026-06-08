using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Customer.Application.DTOs;

public class CustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
}
