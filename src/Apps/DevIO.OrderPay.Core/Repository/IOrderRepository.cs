using DevIO.OrderPay.Order.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Core.Repository;

public interface IOrderRepository : IRepository<Order.Models.Order>
{
    Task<bool> UpdateStatusAsync(Guid orderId, OrderStatus status);
    Task<bool> UpdateDeliveryDateAsync(Guid orderId, DateTime deliveryDate);
    Task<IEnumerable<Order.Models.Order>> GetByCustomerIdAsync(Guid customerId);
    Task<bool> AddOrderItemAsync(Order.Models.OrderItem item);
    Task<bool> RemoveOrderItemAsync(Order.Models.OrderItem item);
}
