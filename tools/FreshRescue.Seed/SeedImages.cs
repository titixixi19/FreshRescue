namespace FreshRescue.Seed;

/// <summary>
/// Khiếu nại bắt buộc có ảnh (03 §5). Seeder ghi một ảnh mẫu vào <c>wwwroot/uploads</c> của API
/// để trang duyệt khiếu nại không hiện ảnh vỡ.
/// </summary>
public static class SeedImages
{
    public const string DisputePhotoUrl = "/uploads/disputes/seed-demo.png";

    // PNG 8×8 màu cam, đủ để trình duyệt hiển thị.
    private static readonly byte[] _png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAIAAABLbSncAAAAEUlEQVR4nGP43yOFFTEMLQkAWW9pQWYKiioAAAAASUVORK5CYII=");

    /// <summary>Tìm <c>src/FreshRescue.API/wwwroot</c> từ thư mục đang chạy trở lên; không thấy thì bỏ qua.</summary>
    public static string? EnsurePlaceholders()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            var wwwroot = Path.Combine(dir.FullName, "src", "FreshRescue.API", "wwwroot");
            if (!Directory.Exists(wwwroot))
            {
                continue;
            }

            var file = Path.Combine(wwwroot, DisputePhotoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            if (!File.Exists(file))
            {
                File.WriteAllBytes(file, _png);
            }

            return file;
        }

        return null;
    }
}
