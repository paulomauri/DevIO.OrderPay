using DevIO.OrderPay.Order.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

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
