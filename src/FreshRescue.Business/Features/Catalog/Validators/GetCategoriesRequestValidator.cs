using FluentValidation;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog.Validators;

public class GetCategoriesRequestValidator
    : AbstractValidator<GetCategoriesRequest>
{
    public GetCategoriesRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PagedResponse<CategoryDto>.MaxPageSize);
    }
}
