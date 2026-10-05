using FluentValidation;
using FreshRescue.Business.Features.Ordering.Dtos;

namespace FreshRescue.Business.Features.Ordering.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.SaleBatchId).NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Đơn hàng phải có ít nhất một món.")
            // Gộp hai dòng cùng offer thì giới hạn mua (BR-06) và thông báo lỗi đều khó hiểu —
            // client gửi mỗi offer một dòng.
            .Must(items => items is null || items.Select(i => i.OfferId).Distinct().Count() == items.Count)
            .WithMessage("Mỗi mặt hàng chỉ được xuất hiện một lần trong đơn.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.OfferId).NotEmpty();
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 10);
        });
    }
}
