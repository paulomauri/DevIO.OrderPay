using DevIO.OrderPay.Core.Repository;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.Infra.Repositories;

public abstract class Repository<T>(AppDbContext context) : IRepository<T> where T : class
{
    protected AppDbContext Context { get; } = context;
    protected DbSet<T> DbSet { get; } = context.Set<T>();

    public async Task AddAsync(T entity) => await DbSet.AddAsync(entity);

    public Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null) DbSet.Remove(entity);
    }

    public virtual async Task<T?> GetByIdAsync(Guid id) => await DbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await DbSet.AsNoTracking().ToListAsync();

    public virtual async Task<int> SaveChangesAsync() => await Context.SaveChangesAsync();

    public void Dispose() => Context.Dispose();
}
