using FluentValidation;
using FreshRescue.Business.Features.Sales.Dtos;

namespace FreshRescue.Business.Features.Sales.Validators;

public class UpdateSaleBatchRequestValidator : AbstractValidator<UpdateSaleBatchRequest>
{
    public UpdateSaleBatchRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(CreateSaleBatchRequestValidator.MaxNameLength);
        RuleFor(x => x.SaleStartTime).NotEmpty();
        RuleFor(x => x.SaleEndTime).NotEmpty();
        RuleFor(x => x.PickupStartTime).NotEmpty();
        RuleFor(x => x.PickupEndTime).NotEmpty();
    }
}
