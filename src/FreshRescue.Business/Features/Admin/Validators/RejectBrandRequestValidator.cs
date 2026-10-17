using FluentValidation;
using FreshRescue.Business.Features.Admin.Dtos;

namespace FreshRescue.Business.Features.Admin.Validators;

public class RejectBrandRequestValidator
    : AbstractValidator<RejectBrandRequest>
{
    public RejectBrandRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithMessage("Rejection reason is required.")
            .MaximumLength(500);
    }
}
