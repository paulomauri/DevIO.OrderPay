using DevIO.OrderPay.Order.Models;

namespace DevIO.OrderPay.Order.Exceptions;

// Thrown by Order.UpdateStatus when a status change isn't allowed by the state machine.
public class InvalidOrderTransitionException(OrderStatus from, OrderStatus to)
    : Exception($"Invalid order transition from {from} to {to}.")
{
    public OrderStatus From { get; } = from;
    public OrderStatus To { get; } = to;
}
