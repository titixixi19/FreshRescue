using FluentValidation;
using FreshRescue.Business.Features.Engagement.Dtos;

namespace FreshRescue.Business.Features.Engagement.Validators;

public sealed class CreateDisputeRequestValidator : AbstractValidator<CreateDisputeRequest>
{
    public CreateDisputeRequestValidator()
    {
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.PhotoUrl).NotEmpty().MaximumLength(500);
    }
}

public sealed class RejectDisputeRequestValidator : AbstractValidator<RejectDisputeRequest>
{
    public RejectDisputeRequestValidator()
    {
        RuleFor(x => x.AdminNote).NotEmpty().MaximumLength(1000);
    }
}

public sealed class MerchantNoteRequestValidator : AbstractValidator<MerchantNoteRequest>
{
    public MerchantNoteRequestValidator()
    {
        RuleFor(x => x.MerchantNote).NotEmpty().MaximumLength(1000);
    }
}
