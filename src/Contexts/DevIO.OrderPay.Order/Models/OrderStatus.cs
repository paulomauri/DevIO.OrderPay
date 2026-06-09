using System.ComponentModel;

namespace DevIO.OrderPay.Order.Models;

public static class OrderStatusConverter
{
    public static OrderStatus? From(string? status)
    {
        if (status is null) return null;
        return Enum.TryParse<OrderStatus>(status, out OrderStatus parsedStatus) ? parsedStatus : null;
    }

    public static string ToStringValue(this OrderStatus? status) =>
        status switch
        {
            OrderStatus.Pending                => "Pending",
            OrderStatus.AwaitingPayment        => "AwaitingPayment",
            OrderStatus.PaymentConfirmed       => "PaymentConfirmed",
            OrderStatus.Processing             => "Processing",
            OrderStatus.Shipped                => "Shipped",
            OrderStatus.Delivered              => "Delivered",
            OrderStatus.CancellationRequested  => "CancellationRequested",
            OrderStatus.Refunding              => "Refunding",
            OrderStatus.Cancelled              => "Cancelled",
            _                                  => status?.ToString() ?? ""
        };
}

public enum OrderStatus
{
    [Description("Pending")]
    Pending,

    [Description("Awaiting Payment")]
    AwaitingPayment,

    [Description("Payment Confirmed")]
    PaymentConfirmed,

    [Description("Processing")]
    Processing,

    [Description("Shipped")]
    Shipped,

    [Description("Delivered")]
    Delivered,

    [Description("Cancellation Requested")]
    CancellationRequested,

    [Description("Refunding")]
    Refunding,

    [Description("Cancelled")]
    Cancelled
}
