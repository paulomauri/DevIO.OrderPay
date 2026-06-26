using System.Text.Json;
using DevIO.OrderPay.Payment.Models;

namespace DevIO.OrderPay.Infra;

// EF Core owned types can't be polymorphic, so the polymorphic PaymentMethod is
// persisted as a single JSON column. Serialization lives here (Infra) so the
// domain model stays persistence-ignorant.
internal static class PaymentMethodJson
{
    private sealed record MethodDto(
        string Kind,
        string Type,
        string? CardBrand,
        string? Last4,
        string? Expiry,
        string? Routing,
        string? AccountNumber,
        string? AccountType);

    public static string Serialize(PaymentMethod method) => method switch
    {
        PaymentMethodCard c => JsonSerializer.Serialize(
            new MethodDto("card", c.Type.ToString(), c.CardBrand, c.Last4, c.Expiry, null, null, null)),
        PaymentMethodACH a => JsonSerializer.Serialize(
            new MethodDto("ach", a.Type.ToString(), null, null, null,
                          a.RoutingTransitNumber, a.AccountNumber, a.AccountType)),
        _ => throw new NotSupportedException($"Unknown payment method: {method.GetType().Name}")
    };

    public static PaymentMethod Deserialize(string json)
    {
        MethodDto d = JsonSerializer.Deserialize<MethodDto>(json)
            ?? throw new InvalidOperationException("Could not deserialize payment method.");

        PaymentType type = Enum.Parse<PaymentType>(d.Type);

        return d.Kind switch
        {
            "card" => new PaymentMethodCard(d.CardBrand!, d.Last4!, d.Expiry!, type),
            "ach"  => new PaymentMethodACH(d.Routing!, d.AccountNumber!, d.AccountType!),
            _ => throw new NotSupportedException($"Unknown payment method kind: {d.Kind}")
        };
    }
}
