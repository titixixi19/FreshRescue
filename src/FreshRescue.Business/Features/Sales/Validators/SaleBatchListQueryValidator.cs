using FluentValidation;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Sales.Dtos;

namespace FreshRescue.Business.Features.Sales.Validators;

public class SaleBatchListQueryValidator : AbstractValidator<SaleBatchListQuery>
{
    public SaleBatchListQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("status không hợp lệ.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("page phải từ 1 trở lên.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PagedResponse<object>.MaxPageSize)
            .WithMessage($"pageSize phải từ 1 đến {PagedResponse<object>.MaxPageSize}.");
    }
}
