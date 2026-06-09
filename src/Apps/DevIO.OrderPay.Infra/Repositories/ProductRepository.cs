using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Order.Models;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.Infra.Repositories;

public class ProductRepository(AppDbContext context)
    : Repository<Product>(context), IProductRepository
{
    public async Task<bool> UpdateSkuAsync(Guid productId, string value)
    {
        var product = await DbSet.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null) return false;

        product.SKU = value;
        product.UpdatedAt = DateTime.UtcNow;
        DbSet.Update(product);
        return true;
    }
}
