namespace FreshRescue.Business.Features.Auth.Dtos;

/// <summary>
/// Yêu cầu đăng ký tài khoản Customer (03-API-CONTRACT.md §2).
/// </summary>
public record RegisterCustomerRequest
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }
}
