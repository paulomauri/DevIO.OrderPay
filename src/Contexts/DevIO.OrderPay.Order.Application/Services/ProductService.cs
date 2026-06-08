using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Models;

namespace DevIO.OrderPay.Order.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ProductResponse>> GetAllAsync()
    {
        var products = await _repository.GetAllAsync();
        return products.Select(MapToResponse).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        return product is null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse> AddAsync(ProductRequest request)
    {
        var product = new Product(request.Name, request.SKU, request.Description);

        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();

        return MapToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, ProductRequest request)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product is null) return null;

        product.Name = request.Name;
        product.SKU = request.SKU;
        product.Description = request.Description;
        product.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(product);
        await _repository.SaveChangesAsync();

        return MapToResponse(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product is null) return false;

        await _repository.DeleteAsync(id);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<ProductResponse?> UpdateSkuAsync(Guid id, string sku)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product is null) return null;

        await _repository.UpdateSkuAsync(id, sku);
        await _repository.SaveChangesAsync();

        product.SKU = sku;
        product.UpdatedAt = DateTime.UtcNow;

        return MapToResponse(product);
    }

    private static ProductResponse MapToResponse(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        SKU = p.SKU,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
