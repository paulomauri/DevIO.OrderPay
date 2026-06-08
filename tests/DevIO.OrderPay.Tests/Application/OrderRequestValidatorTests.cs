using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Application.Validators;
using FluentAssertions;

namespace DevIO.OrderPay.Tests.Application;

public class OrderRequestValidatorTests
{
    private readonly OrderRequestValidator _sut = new();

    private static OrderRequest ValidRequest() => new()
    {
        CustomerId = Guid.NewGuid(),
        Details    = "Order notes",
        Items      =
        [
            new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 2, Price = 10.00m, Discount = 1.00m }
        ]
    };

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var result = _sut.Validate(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCustomerId_Fails()
    {
        var request = ValidRequest();
        request.CustomerId = Guid.Empty;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void Validate_NoItems_Fails()
    {
        var request = ValidRequest();
        request.Items = [];

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void Validate_ItemQuantityZero_Fails()
    {
        var request = ValidRequest();
        request.Items[0].Quantity = 0;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ItemNegativePrice_Fails()
    {
        var request = ValidRequest();
        request.Items[0].Price = -1;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ItemDiscountExceedsPrice_Fails()
    {
        var request = ValidRequest();
        request.Items[0].Price    = 5.00m;
        request.Items[0].Discount = 10.00m;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DetailsOverLimit_Fails()
    {
        var request = ValidRequest();
        request.Details = new string('x', 501);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Details");
    }

    [Fact]
    public void Validate_DiscountEqualToPrice_IsValid()
    {
        // Boundary: discount == price is allowed (≤ not <)
        var request = ValidRequest();
        request.Items[0].Price    = 10.00m;
        request.Items[0].Discount = 10.00m;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PriceZero_IsValid()
    {
        // Price ≥ 0 — zero price is valid
        var request = ValidRequest();
        request.Items[0].Price    = 0m;
        request.Items[0].Discount = 0m;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
