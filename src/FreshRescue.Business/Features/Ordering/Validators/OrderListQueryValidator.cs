using FluentValidation;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering.Dtos;

namespace FreshRescue.Business.Features.Ordering.Validators;

public class OrderListQueryValidator : AbstractValidator<OrderListQuery>
{
    public OrderListQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("status không hợp lệ.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("page phải từ 1 trở lên.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PagedResponse<object>.MaxPageSize)
            .WithMessage($"pageSize phải từ 1 đến {PagedResponse<object>.MaxPageSize}.");
    }
}
