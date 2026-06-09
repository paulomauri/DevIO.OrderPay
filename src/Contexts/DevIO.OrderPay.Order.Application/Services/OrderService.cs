using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Models;

namespace DevIO.OrderPay.Order.Application.Services;

public class OrderService(IOrderRepository repository) : IOrderService
{
    private readonly IOrderRepository _repository = repository;

    public async Task<IEnumerable<OrderResponse>> GetAllAsync()
    {
        IEnumerable<Models.Order> orders = await _repository.GetAllAsync();
        return [.. orders.Select(MapToResponse)];
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid id)
    {
        Models.Order? order = await _repository.GetByIdAsync(id);
        return order is null ? null : MapToResponse(order);
    }

    public async Task<IEnumerable<OrderResponse>> GetByCustomerIdAsync(Guid customerId)
    {
        IEnumerable<Models.Order> orders = await _repository.GetByCustomerIdAsync(customerId);
        return [.. orders.Select(MapToResponse)];
    }

    public async Task<OrderResponse> AddAsync(OrderRequest request)
    {
        var order = new Models.Order(request.CustomerId) { Details = request.Details };

        foreach (OrderItemRequest item in request.Items)
            order.AddItem(item.ProductId, item.Quantity, item.Price, item.Discount);

        order.TotalPrice = new Price(order.Items.Sum(i => i.Price.Value * i.Quantity));
        order.TotalDiscount = new Price(order.Items.Sum(i => i.Discount.Value * i.Quantity));
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync();

        return MapToResponse(order);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Models.Order? order = await _repository.GetByIdAsync(id);
        if (order is null) return false;

        await _repository.DeleteAsync(id);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<OrderResponse?> UpdateStatusAsync(Guid id, OrderStatus status)
    {
        Models.Order? order = await _repository.GetByIdAsync(id);
        if (order is null) return null;

        await _repository.UpdateStatusAsync(id, status);
        await _repository.SaveChangesAsync();

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        return MapToResponse(order);
    }

    public async Task<OrderResponse?> UpdateDeliveryDateAsync(Guid id, DateTime deliveryDate)
    {
        Models.Order? order = await _repository.GetByIdAsync(id);
        if (order is null) return null;

        await _repository.UpdateDeliveryDateAsync(id, deliveryDate);
        await _repository.SaveChangesAsync();

        order.DeliveryDate = deliveryDate;
        order.UpdatedAt = DateTime.UtcNow;

        return MapToResponse(order);
    }

    public async Task<OrderResponse?> AddItemAsync(Guid orderId, OrderItemRequest item)
    {
        Models.Order? order = await _repository.GetByIdAsync(orderId);
        if (order is null) return null;

        order.AddItem(item.ProductId, item.Quantity, item.Price, item.Discount);
        OrderItem addedItem = order.Items.Last();
        await _repository.AddOrderItemAsync(addedItem);

        order.TotalPrice = new Price(order.Items.Sum(i => (i.Price?.Value ?? 0m) * i.Quantity));
        order.TotalDiscount = new Price(order.Items.Sum(i => (i.Discount?.Value ?? 0m) * i.Quantity));
        order.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync();

        return MapToResponse(order);
    }

    public async Task<OrderResponse?> RemoveItemAsync(Guid orderId, Guid itemId)
    {
        Models.Order? order = await _repository.GetByIdAsync(orderId);
        if (order is null) return null;

        OrderItem? item = order.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return null;

        order.RemoveItem(item);
        await _repository.RemoveOrderItemAsync(item);

        order.TotalPrice = new Price(order.Items.Sum(i => (i.Price?.Value ?? 0m) * i.Quantity));
        order.TotalDiscount = new Price(order.Items.Sum(i => (i.Discount?.Value ?? 0m) * i.Quantity));
        order.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync();

        return MapToResponse(order);
    }

    private static OrderResponse MapToResponse(Models.Order o) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        Details = o.Details ?? string.Empty,
        OrderDate = o.OrderDate,
        TotalPrice = o.TotalPrice?.Value ?? 0,
        TotalDiscount = o.TotalDiscount?.Value ?? 0,
        DeliveryDate = o.DeliveryDate,
        Status = ((OrderStatus?)o.Status).ToStringValue(),
        Items = o.Items?.Select(i => new OrderItemResponse
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            Price = i.Price?.Value ?? 0,
            Discount = i.Discount?.Value ?? 0
        }).ToList() ?? [],
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt
    };
}
