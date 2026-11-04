using FluentValidation;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Admin.Dtos;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Admin.Validators;

public class GetAdminBrandsRequestValidator
    : AbstractValidator<GetAdminBrandsRequest>
{
    public GetAdminBrandsRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(
                1,
                PagedResponse<AdminBrandDto>.MaxPageSize);

        RuleFor(x => x.Status)
            .Must(status =>
                !status.HasValue ||
                Enum.IsDefined(typeof(BrandStatus), status.Value))
            .WithMessage("Invalid brand status.");
    }
}
