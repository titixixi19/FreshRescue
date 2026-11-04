namespace FreshRescue.Business.Features.Auth.Dtos;

/// <summary>
/// Yêu cầu đăng ký thương hiệu mới (03-API-CONTRACT.md §2, OQ-01, OQ-02).
/// Tạo cùng lúc tài khoản Brand Manager và hồ sơ thương hiệu ở trạng thái PendingApproval.
/// </summary>
public record RegisterBrandRequest
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string BrandName { get; init; } = string.Empty;

    public string? BrandSlug { get; init; }

    public string? BrandDescription { get; init; }

    public string? BrandLogoUrl { get; init; }
}
