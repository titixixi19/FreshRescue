using FluentValidation;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog.Validators;

public class StoreNearbyQueryValidator : AbstractValidator<StoreNearbyQuery>
{
    public StoreNearbyQueryValidator()
    {
        RuleFor(x => x.Lat).NotNull().WithMessage("lat là bắt buộc.");
        RuleFor(x => x.Lat).InclusiveBetween(-90m, 90m).When(x => x.Lat.HasValue)
            .WithMessage("lat phải từ -90 đến 90.");
        RuleFor(x => x.Lng).NotNull().WithMessage("lng là bắt buộc.");
        RuleFor(x => x.Lng).InclusiveBetween(-180m, 180m).When(x => x.Lng.HasValue)
            .WithMessage("lng phải từ -180 đến 180.");
        RuleFor(x => x.RadiusKm)
            .GreaterThan(0).LessThanOrEqualTo(StoreNearbyQuery.MaxRadiusKm)
            .WithMessage($"radiusKm phải lớn hơn 0 và tối đa {StoreNearbyQuery.MaxRadiusKm} km.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("page phải từ 1 trở lên.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PagedResponse<object>.MaxPageSize)
            .WithMessage($"pageSize phải từ 1 đến {PagedResponse<object>.MaxPageSize}.");
    }
}
