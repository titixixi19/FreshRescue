using FluentValidation;
using FreshRescue.Business.Features.Sales.Dtos;

namespace FreshRescue.Business.Features.Sales.Validators;

public class CreateSaleBatchRequestValidator : AbstractValidator<CreateSaleBatchRequest>
{
    public const int MaxNameLength = 150;
    public const int MaxOffers = 20;

    public CreateSaleBatchRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(MaxNameLength);

        // Thứ tự bốn mốc (BR-15) là luật nghiệp vụ nên service kiểm; ở đây chỉ chặn giá trị rỗng.
        RuleFor(x => x.SaleStartTime).NotEmpty();
        RuleFor(x => x.SaleEndTime).NotEmpty();
        RuleFor(x => x.PickupStartTime).NotEmpty();
        RuleFor(x => x.PickupEndTime).NotEmpty();

        RuleFor(x => x.Offers)
            .NotEmpty().WithMessage("Đợt bán phải có ít nhất một mặt hàng.")
            .Must(o => o is null || o.Count <= MaxOffers).WithMessage($"Mỗi đợt tối đa {MaxOffers} mặt hàng.")
            .Must(o => o is null || o.Select(i => i.OfferTemplateId).Distinct().Count() == o.Count)
            .WithMessage("Mỗi mẫu mặt hàng chỉ được xuất hiện một lần trong đợt.");

        RuleForEach(x => x.Offers).SetValidator(new CreateSaleBatchOfferRequestValidator());
    }
}
