namespace FreshRescue.Business.Features.Auth.Dtos;

/// <summary>
/// Yêu cầu gửi email đặt lại mật khẩu (03-API-CONTRACT.md §2).
/// </summary>
public record ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
}
