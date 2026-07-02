using DevIO.OrderPay.Core.Repository;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.Infra.Repositories;

public class CustomerRepository(AppDbContext context)
    : Repository<Customer.Models.Customer>(context), ICustomerRepository
{
    public async Task<Customer.Models.Customer?> GetByEmailAsync(string email) =>
        await DbSet.AsNoTracking().FirstOrDefaultAsync(c => c.Email.Value == email);

    public async Task<Customer.Models.Customer?> GetByCpfAsync(string cpf) =>
        await DbSet.AsNoTracking().FirstOrDefaultAsync(c => c.CPF == cpf);

    public async Task<Customer.Models.Customer?> GetByIdWithAddressAsync(Guid id) =>
        await DbSet.AsNoTracking()
            .Include(c => c.Enderecos)
            .FirstOrDefaultAsync(c => c.Id == id);
}
