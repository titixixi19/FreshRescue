using FluentValidation;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog.Validators;

public class GetBrandStaffRequestValidator : AbstractValidator<GetBrandStaffRequest>
{
    public GetBrandStaffRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Số lượng mỗi trang từ 1 đến 100.");
    }
}
