using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Customer.Application.DTOs;
using DevIO.OrderPay.Customer.Exceptions;
using DevIO.OrderPay.Customer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Customer.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository; // ← from Core

    public CustomerService(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<CustomerResponse?> GetByIdAsync(Guid id)
    {
        var cliente = await _repository.GetByIdAsync(id); // ← Core
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
        var customers = await _repository.GetAllAsync();

        return customers
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<CustomerResponse> AddAsync(CustomerRequest request)
    {
        var existing = await _repository.GetByCpfAsync(request.Cpf);
        if (existing is not null)
            throw new DuplicateCpfException(request.Cpf);

        var cliente = new Customer.Models.Customer(
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
        var cliente = await _repository.GetByIdAsync(id);
        if (cliente is null) return null;

        if (cliente.CPF != request.Cpf)
        {
            var withSameCpf = await _repository.GetByCpfAsync(request.Cpf);
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
        var cliente = await _repository.GetByIdAsync(id);
        if (cliente is null) return null;

        cliente.Update(cliente.Nome, request.Email, cliente.CPF, request.Mobile);

        await _repository.UpdateAsync(cliente);
        await _repository.SaveChangesAsync();

        return MapToResponse(cliente);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var customer = await _repository.GetByIdAsync(id);
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
