using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DevIO.OrderPay.Order.Models;

public static class OrderStatusConverter
{
    public static OrderStatus? From(string? status)
    {
        if (status is null) return null;
        return Enum
            .TryParse<OrderStatus>(status, out var parsedStatus) ? parsedStatus : null;
    }

    public static string ToStringValue(this OrderStatus? status)
    {
        return status switch
        {
            OrderStatus.Pending => "Pending",
            OrderStatus.Confirmed => "Confirmed",
            OrderStatus.Cancelled => "Cancelled",
            OrderStatus.Shipped => "Shipped",
            OrderStatus.InRoute => "InRoute",
            OrderStatus.Delivered => "Delivered",
            _ => status?.ToString() ?? ""
        };
    }
}
public enum OrderStatus
{
    [Description("Pending")]
    Pending,
    [Description("Confirmed")]
    Confirmed,
    [Description("Cancelled")]
    Cancelled,
    [Description("Shipped")]
    Shipped,
    [Description("In Route")]
    InRoute,
    [Description("Delivered")]
    Delivered
}
