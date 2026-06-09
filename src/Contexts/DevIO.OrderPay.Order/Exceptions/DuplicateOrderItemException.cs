namespace DevIO.OrderPay.Order.Exceptions;

public class DuplicateOrderItemException(string productId) : Exception($"The '{productId}' is already registered.")    
{
}

