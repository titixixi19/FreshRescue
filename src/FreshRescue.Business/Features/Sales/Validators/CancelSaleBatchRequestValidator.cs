using FluentValidation;
using FreshRescue.Business.Features.Sales.Dtos;

namespace FreshRescue.Business.Features.Sales.Validators;

public class CancelSaleBatchRequestValidator : AbstractValidator<CancelSaleBatchRequest>
{
    public const int MaxReasonLength = 500;

    public CancelSaleBatchRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Phải nhập lý do huỷ đợt bán.")
            .MaximumLength(MaxReasonLength).WithMessage($"Lý do tối đa {MaxReasonLength} ký tự.");
    }
}
