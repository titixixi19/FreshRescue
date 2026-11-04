using FluentValidation;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog.Validators;

public class GetBrandStoresRequestValidator
    : AbstractValidator<GetBrandStoresRequest>
{
    public GetBrandStoresRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(
                1,
                PagedResponse<StoreDto>.MaxPageSize);
    }
}
