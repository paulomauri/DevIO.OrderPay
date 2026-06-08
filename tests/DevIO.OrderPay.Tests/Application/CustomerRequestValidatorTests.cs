using DevIO.OrderPay.Customer.Application.DTOs;
using DevIO.OrderPay.Customer.Application.Validators;
using FluentAssertions;

namespace DevIO.OrderPay.Tests.Application;

public class CustomerRequestValidatorTests
{
    private readonly CustomerRequestValidator _sut = new();

    private static CustomerRequest ValidRequest() => new()
    {
        Name   = "João Silva",
        Email  = "joao@example.com",
        Cpf    = "12345678901",
        Mobile = "11999999999"
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
    public void Validate_InvalidEmail_Fails()
    {
        var request = ValidRequest();
        request.Email = "notanemail";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_CpfTooShort_Fails()
    {
        var request = ValidRequest();
        request.Cpf = "1234";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cpf");
    }

    [Fact]
    public void Validate_CpfNonNumeric_Fails()
    {
        var request = ValidRequest();
        request.Cpf = "abc.def.ghi";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cpf");
    }

    [Fact]
    public void Validate_CpfExactly11Digits_Passes()
    {
        var request = ValidRequest();
        request.Cpf = "98765432100";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
