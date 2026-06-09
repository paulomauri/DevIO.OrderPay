using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Customer.Application.DTOs;
using DevIO.OrderPay.Customer.Exceptions;
using DevIO.OrderPay.Customer.Models;

namespace DevIO.OrderPay.Customer.Application.Services;

public class CustomerService(ICustomerRepository repository) : ICustomerService
{
    private readonly ICustomerRepository _repository = repository; // ← from Core

    public async Task<CustomerResponse?> GetByIdAsync(Guid id)
    {
        Models.Customer? cliente = await _repository.GetByIdAsync(id); // ← Core
        if (cliente is null) return null;

        return new CustomerResponse        // ← Application DTO
        {
            Id = cliente.Id,
            Name = cliente.Nome,
            Email = cliente.Email.Value,   // ← Domain model
            Mobile = cliente.Celular ?? ""
        };
    }

    public async Task<IEnumerable<CustomerResponse>> GetAllAsync()
    {
        IEnumerable<Models.Customer> customers = await _repository.GetAllAsync();

        return [.. customers.Select(MapToResponse)];
    }

    public async Task<CustomerResponse> AddAsync(CustomerRequest request)
    {
        Models.Customer? existing = await _repository.GetByCpfAsync(request.Cpf);
        if (existing is not null)
            throw new DuplicateCpfException(request.Cpf);

        var cliente = new Models.Customer(
            request.Name,
            new Email(request.Email),
            request.Cpf,
            request.Mobile);

        await _repository.AddAsync(cliente);
        await _repository.SaveChangesAsync();

        return MapToResponse(cliente);
    }

    public async Task<CustomerResponse?> UpdateAsync(Guid id, CustomerRequest request)
    {
        Models.Customer? cliente = await _repository.GetByIdAsync(id);
        if (cliente is null) return null;

        if (cliente.CPF != request.Cpf)
        {
            Models.Customer? withSameCpf = await _repository.GetByCpfAsync(request.Cpf);
            if (withSameCpf is not null)
                throw new DuplicateCpfException(request.Cpf);
        }

        cliente.Update(request.Name, request.Email, request.Cpf, request.Mobile);

        await _repository.UpdateAsync(cliente);
        await _repository.SaveChangesAsync();

        return MapToResponse(cliente);
    }

    public async Task<CustomerResponse?> PatchAsync(Guid id, CustomerInfoRequest request)
    {
        Models.Customer? cliente = await _repository.GetByIdAsync(id);
        if (cliente is null) return null;

        cliente.Update(cliente.Nome, request.Email, cliente.CPF, request.Mobile);

        await _repository.UpdateAsync(cliente);
        await _repository.SaveChangesAsync();

        return MapToResponse(cliente);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Models.Customer? customer = await _repository.GetByIdAsync(id);
        if (customer is null) return false;

        await _repository.DeleteAsync(customer.Id);
        await _repository.SaveChangesAsync();

        return true;
    }

    private static CustomerResponse MapToResponse(Customer.Models.Customer c) => new()
    {
        Id = c.Id,
        Name = c.Nome,
        Email = c.Email.Value,
        Mobile = c.Celular ?? ""
    };
}
