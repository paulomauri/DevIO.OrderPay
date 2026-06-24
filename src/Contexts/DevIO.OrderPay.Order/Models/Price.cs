using DevIO.OrderPay.Order.Exceptions;

namespace DevIO.OrderPay.Order.Models
{
    public class Price
    {
        public decimal Value { get; }
        public Price(decimal price)
        {
            if (price < 0)
                throw new ValueLowerThanZeroException(price);
            Value = price;
        }
    }
}
