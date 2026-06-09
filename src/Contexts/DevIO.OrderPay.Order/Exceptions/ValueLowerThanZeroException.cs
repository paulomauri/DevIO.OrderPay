namespace DevIO.OrderPay.Order.Exceptions;
public class ValueLowerThanZeroException(decimal value) : Exception($"Value {value} should be greater than or equal zero.")
{
}
