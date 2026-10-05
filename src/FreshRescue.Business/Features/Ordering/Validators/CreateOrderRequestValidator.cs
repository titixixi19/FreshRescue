using FluentValidation;
using FreshRescue.Business.Features.Ordering.Dtos;

namespace FreshRescue.Business.Features.Ordering.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    // Khoá bi quan đưa mỗi offerId thành một tham số SQL, SQL Server nhận tối đa 2100 tham số.
    // Một đợt bán thực tế chỉ có vài chục offer nên 20 dòng là đủ (Tùng duyệt 05/10).
    public const int MaxItems = 20;

    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.SaleBatchId).NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Đơn hàng phải có ít nhất một món.")
            .Must(items => items is null || items.Count <= MaxItems)
            .WithMessage($"Mỗi đơn tối đa {MaxItems} mặt hàng.")
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
