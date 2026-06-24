using DevIO.OrderPay.Payment.Application.DTOs;
using FluentValidation;

namespace DevIO.OrderPay.Payment.Application.Validators;

public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
{
    public PaymentRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(x => x.CardBrand)
            .NotEmpty().WithMessage("Card brand is required.");

        RuleFor(x => x.Last4)
            .NotEmpty().WithMessage("Last 4 digits are required.")
            .Length(4).WithMessage("Last4 must be exactly 4 digits.")
            .Matches(@"^\d{4}$").WithMessage("Last4 must contain only digits.");

        RuleFor(x => x.Expiry)
            .NotEmpty().WithMessage("Expiry is required.")
            .Matches(@"^(0[1-9]|1[0-2])\/\d{2}$").WithMessage("Expiry must be in MM/YY format.");

        RuleFor(x => x.AttemptNumber)
            .GreaterThan(0).When(x => x.AttemptNumber.HasValue)
            .WithMessage("AttemptNumber must be greater than zero when provided.");
    }
}
