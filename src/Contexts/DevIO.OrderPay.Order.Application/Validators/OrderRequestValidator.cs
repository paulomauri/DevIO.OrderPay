using DevIO.OrderPay.Order.Application.DTOs;
using FluentValidation;

namespace DevIO.OrderPay.Order.Application.Validators;

public class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public OrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required.");

        RuleFor(x => x.Details)
            .MaximumLength(500).WithMessage("Details must not exceed 500 characters.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must have at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("ProductId is required.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

            item.RuleFor(i => i.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to zero.");

            item.RuleFor(i => i.Discount)
                .GreaterThanOrEqualTo(0).WithMessage("Discount must be greater than or equal to zero.")
                .Must((req, discount) => discount <= req.Price)
                    .WithMessage("Discount cannot exceed the item price.");
        });
    }
}
