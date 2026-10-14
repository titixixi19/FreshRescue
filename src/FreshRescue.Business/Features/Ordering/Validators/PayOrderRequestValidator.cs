using FluentValidation;
using FreshRescue.Business.Features.Ordering.Dtos;

namespace FreshRescue.Business.Features.Ordering.Validators;

public class PayOrderRequestValidator : AbstractValidator<PayOrderRequest>
{
    public PayOrderRequestValidator()
    {
        RuleFor(x => x.Outcome).IsInEnum().WithMessage("outcome phải là Success hoặc Failure.");
    }
}
