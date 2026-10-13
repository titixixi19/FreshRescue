namespace FreshRescue.Business.Features.Auth.Dtos;

/// <summary>
/// Yêu cầu đặt lại mật khẩu bằng token (03-API-CONTRACT.md §2).
/// </summary>
public record ResetPasswordRequest
{
    public string Email { get; init; } = string.Empty;

    public string Token { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
