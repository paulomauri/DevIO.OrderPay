using DevIO.OrderPay.Customer.Application.Services;
using DevIO.OrderPay.Customer.Application.DTOs;
using DevIO.OrderPay.Customer.Exceptions;
using DevIO.OrderPay.WebApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DevIO.OrderPay.Tests.WebApi;

public class CustomerControllerTests
{
    private readonly Mock<ICustomerService> _serviceMock = new();
    private readonly CustomerController _sut;

    public CustomerControllerTests()
    {
        _sut = new CustomerController(_serviceMock.Object);
    }

    // ── GET /{id} ────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_Returns200WithResponse()
    {
        var id = Guid.NewGuid();
        var response = new CustomerResponse { Id = id, Name = "João", Email = "joao@test.com", Mobile = "11999999999" };

        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(response);

        var result = await _sut.GetById(id);

        result.Should().BeOfType<OkObjectResult>()
              .Which.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync((CustomerResponse?)null);

        var result = await _sut.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── POST ─────────────────────────────────────────────────

    [Fact]
    public async Task Post_ValidRequest_Returns201()
    {
        var request  = ValidRequest();
        var response = new CustomerResponse { Id = Guid.NewGuid(), Name = request.Name, Email = request.Email };

        _serviceMock.Setup(s => s.AddAsync(request)).ReturnsAsync(response);

        var result = await _sut.Post(request);

        result.Should().BeOfType<CreatedAtActionResult>()
              .Which.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Post_DuplicateCpf_Returns409()
    {
        var request = ValidRequest();

        _serviceMock.Setup(s => s.AddAsync(request))
                    .ThrowsAsync(new DuplicateCpfException(request.Cpf));

        var result = await _sut.Post(request);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── PUT ──────────────────────────────────────────────────

    [Fact]
    public async Task Put_NonExistingId_Returns404()
    {
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<CustomerRequest>()))
                    .ReturnsAsync((CustomerResponse?)null);

        var result = await _sut.Put(Guid.NewGuid(), ValidRequest());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Put_DuplicateCpf_Returns409()
    {
        var id      = Guid.NewGuid();
        var request = ValidRequest();

        _serviceMock.Setup(s => s.UpdateAsync(id, request))
                    .ThrowsAsync(new DuplicateCpfException(request.Cpf));

        var result = await _sut.Put(id, request);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── Helpers ───────────────────────────────────────────────

    private static CustomerRequest ValidRequest() => new()
    {
        Name   = "João Silva",
        Email  = "joao@example.com",
        Cpf    = "12345678901",
        Mobile = "11999999999"
    };
}
