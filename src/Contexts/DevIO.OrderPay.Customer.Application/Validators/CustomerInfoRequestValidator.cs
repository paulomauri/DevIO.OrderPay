using DevIO.OrderPay.Customer.Application.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.OrderPay.Customer.Application.Validators;

public class CustomerInfoRequestValidator : AbstractValidator<CustomerInfoRequest>
{
    public CustomerInfoRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("Mobile is required.")
            .MinimumLength(10).WithMessage("Mobile phone must be at least 10 characters.")
            .MaximumLength(20).WithMessage("Mobile phone must not exceed 16 characters.");
    }
}
