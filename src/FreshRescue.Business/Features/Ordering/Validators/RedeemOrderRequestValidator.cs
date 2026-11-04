using FluentValidation;
using FreshRescue.Business.Features.Ordering.Dtos;

namespace FreshRescue.Business.Features.Ordering.Validators;

public class RedeemOrderRequestValidator : AbstractValidator<RedeemOrderRequest>
{
    public RedeemOrderRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.Token) != string.IsNullOrWhiteSpace(x.OrderCode))
            .OverridePropertyName("token")
            .WithMessage("Gửi đúng một trong hai: token hoặc orderCode.");
    }
}
