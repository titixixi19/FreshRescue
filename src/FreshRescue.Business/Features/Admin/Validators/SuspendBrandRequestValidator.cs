using FluentValidation;
using FreshRescue.Business.Features.Admin.Dtos;

namespace FreshRescue.Business.Features.Admin.Validators;

public class SuspendBrandRequestValidator
    : AbstractValidator<SuspendBrandRequest>
{
    public SuspendBrandRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithMessage("Suspension reason is required.")
            .MaximumLength(500);
    }
}
