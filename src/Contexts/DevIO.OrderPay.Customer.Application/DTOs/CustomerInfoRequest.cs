using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Customer.Application.DTOs;

public class CustomerInfoRequest
{
    public string Mobile { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
