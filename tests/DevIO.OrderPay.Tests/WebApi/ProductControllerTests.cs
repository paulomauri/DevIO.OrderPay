using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Application.Services;
using DevIO.OrderPay.WebApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DevIO.OrderPay.Tests.WebApi;

public class ProductControllerTests
{
    private readonly Mock<IProductService> _serviceMock = new();
    private readonly ProductController _sut;

    public ProductControllerTests()
    {
        _sut = new ProductController(_serviceMock.Object);
    }

    // ── GET ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Always_Returns200WithList()
    {
        _serviceMock.Setup(s => s.GetAllAsync())
                    .ReturnsAsync([BuildResponse(), BuildResponse()]);

        var result = await _sut.GetAll();

        result.Should().BeOfType<OkObjectResult>()
              .Which.Value.Should().BeAssignableTo<IEnumerable<ProductResponse>>()
              .Which.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ExistingId_Returns200()
    {
        var response = BuildResponse();
        _serviceMock.Setup(s => s.GetByIdAsync(response.Id)).ReturnsAsync(response);

        var result = await _sut.GetById(response.Id);

        result.Should().BeOfType<OkObjectResult>()
              .Which.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync((ProductResponse?)null);

        var result = await _sut.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── POST ─────────────────────────────────────────────────

    [Fact]
    public async Task Post_ValidRequest_Returns201()
    {
        var request  = ValidRequest();
        var response = BuildResponse();
        _serviceMock.Setup(s => s.AddAsync(request)).ReturnsAsync(response);

        var result = await _sut.Post(request);

        result.Should().BeOfType<CreatedAtActionResult>()
              .Which.Value.Should().BeEquivalentTo(response);
    }

    // ── DELETE ────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingId_Returns200()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _sut.Delete(id);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_NonExistingId_Returns404()
    {
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _sut.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── PATCH /sku ────────────────────────────────────────────

    [Fact]
    public async Task UpdateSku_ExistingId_Returns200()
    {
        var response = BuildResponse();
        _serviceMock.Setup(s => s.UpdateSkuAsync(response.Id, "NEW-001")).ReturnsAsync(response);

        var result = await _sut.UpdateSku(response.Id, "NEW-001");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateSku_NonExistingId_Returns404()
    {
        _serviceMock.Setup(s => s.UpdateSkuAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                    .ReturnsAsync((ProductResponse?)null);

        var result = await _sut.UpdateSku(Guid.NewGuid(), "NEW-001");

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── Helpers ───────────────────────────────────────────────

    private static ProductRequest ValidRequest() => new()
    {
        Name = "Product A", SKU = "PROD-001", Description = "Valid description."
    };

    private static ProductResponse BuildResponse() => new()
    {
        Id = Guid.NewGuid(), Name = "Product A", SKU = "PROD-001", Description = "Valid description."
    };
}
