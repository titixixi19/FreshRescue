using FluentValidation;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog.Validators;

public sealed class UpsertOfferTemplateRequestValidator : AbstractValidator<UpsertOfferTemplateRequest>
{
    public UpsertOfferTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên mẫu hàng không được để trống.")
            .MaximumLength(200).WithMessage("Tên mẫu hàng tối đa 200 ký tự.");
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả tối đa 1000 ký tự.");
        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Đường dẫn ảnh tối đa 500 ký tự.");
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Phải chọn danh mục.");
        RuleFor(x => x.OfferType)
            .IsInEnum().WithMessage("Loại mẫu hàng không hợp lệ.");
        RuleFor(x => x.OriginalPrice)
            .GreaterThan(0).WithMessage("Giá gốc phải lớn hơn 0.");
        RuleFor(x => x.SuggestedPrice)
            .GreaterThan(0).WithMessage("Giá gợi ý phải lớn hơn 0.");
        // Các lỗi thuộc BR-07 để Service trả mã PRICE_OUT_OF_BAND thống nhất.
    }
}
