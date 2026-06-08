using DevIO.OrderPay.Customer.Application.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Customer.Application.Validators;

public class CustomerRequestValidator : AbstractValidator<CustomerRequest>
{
    public CustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF is required.")
            .Length(11).WithMessage("CPF must be 11 digits.")
            .Matches(@"^\d+$").WithMessage("CPF must contain only digits.");

        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("Mobile is required.")
            .MinimumLength(10).WithMessage("Mobile phone must be at least 10 characters.")
            .MaximumLength(20).WithMessage("Mobile phone must not exceed 16 characters.");
    }
}
