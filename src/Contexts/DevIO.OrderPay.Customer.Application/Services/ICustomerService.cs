using DevIO.OrderPay.Customer.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Customer.Application.Services;

public interface ICustomerService
{
    Task<IEnumerable<CustomerResponse>> GetAllAsync();
    Task<CustomerResponse?> GetByIdAsync(Guid id);
    Task<CustomerResponse> AddAsync(CustomerRequest request);
    Task<CustomerResponse?> UpdateAsync(Guid id, CustomerRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<CustomerResponse?> PatchAsync(Guid id, CustomerInfoRequest request);
}
