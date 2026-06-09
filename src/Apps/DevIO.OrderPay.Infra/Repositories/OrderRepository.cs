using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Order.Models;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.Infra.Repositories;

public class OrderRepository(AppDbContext context)
    : Repository<Order.Models.Order>(context), IOrderRepository
{
    public override async Task<Order.Models.Order?> GetByIdAsync(Guid id) =>
        await DbSet.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IEnumerable<Order.Models.Order>> GetByCustomerIdAsync(Guid customerId) =>
        await DbSet.AsNoTracking()
                   .Include(o => o.Items)
                   .Where(o => o.CustomerId == customerId)
                   .ToListAsync();

    public async Task<bool> UpdateStatusAsync(Guid orderId, Order.Models.OrderStatus status)
    {
        var order = await DbSet.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return false;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        DbSet.Update(order);
        return true;
    }

    public async Task<bool> UpdateDeliveryDateAsync(Guid orderId, DateTime deliveryDate)
    {
        var order = await DbSet.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return false;

        order.DeliveryDate = deliveryDate;
        order.UpdatedAt = DateTime.UtcNow;
        DbSet.Update(order);
        return true;
    }

    public async Task<bool> AddOrderItemAsync(OrderItem item)
    {
        Context.Set<OrderItem>().Add(item);
        return await Task.FromResult(true);
    }

    public async Task<bool> RemoveOrderItemAsync(OrderItem item)
    {
        Context.Set<OrderItem>().Remove(item);
        return await Task.FromResult(true);
    }
}
