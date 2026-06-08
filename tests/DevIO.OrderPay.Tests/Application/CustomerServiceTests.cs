using DevIO.OrderPay.Customer.Application.Services;
using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Customer.Application.DTOs;
using DevIO.OrderPay.Customer.Exceptions;
using DevIO.OrderPay.Customer.Models;
using FluentAssertions;
using Moq;

namespace DevIO.OrderPay.Tests.Application;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _repoMock = new();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _sut = new CustomerService(_repoMock.Object);
    }

    // ── GetByIdAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMappedResponse()
    {
        var id = Guid.NewGuid();
        var customer = BuildCustomer(id);

        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(customer);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Name.Should().Be(customer.Nome);
        result.Email.Should().Be(customer.Email.Value);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Customer.Models.Customer?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── AddAsync ─────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_NewCpf_AddsAndReturnsResponse()
    {
        var request = ValidRequest();

        _repoMock.Setup(r => r.GetByCpfAsync(request.Cpf))
                 .ReturnsAsync((Customer.Models.Customer?)null);

        var result = await _sut.AddAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.Email.Should().Be(request.Email);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Customer.Models.Customer>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddAsync_DuplicateCpf_ThrowsDuplicateCpfException()
    {
        var request = ValidRequest();
        var existing = BuildCustomer(cpf: request.Cpf);

        _repoMock.Setup(r => r.GetByCpfAsync(request.Cpf)).ReturnsAsync(existing);

        var act = async () => await _sut.AddAsync(request);

        await act.Should().ThrowAsync<DuplicateCpfException>();

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Customer.Models.Customer>()), Times.Never);
    }

    // ── UpdateAsync ───────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Customer.Models.Customer?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), ValidRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ChangedCpfAlreadyTaken_ThrowsDuplicateCpfException()
    {
        var id = Guid.NewGuid();
        var existingCustomer = BuildCustomer(id, cpf: "11111111111");
        var anotherCustomer  = BuildCustomer(cpf: "22222222222");
        var request = ValidRequest();
        request.Cpf = "22222222222"; // new CPF already belongs to another customer

        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingCustomer);
        _repoMock.Setup(r => r.GetByCpfAsync("22222222222")).ReturnsAsync(anotherCustomer);

        var act = async () => await _sut.UpdateAsync(id, request);

        await act.Should().ThrowAsync<DuplicateCpfException>();
    }

    // ── Helpers ───────────────────────────────────────────────

    private static CustomerRequest ValidRequest() => new()
    {
        Name   = "João Silva",
        Email  = "joao@example.com",
        Cpf    = "12345678901",
        Mobile = "11999999999"
    };

    private static Customer.Models.Customer BuildCustomer(
        Guid? id = null, string cpf = "12345678901")
    {
        var c = new Customer.Models.Customer(
            "João Silva",
            new Email("joao@example.com"),
            cpf,
            "11999999999");

        if (id.HasValue) c.Id = id.Value;

        return c;
    }
}
