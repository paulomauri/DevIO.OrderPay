using DevIO.OrderPay.Customer.Models;
using FluentAssertions;

namespace DevIO.OrderPay.Tests.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("user@test.com")]
    [InlineData("user.name+tag@example.co.uk")]
    [InlineData("admin@orderpay.com")]
    public void Email_ValidAddress_DoesNotThrow(string value)
    {
        var act = () => new Email(value);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("no-at-sign")]
    public void Email_InvalidAddress_ThrowsArgumentException(string value)
    {
        var act = () => new Email(value);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    public void Email_InvalidShouldNotBeEmpty_ThrowsArgumentException(string value)
    {
        var act = () => new Email(value);
        act.Should().Throw<ArgumentException>();
    }
}
