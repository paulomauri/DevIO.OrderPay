using System.ComponentModel;

namespace DevIO.OrderPay.Payment.Models;

public static class PaymentStatusConverter
{
    public static PaymentStatus? From(string? status)
    {
        if (status is null)
            return null;
        return Enum.TryParse<PaymentStatus>(status, out PaymentStatus parsedStatus) ? parsedStatus : null;
    }

    public static string ToStringValue(this PaymentStatus? status) =>
        status switch
        {
            PaymentStatus.Pending => "Pending",
            PaymentStatus.Processing => "Processing",
            PaymentStatus.Authorized => "Authorized",
            PaymentStatus.Captured => "Captured",
            PaymentStatus.Refunded => "Refunded",
            PaymentStatus.Failed => "Failed",
             _ => status?.ToString() ?? ""
        };
}
public enum PaymentStatus
{
    [Description("Pending")]
    Pending,

    [Description("Processing")]
    Processing,

    [Description("Authorized")]
    Authorized,

    [Description("Captured")]
    Captured,

    [Description("Refunded")]
    Refunded,

    [Description("Failed")]
    Failed
}
