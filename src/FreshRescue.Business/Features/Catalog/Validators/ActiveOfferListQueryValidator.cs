using FluentValidation;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog.Validators;

public class ActiveOfferListQueryValidator : AbstractValidator<ActiveOfferListQuery>
{
    public ActiveOfferListQueryValidator()
    {
        RuleFor(x => x.OfferType).IsInEnum().WithMessage("offerType không hợp lệ.");
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue)
            .WithMessage("minPrice không được âm.");
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue)
            .WithMessage("maxPrice không được âm.");
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("maxPrice phải lớn hơn hoặc bằng minPrice.");
        // Thiếu một trong hai toạ độ thì không tính được khoảng cách — báo lỗi thay vì lặng lẽ bỏ qua.
        RuleFor(x => x.Lat).NotNull().When(x => x.Lng.HasValue).WithMessage("Cần cả lat và lng.");
        RuleFor(x => x.Lng).NotNull().When(x => x.Lat.HasValue).WithMessage("Cần cả lat và lng.");
        RuleFor(x => x.Lat).InclusiveBetween(-90m, 90m).When(x => x.Lat.HasValue)
            .WithMessage("lat phải từ -90 đến 90.");
        RuleFor(x => x.Lng).InclusiveBetween(-180m, 180m).When(x => x.Lng.HasValue)
            .WithMessage("lng phải từ -180 đến 180.");
        RuleFor(x => x.RadiusKm)
            .GreaterThan(0).LessThanOrEqualTo(StoreNearbyQuery.MaxRadiusKm)
            .When(x => x.Lat.HasValue && x.Lng.HasValue)
            .WithMessage($"radiusKm phải lớn hơn 0 và tối đa {StoreNearbyQuery.MaxRadiusKm} km.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("page phải từ 1 trở lên.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PagedResponse<object>.MaxPageSize)
            .WithMessage($"pageSize phải từ 1 đến {PagedResponse<object>.MaxPageSize}.");
    }
}
