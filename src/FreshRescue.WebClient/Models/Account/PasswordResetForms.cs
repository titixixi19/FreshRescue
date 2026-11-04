using FluentValidation;

namespace FreshRescue.WebClient.Models.Account;

public class ForgotPasswordForm
{
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordFormValidator : AbstractValidator<ForgotPasswordForm>
{
    public ForgotPasswordFormValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");
    }
}

/// <summary>Mở từ link trong email: <c>?email=..&amp;token=..</c> (03 §2 <c>reset-password</c>).</summary>
public class ResetPasswordForm
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordFormValidator : AbstractValidator<ResetPasswordForm>
{
    public ResetPasswordFormValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Thiếu email — hãy mở lại link trong email.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Thiếu mã đặt lại — hãy mở lại link trong email.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Mật khẩu nhập lại không khớp.");
    }
}

/// <summary>Đăng ký thương hiệu — tạo Brand Manager + hồ sơ ở trạng thái chờ duyệt (OQ-01, OQ-02).</summary>
public class RegisterBrandForm
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? BrandDescription { get; set; }
}

public class RegisterBrandFormValidator : AbstractValidator<RegisterBrandForm>
{
    public RegisterBrandFormValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Mật khẩu nhập lại không khớp.");
        RuleFor(x => x.BrandName)
            .NotEmpty().WithMessage("Tên thương hiệu không được để trống.")
            .MaximumLength(150).WithMessage("Tên thương hiệu tối đa 150 ký tự.");
        RuleFor(x => x.BrandDescription)
            .MaximumLength(1000).WithMessage("Giới thiệu tối đa 1000 ký tự.");
    }
}
