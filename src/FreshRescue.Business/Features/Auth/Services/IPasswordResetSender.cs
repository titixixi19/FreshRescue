using Microsoft.Extensions.Logging;

namespace FreshRescue.Business.Features.Auth.Services;

/// <summary>
/// Gửi token đặt lại mật khẩu tới người dùng (<c>POST /auth/forgot-password</c>, 03 §2).
/// Tầng Business không biết gửi bằng gì — API cài đặt bằng email.
/// </summary>
public interface IPasswordResetSender
{
    Task SendAsync(string email, string fullName, string resetToken, CancellationToken cancellationToken);
}

/// <summary>
/// Bản mặc định khi tiến trình không đăng ký cách gửi (seeder, test): chỉ ghi log, không lộ token.
/// </summary>
internal sealed class LogOnlyPasswordResetSender : IPasswordResetSender
{
    private readonly ILogger<LogOnlyPasswordResetSender> _logger;

    public LogOnlyPasswordResetSender(ILogger<LogOnlyPasswordResetSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string email, string fullName, string resetToken, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Chưa cấu hình cách gửi email đặt lại mật khẩu; bỏ qua yêu cầu của {Email}.", email);
        return Task.CompletedTask;
    }
}
