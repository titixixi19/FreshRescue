namespace FreshRescue.WebClient.Models.Api;

// 03-API-CONTRACT.md §2.

public record LoginRequest(string Email, string Password);

public record RegisterCustomerRequest(string FullName, string Email, string Password, string? PhoneNumber);

public record RefreshTokenRequest(string RefreshToken);

public record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserInfo User { get; init; } = new();
}

public record UserInfo
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    /// <summary>Luôn là mảng — một người có thể mang nhiều vai trò (OQ-03).</summary>
    public IReadOnlyList<string> Roles { get; init; } = [];
    public Guid? BrandId { get; init; }
    public Guid? StoreId { get; init; }
}
