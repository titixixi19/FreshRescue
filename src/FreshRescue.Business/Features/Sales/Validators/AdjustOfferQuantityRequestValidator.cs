using FluentValidation;
using FreshRescue.Business.Features.Sales.Dtos;

namespace FreshRescue.Business.Features.Sales.Validators;

public class AdjustOfferQuantityRequestValidator : AbstractValidator<AdjustOfferQuantityRequest>
{
    public AdjustOfferQuantityRequestValidator()
    {
        // 0 là cách ngừng bán giữa chừng (BR-24).
        RuleFor(x => x.TotalQuantity).InclusiveBetween(0, CreateSaleBatchOfferRequestValidator.MaxQuantity);
    }
}
