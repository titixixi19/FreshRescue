namespace FreshRescue.Business.Features.Auth.Dtos;

/// <summary>
/// Kết quả trả về sau khi đăng nhập hoặc làm mới token thành công (03-API-CONTRACT.md §2).
/// </summary>
public record AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }

    public UserInfoDto User { get; init; } = null!;
}
