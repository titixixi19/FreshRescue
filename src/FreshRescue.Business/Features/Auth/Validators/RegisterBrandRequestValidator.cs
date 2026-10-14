using FluentValidation;
using FreshRescue.Business.Features.Auth.Dtos;

namespace FreshRescue.Business.Features.Auth.Validators;

public class RegisterBrandRequestValidator : AbstractValidator<RegisterBrandRequest>
{
    public RegisterBrandRequestValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.")
            .MaximumLength(256).WithMessage("Email không được vượt quá 256 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.");

        RuleFor(x => x.BrandName)
            .NotEmpty().WithMessage("Tên thương hiệu không được để trống.")
            .MaximumLength(150).WithMessage("Tên thương hiệu không được vượt quá 150 ký tự.");

        RuleFor(x => x.BrandSlug)
            .MaximumLength(160).WithMessage("Slug thương hiệu không được vượt quá 160 ký tự.")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug chỉ gồm chữ thường, số và dấu gạch ngang.")
            .When(x => !string.IsNullOrWhiteSpace(x.BrandSlug));

        RuleFor(x => x.BrandDescription)
            .MaximumLength(1000).WithMessage("Mô tả thương hiệu không được vượt quá 1000 ký tự.");

        RuleFor(x => x.BrandLogoUrl)
            .MaximumLength(500).WithMessage("Logo URL không được vượt quá 500 ký tự.");
    }
}
