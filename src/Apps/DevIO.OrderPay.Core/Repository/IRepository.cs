using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Core.Repository;

public interface IRepository<T> : IDisposable where T : class
{
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<int> SaveChangesAsync();
}