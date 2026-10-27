using FluentValidation;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog.Validators;

public class UpdateBrandProfileRequestValidator
    : AbstractValidator<UpdateBrandProfileRequest>
{
    public UpdateBrandProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(160)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage(
                "Slug must contain lowercase letters, numbers and hyphens only.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}
