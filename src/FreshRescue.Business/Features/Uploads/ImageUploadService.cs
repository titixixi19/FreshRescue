using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Uploads.Dtos;
using Microsoft.Extensions.Options;

namespace FreshRescue.Business.Features.Uploads;

/// <summary>
/// Endpoint upload ảnh dùng chung cho mặt hàng / đánh giá / khiếu nại / avatar / logo (OQ-09).
/// Không tin đuôi file hay Content-Type do client gửi: đối chiếu cả chữ ký byte đầu file.
/// </summary>
public class ImageUploadService : IImageUploadService
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedFolders = new HashSet<string>(StringComparer.Ordinal)
    {
        "offers", "reviews", "disputes", "avatars", "logos",
    };

    private static readonly IReadOnlySet<string> _allowedExtensions = new HashSet<string>(StringComparer.Ordinal)
    {
        ".jpg", ".jpeg", ".png", ".webp",
    };

    private readonly string _rootPath;

    public ImageUploadService(IOptions<UploadOptions> options)
    {
        _rootPath = options.Value.RootPath;
    }

    public async Task<UploadedImageDto> SaveAsync(
        Stream content,
        string fileName,
        string folder,
        CancellationToken cancellationToken)
    {
        if (!AllowedFolders.Contains(folder))
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                $"Thư mục '{folder}' không hợp lệ. Chọn một trong: {string.Join(", ", AllowedFolders.Order())}.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Chỉ nhận ảnh .jpg, .jpeg, .png hoặc .webp.");
        }

        // Giới hạn thô đã chặn ở RequestSizeLimit của controller; ở đây kiểm lại chính xác 5 MB.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, 81920, cancellationToken);
        if (buffer.Length == 0)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "File rỗng.");
        }

        if (buffer.Length > MaxSizeBytes)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Ảnh vượt quá 5 MB.");
        }

        var bytes = buffer.ToArray();
        if (!MatchesSignature(bytes, extension))
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Nội dung file không phải ảnh hợp lệ.");
        }

        var directory = Path.Combine(_rootPath, folder);
        Directory.CreateDirectory(directory);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(
            Path.Combine(directory, storedName),
            bytes,
            cancellationToken);

        return new UploadedImageDto($"/uploads/{folder}/{storedName}", buffer.Length);
    }

    private static bool MatchesSignature(byte[] bytes, string extension)
    {
        ReadOnlySpan<byte> b = bytes;
        return extension switch
        {
            ".jpg" or ".jpeg" => b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
            ".png" => b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            // RIFF....WEBP
            ".webp" => b.Length >= 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8),
            _ => false,
        };
    }
}
