using FluentValidation;
using FreshRescue.Business.Features.Ordering.Dtos;

namespace FreshRescue.Business.Features.Ordering.Validators;

public class CancelOrderByStoreRequestValidator : AbstractValidator<CancelOrderByStoreRequest>
{
    public const int MaxReasonLength = 500;

    public CancelOrderByStoreRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Phải nhập lý do huỷ đơn.")
            .MaximumLength(MaxReasonLength).WithMessage($"Lý do tối đa {MaxReasonLength} ký tự.");
    }
}
