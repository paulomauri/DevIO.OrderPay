using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Order.Exceptions;
public class ValueLowerThanZeroException : Exception
{
    public ValueLowerThanZeroException(decimal value) 
        : base($"Value {value} should be greater than or equal zero.")
    { }
}
