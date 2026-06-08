namespace DevIO.OrderPay.Order.Exceptions;

public class DuplicateOrderItemException : Exception    
{
    public DuplicateOrderItemException(string productId)
    : base($"The '{productId}' is already registered.") { }
}

