using DevIO.OrderPay.Payment.Exceptions;

namespace DevIO.OrderPay.Payment.Models
{
    public class Amount
    {
        public decimal Value { get; }
        public string Currency { get; }
        public Amount(decimal amount, string currency)
        {
            if (amount < 0)
                throw new ValueLowerThanZeroException(amount);
            Value = amount;
            Currency = string.IsNullOrEmpty(currency.Trim()) ? "USD" : currency;
        }
    }
}
