using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Order.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Infra.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public async Task<bool> UpdateSkuAsync(Guid productId, string value)
    {
        var product = await _dbSet.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null) return false;

        product.SKU = value;
        product.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(product);
        return true;
    }
}
