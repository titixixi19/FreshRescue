using FluentValidation;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Catalog.Validators;

public class UpdateStoreRequestValidator
    : AbstractValidator<UpdateStoreRequest>
{
    public UpdateStoreRequestValidator()
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

        RuleFor(x => x.Status)
            .NotNull()
            .Must(status =>
                status.HasValue &&
                Enum.IsDefined(typeof(StoreStatus), status.Value))
            .WithMessage("Invalid store status.");
    }
}
