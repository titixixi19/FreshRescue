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
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("page phải từ 1 trở lên.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PagedResponse<object>.MaxPageSize)
            .WithMessage($"pageSize phải từ 1 đến {PagedResponse<object>.MaxPageSize}.");
    }
}
