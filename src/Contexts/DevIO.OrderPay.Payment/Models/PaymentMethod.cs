using System.ComponentModel;

namespace DevIO.OrderPay.Payment.Models;

public static class PaymentMethodConverter
{
    public static PaymentType? From(string? status)
    {
        if (status is null)
            return null;
        return Enum.TryParse<PaymentType>(status, out PaymentType parsedValue) ? parsedValue : null;
    }

    public static string ToStringValue(this PaymentType? status) =>
        status switch
        {
            PaymentType.ACH => "ACH",
            PaymentType.DEBIT => "DEBIT",
            PaymentType.CREDIT => "CREDIT",
            _ => status?.ToString() ?? ""
        };
}

public enum PaymentType
{
    [Description("ACH")]
    ACH,

    [Description("DEBIT")]
    DEBIT,

    [Description("CREDIT")]
    CREDIT
}

public abstract class PaymentMethod(PaymentType type)
{
    public PaymentType Type { get; } = type;
}

public sealed class PaymentMethodCard(string cardBrand, string last4, string expiry, PaymentType type) : PaymentMethod(type)
{
    public string CardBrand { get; } = cardBrand;
    public string Last4 { get; } = last4;
    public string Expiry { get; } = expiry;
}

public sealed class PaymentMethodACH(string routingTransitNumber, string accountNumber, string accountType) : PaymentMethod(PaymentType.ACH)
{
    public string RoutingTransitNumber { get; } = routingTransitNumber;
    public string AccountNumber { get; } = accountNumber;
    public string AccountType { get; } = accountType;
}
