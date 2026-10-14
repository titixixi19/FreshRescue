namespace FreshRescue.Business.Features.Auth.Dtos;

/// <summary>
/// Yêu cầu làm mới Access Token bằng Refresh Token (03-API-CONTRACT.md §2).
/// </summary>
public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
