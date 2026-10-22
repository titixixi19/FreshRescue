using FluentValidation;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog.Validators;

public class CreateStoreRequestValidator
    : AbstractValidator<CreateStoreRequest>
{
    public CreateStoreRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(150);

        RuleFor(x => x.AddressLine)
            .NotEmpty().MaximumLength(300);

        RuleFor(x => x.Ward)
            .NotEmpty().MaximumLength(100);

        RuleFor(x => x.District)
            .NotEmpty().MaximumLength(100);

        RuleFor(x => x.City)
            .NotEmpty().MaximumLength(100);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m);

        RuleFor(x => x.Phone)
            .NotEmpty().MaximumLength(20);

        RuleFor(x => x.OpenTime).NotNull();
        RuleFor(x => x.CloseTime).NotNull();
    }
}
