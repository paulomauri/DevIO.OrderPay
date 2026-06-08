using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Models;

namespace DevIO.OrderPay.Order.Application.Services;

public interface IOrderService
{
    Task<IEnumerable<OrderResponse>> GetAllAsync();
    Task<OrderResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<OrderResponse>> GetByCustomerIdAsync(Guid customerId);
    Task<OrderResponse> AddAsync(OrderRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<OrderResponse?> UpdateStatusAsync(Guid id, OrderStatus status);
    Task<OrderResponse?> UpdateDeliveryDateAsync(Guid id, DateTime deliveryDate);
    Task<OrderResponse?> AddItemAsync(Guid orderId, OrderItemRequest item);
    Task<OrderResponse?> RemoveItemAsync(Guid orderId, Guid itemId);
}
