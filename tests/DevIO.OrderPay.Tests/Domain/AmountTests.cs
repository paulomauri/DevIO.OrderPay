using DevIO.OrderPay.Payment.Exceptions;
using DevIO.OrderPay.Payment.Models;
using FluentAssertions;

namespace DevIO.OrderPay.Tests.Domain;

public class AmountTests
{
    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-9999)]
    public void Amount_NegativeValue_ThrowsValueLowerThanZeroException(decimal value)
    {
        var act = () => new Amount(value, "USD");
        act.Should().Throw<ValueLowerThanZeroException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Amount_MissingCurrency_DefaultsToUsd(string? currency)
    {
        var amount = new Amount(10m, currency!);
        amount.Currency.Should().Be("USD");
    }

    [Fact]
    public void Amount_StoresValueAndCurrency()
    {
        var amount = new Amount(42.50m, "EUR");

        amount.Value.Should().Be(42.50m);
        amount.Currency.Should().Be("EUR");
    }
}
