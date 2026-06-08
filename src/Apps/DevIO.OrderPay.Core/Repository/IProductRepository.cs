using DevIO.OrderPay.Order.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Core.Repository;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> UpdateSkuAsync(Guid productId, string value);
}
