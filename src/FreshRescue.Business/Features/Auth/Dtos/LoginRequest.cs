namespace FreshRescue.Business.Features.Auth.Dtos;

/// <summary>
/// Yêu cầu đăng nhập bằng email và mật khẩu (03-API-CONTRACT.md §2).
/// </summary>
public record LoginRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
