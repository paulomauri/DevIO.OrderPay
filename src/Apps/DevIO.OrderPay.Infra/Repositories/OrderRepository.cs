using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Order.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DevIO.OrderPay.Infra.Repositories;

public class OrderRepository : Repository<Order.Models.Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context) { }

    public override async Task<Order.Models.Order?> GetByIdAsync(Guid id) =>
        await _dbSet.Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IEnumerable<Order.Models.Order>> GetByCustomerIdAsync(Guid customerId) =>
        await _dbSet.AsNoTracking()
                    .Include(o => o.Items)
                    .Where(o => o.CustomerId == customerId)
                    .ToListAsync();

    public async Task<bool> UpdateStatusAsync(Guid orderId, Order.Models.OrderStatus status)
    {
        var order = await _dbSet.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return false;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(order);
        return true;
    }

    public async Task<bool> UpdateDeliveryDateAsync(Guid orderId, DateTime deliveryDate)
    {
        var order = await _dbSet.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return false;

        order.DeliveryDate = deliveryDate;
        order.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(order);
        return true;
    }

    public async Task<bool> AddOrderItemAsync(OrderItem item)
    {
        var itemsDbSet = _context.Set<OrderItem>();
        itemsDbSet.Add(item);
        return await Task.FromResult(true);
    }

    public async Task<bool> RemoveOrderItemAsync(OrderItem item)
    {
        var itemsDbSet = _context.Set<OrderItem>();
        itemsDbSet.Remove(item);
        return await Task.FromResult(true);
    }
}
