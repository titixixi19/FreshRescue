namespace FreshRescue.Business.Features.Auth.Dtos;

/// <summary>
/// Thông tin người dùng hiện tại (03-API-CONTRACT.md §2).
/// </summary>
public record UserInfoDto
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public Guid? BrandId { get; init; }

    public Guid? StoreId { get; init; }
}
