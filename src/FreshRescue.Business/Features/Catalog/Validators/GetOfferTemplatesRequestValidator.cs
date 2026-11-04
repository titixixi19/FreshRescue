using FluentValidation;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog.Validators;

public sealed class GetOfferTemplatesRequestValidator : AbstractValidator<GetOfferTemplatesRequest>
{
    public GetOfferTemplatesRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.OfferType)
            .Must(x => x is null || Enum.IsDefined(x.Value))
            .WithMessage("Loại mẫu hàng không hợp lệ.");
    }
}
