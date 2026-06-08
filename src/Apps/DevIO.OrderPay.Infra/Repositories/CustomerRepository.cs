using DevIO.OrderPay.Core.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Infra.Repositories;

// Implementation in Infra
public class CustomerRepository : Repository<Customer.Models.Customer>, Core.Repository.ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public async Task<Customer.Models.Customer?> GetByEmailAsync(string email) =>
        await _dbSet.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email.Value == email);

    public async Task<Customer.Models.Customer?> GetByCpfAsync(string cpf) =>
        await _dbSet.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CPF == cpf);
}
