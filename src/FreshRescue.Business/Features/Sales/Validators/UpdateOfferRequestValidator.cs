using FluentValidation;
using FreshRescue.Business.Features.Sales.Dtos;

namespace FreshRescue.Business.Features.Sales.Validators;

public class UpdateOfferRequestValidator : AbstractValidator<UpdateOfferRequest>
{
    public UpdateOfferRequestValidator()
    {
        RuleFor(x => x.ActualPrice).GreaterThan(0);
        RuleFor(x => x.MaxPerCustomer)
            .InclusiveBetween(1, CreateSaleBatchOfferRequestValidator.MaxPerCustomerLimit);
    }
}
