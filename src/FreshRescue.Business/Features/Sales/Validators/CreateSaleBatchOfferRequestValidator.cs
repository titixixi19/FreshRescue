using FluentValidation;
using FreshRescue.Business.Features.Sales.Dtos;

namespace FreshRescue.Business.Features.Sales.Validators;

public class CreateSaleBatchOfferRequestValidator : AbstractValidator<CreateSaleBatchOfferRequest>
{
    public const int MaxQuantity = 10_000;
    public const int MaxPerCustomerLimit = 10;

    public CreateSaleBatchOfferRequestValidator()
    {
        RuleFor(x => x.OfferTemplateId).NotEmpty();
        RuleFor(x => x.ActualPrice).GreaterThan(0);
        RuleFor(x => x.TotalQuantity).InclusiveBetween(1, MaxQuantity);
        RuleFor(x => x.MaxPerCustomer)
            .InclusiveBetween(1, MaxPerCustomerLimit)
            .When(x => x.MaxPerCustomer.HasValue);
    }
}
