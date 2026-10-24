using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Services;

/// <summary>
/// Cho đúng một request đổi một refresh token; request khác cầm cùng token đó dùng lại kết quả.
/// </summary>
/// <remarks>
/// Trang và lệnh AJAX của cùng một người có thể cùng thấy access token sắp hết và cùng đổi một
/// refresh token. API coi lần đổi thứ hai là token bị đánh cắp và thu hồi MỌI phiên của người đó
/// (<c>AuthService.RefreshTokenAsync</c>) — người dùng bị đá ra giữa lúc đặt hàng.
/// </remarks>
public class TokenRefreshCoordinator
{
    private static readonly TimeSpan _reuseWindow = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, (AuthResponse Result, DateTime At)> _recent = new(StringComparer.Ordinal);
    private readonly TimeProvider _clock;

    public TokenRefreshCoordinator(TimeProvider clock)
    {
        _clock = clock;
    }

    public async Task<AuthResponse?> RefreshAsync(
        string refreshToken,
        Func<Task<AuthResponse?>> refresh,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            foreach (var expired in _recent.Where(e => now - e.Value.At > _reuseWindow).Select(e => e.Key).ToList())
            {
                _recent.Remove(expired);
            }

            if (_recent.TryGetValue(refreshToken, out var cached))
            {
                return cached.Result;
            }

            var result = await refresh();
            if (result is not null)
            {
                _recent[refreshToken] = (result, now);
            }

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }
}
