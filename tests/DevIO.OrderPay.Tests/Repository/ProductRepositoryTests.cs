using DevIO.OrderPay.Infra;
using DevIO.OrderPay.Infra.Repositories;
using DevIO.OrderPay.Order.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevIO.OrderPay.Tests.Repository;

public class ProductRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new ProductRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── AddAsync / GetByIdAsync ───────────────────────────────────────

    [Fact]
    public async Task AddAsync_NewProduct_IsPersisted()
    {
        var product = new Product("Widget", "WGT001", "A nice widget");

        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _repository.GetByIdAsync(product.Id);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Widget");
        loaded.SKU.Should().Be("WGT001");
        loaded.Description.Should().Be("A nice widget");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_MultipleProducts_ReturnsAll()
    {
        await _context.Product.AddRangeAsync(
            new Product("Alpha", "SKU-A", "desc"),
            new Product("Beta", "SKU-B", "desc"),
            new Product("Gamma", "SKU-C", "desc"));
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmpty()
    {
        var result = await _repository.GetAllAsync();

        result.Should().BeEmpty();
    }

    // ── UpdateSkuAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSkuAsync_ExistingProduct_UpdatesSku()
    {
        var product = new Product("Widget", "SKU-OLD", "desc");
        await _context.Product.AddAsync(product);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        bool result = await _repository.UpdateSkuAsync(product.Id, "SKU-NEW");
        await _repository.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        result.Should().BeTrue();
        var loaded = await _repository.GetByIdAsync(product.Id);
        loaded!.SKU.Should().Be("SKU-NEW");
    }

    [Fact]
    public async Task UpdateSkuAsync_NonExistingProduct_ReturnsFalse()
    {
        bool result = await _repository.UpdateSkuAsync(Guid.NewGuid(), "SKU-X");

        result.Should().BeFalse();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingProduct_PersistsChanges()
    {
        var product = new Product("OldName", "SKU-UPD", "old desc");
        await _context.Product.AddAsync(product);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _repository.GetByIdAsync(product.Id);
        loaded!.Name = "NewName";
        loaded.Description = "new desc";
        loaded.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(loaded);
        await _repository.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _repository.GetByIdAsync(product.Id);
        reloaded!.Name.Should().Be("NewName");
        reloaded.Description.Should().Be("new desc");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingProduct_RemovesFromDb()
    {
        var product = new Product("ToDelete", "SKU-DEL", "desc");
        await _context.Product.AddAsync(product);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(product.Id);
        await _repository.SaveChangesAsync();

        var loaded = await _repository.GetByIdAsync(product.Id);
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingProduct_DoesNotThrow()
    {
        var act = async () =>
        {
            await _repository.DeleteAsync(Guid.NewGuid());
            await _repository.SaveChangesAsync();
        };

        await act.Should().NotThrowAsync();
    }
}
