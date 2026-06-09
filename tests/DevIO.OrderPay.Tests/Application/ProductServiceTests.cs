using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Application.Services;
using DevIO.OrderPay.Order.Models;
using FluentAssertions;
using Moq;

namespace DevIO.OrderPay.Tests.Application;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repoMock = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_repoMock.Object);
    }

    // ── GetByIdAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMappedResponse()
    {
        var product = BuildProduct();
        _repoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be(product.Name);
        result.SKU.Should().Be(product.SKU);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Product?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── AddAsync ─────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidRequest_AddsAndReturnsResponse()
    {
        var request = ValidRequest();

        var result = await _sut.AddAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.SKU.Should().Be(request.SKU);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── UpdateAsync ───────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Product?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), ValidRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesAndReturnsResponse()
    {
        var product = BuildProduct();
        var request = new ProductRequest { Name = "Updated", SKU = "NEW-001", Description = "Updated desc" };
        _repoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var result = await _sut.UpdateAsync(product.Id, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        result.SKU.Should().Be("NEW-001");
        _repoMock.Verify(r => r.UpdateAsync(product), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── DeleteAsync ───────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Product?)null);

        bool result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndDeletes()
    {
        var product = BuildProduct();
        _repoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        bool result = await _sut.DeleteAsync(product.Id);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.DeleteAsync(product.Id), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── UpdateSkuAsync ────────────────────────────────────────

    [Fact]
    public async Task UpdateSkuAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Product?)null);

        var result = await _sut.UpdateSkuAsync(Guid.NewGuid(), "NEW-SKU");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSkuAsync_ExistingId_UpdatesSkuAndReturns()
    {
        var product = BuildProduct();
        _repoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _repoMock.Setup(r => r.UpdateSkuAsync(product.Id, "NEW-SKU")).Returns(Task.FromResult(true));

        var result = await _sut.UpdateSkuAsync(product.Id, "NEW-SKU");

        result.Should().NotBeNull();
        result!.SKU.Should().Be("NEW-SKU");
        _repoMock.Verify(r => r.UpdateSkuAsync(product.Id, "NEW-SKU"), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────

    private static ProductRequest ValidRequest() => new()
    {
        Name        = "Product A",
        SKU         = "PROD-001",
        Description = "A valid description."
    };

    private static Product BuildProduct() =>
        new("Product A", "PROD-001", "A valid description.");
}
