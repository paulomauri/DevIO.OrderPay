using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Application.Validators;
using FluentAssertions;

namespace DevIO.OrderPay.Tests.Application;

public class ProductRequestValidatorTests
{
    private readonly ProductRequestValidator _sut = new();

    private static ProductRequest ValidRequest() => new()
    {
        Name        = "Product A",
        SKU         = "PROD-001",
        Description = "A valid product description."
    };

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var result = _sut.Validate(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var request = ValidRequest();
        request.Name = "";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_EmptySku_Fails()
    {
        var request = ValidRequest();
        request.SKU = "";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Theory]
    [InlineData("PROD 001")]
    [InlineData("PROD@001")]
    [InlineData("PROD/001")]
    public void Validate_InvalidSkuChars_Fails(string sku)
    {
        var request = ValidRequest();
        request.SKU = sku;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public void Validate_EmptyDescription_Fails()
    {
        var request = ValidRequest();
        request.Description = "";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }
}
