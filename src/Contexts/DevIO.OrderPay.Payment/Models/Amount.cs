using DevIO.OrderPay.Payment.Exceptions;

namespace DevIO.OrderPay.Payment.Models
{
    public class Amount
    {
        public decimal Value { get; }
        public string Currency { get; }
        // Parameter names must match the property names (value → Value, currency →
        // Currency) so EF Core can bind this constructor when materializing the owned type.
        public Amount(decimal value, string currency)
        {
            if (value < 0)
                throw new ValueLowerThanZeroException(value);
            Value = value;
            Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency;
        }
    }
}
