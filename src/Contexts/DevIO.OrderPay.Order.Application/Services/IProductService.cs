using DevIO.OrderPay.Order.Application.DTOs;

namespace DevIO.OrderPay.Order.Application.Services;

public interface IProductService
{
    Task<IEnumerable<ProductResponse>> GetAllAsync();
    Task<ProductResponse?> GetByIdAsync(Guid id);
    Task<ProductResponse> AddAsync(ProductRequest request);
    Task<ProductResponse?> UpdateAsync(Guid id, ProductRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<ProductResponse?> UpdateSkuAsync(Guid id, string sku);
}
