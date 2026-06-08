using DevIO.OrderPay.Order.Exceptions;
using DevIO.OrderPay.Order.Models;
using FluentAssertions;

namespace DevIO.OrderPay.Tests.Domain;

public class PriceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(100)]
    [InlineData(9999.99)]
    public void Price_ValidValue_DoesNotThrow(decimal value)
    {
        var act = () => new Price(value);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-9999)]
    public void Price_NegativeValue_ThrowsValueLowerThanZeroException(decimal value)
    {
        var act = () => new Price(value);
        act.Should().Throw<ValueLowerThanZeroException>();
    }

    [Fact]
    public void Price_StoresValue_Correctly()
    {
        var price = new Price(42.50m);
        price.Value.Should().Be(42.50m);
    }
}
