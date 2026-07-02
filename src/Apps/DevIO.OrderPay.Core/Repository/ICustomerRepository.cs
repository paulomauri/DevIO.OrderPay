using DevIO.OrderPay.Customer.Models;
using DevIO.OrderPay.Core.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Core.Repository;

public interface ICustomerRepository : IRepository<Customer.Models.Customer>
{
    Task<Customer.Models.Customer?> GetByEmailAsync(string email);
    Task<Customer.Models.Customer?> GetByCpfAsync(string cpf);

    // Loads the customer with its addresses eagerly — the base GetByIdAsync (FindAsync)
    // does not include Enderecos, which the logistics dispatch needs for the ship-to.
    Task<Customer.Models.Customer?> GetByIdWithAddressAsync(Guid id);
}

