using System.Net;
using System.Net.Mail;
using FreshRescue.Business.Features.Auth.Services;
using Microsoft.Extensions.Options;

namespace FreshRescue.API.Extensions;

/// <summary>Cấu hình mục <c>PasswordReset</c> trong appsettings.</summary>
public class PasswordResetEmailOptions
{
    public const string SectionName = "PasswordReset";

    /// <summary>Trang đặt lại mật khẩu của Web Client; link gửi đi = ResetUrl?email=..&amp;token=..</summary>
    public string ResetUrl { get; set; } = "https://localhost:7002/Account/ResetPassword";

    public string From { get; set; } = "no-reply@freshrescue.test";

    /// <summary>Bỏ trống = không gửi SMTP thật, ghi file .eml vào <see cref="PickupDirectory"/>.</summary>
    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public string? SmtpUserName { get; set; }

    public string? SmtpPassword { get; set; }

    public bool EnableSsl { get; set; } = true;

    /// <summary>Tương đối với thư mục gốc của API.</summary>
    public string PickupDirectory { get; set; } = "App_Data/emails";
}

/// <summary>
/// Gửi email đặt lại mật khẩu bằng <see cref="SmtpClient"/> có sẵn trong .NET (không thêm package).
/// Máy demo không có SMTP thì email được ghi thành file .eml, và ở Development link được in ra log
/// để người demo mở thẳng.
/// </summary>
public sealed class EmailPasswordResetSender : IPasswordResetSender
{
    private readonly PasswordResetEmailOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EmailPasswordResetSender> _logger;

    public EmailPasswordResetSender(
        IOptions<PasswordResetEmailOptions> options,
        IWebHostEnvironment env,
        ILogger<EmailPasswordResetSender> logger)
    {
        _options = options.Value;
        _env = env;
        _logger = logger;
    }

    public async Task SendAsync(string email, string fullName, string resetToken, CancellationToken cancellationToken)
    {
        var link = $"{_options.ResetUrl}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";

        using var message = new MailMessage(_options.From, email)
        {
            Subject = "FreshRescue — Đặt lại mật khẩu",
            IsBodyHtml = true,
            Body = $"""
                <p>Xin chào {WebUtility.HtmlEncode(fullName)},</p>
                <p>Bạn vừa yêu cầu đặt lại mật khẩu FreshRescue. Bấm vào liên kết dưới đây để đặt mật khẩu mới:</p>
                <p><a href="{WebUtility.HtmlEncode(link)}">Đặt lại mật khẩu</a></p>
                <p>Nếu không phải bạn yêu cầu, hãy bỏ qua email này.</p>
                """,
        };

        using var client = new SmtpClient();
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            var dir = Path.Combine(_env.ContentRootPath, _options.PickupDirectory);
            Directory.CreateDirectory(dir);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = dir;
        }
        else
        {
            client.Host = _options.SmtpHost;
            client.Port = _options.SmtpPort;
            client.EnableSsl = _options.EnableSsl;
            if (!string.IsNullOrWhiteSpace(_options.SmtpUserName))
            {
                client.Credentials = new NetworkCredential(_options.SmtpUserName, _options.SmtpPassword);
            }
        }

        await client.SendMailAsync(message, cancellationToken);

        if (_env.IsDevelopment())
        {
            // Chỉ in link ở môi trường phát triển — đây là cách duy nhất để demo khi không có hộp thư thật.
            _logger.LogInformation("Link đặt lại mật khẩu cho {Email}: {Link}", email, link);
        }
    }
}

public static class PasswordResetExtensions
{
    public static IServiceCollection AddPasswordResetEmail(this IServiceCollection services)
    {
        services.AddOptions<PasswordResetEmailOptions>().BindConfiguration(PasswordResetEmailOptions.SectionName);
        services.AddScoped<IPasswordResetSender, EmailPasswordResetSender>();
        return services;
    }
}
