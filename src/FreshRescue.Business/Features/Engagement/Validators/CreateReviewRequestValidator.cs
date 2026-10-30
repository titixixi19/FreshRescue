using FluentValidation;
using FreshRescue.Business.Features.Engagement.Dtos;

namespace FreshRescue.Business.Features.Engagement.Validators;

public sealed class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween((byte)1, (byte)5)
            .WithMessage("Số sao phải từ 1 đến 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .WithMessage("Bình luận tối đa 1000 ký tự.");

        RuleFor(x => x.PhotoUrl)
            .MaximumLength(500)
            .WithMessage("Đường dẫn ảnh tối đa 500 ký tự.");
    }
}
