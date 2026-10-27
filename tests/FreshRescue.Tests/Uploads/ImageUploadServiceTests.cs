using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Uploads;
using Microsoft.Extensions.Options;

namespace FreshRescue.Tests.Uploads;

/// <summary>Upload ảnh dùng chung (OQ-09): thư mục, đuôi file, dung lượng, chữ ký byte.</summary>
public sealed class ImageUploadServiceTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
    private static readonly byte[] Jpg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0];
    private static readonly byte[] Webp = "RIFF\0\0\0\0WEBP"u8.ToArray();

    private readonly string _root = Path.Combine(Path.GetTempPath(), "fr-upload-" + Guid.NewGuid().ToString("N"));
    private readonly ImageUploadService _service;

    public ImageUploadServiceTests()
    {
        _service = new ImageUploadService(Options.Create(new UploadOptions { RootPath = _root }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Theory]
    [InlineData("a.png", "offers")]
    [InlineData("A.PNG", "reviews")]
    [InlineData("a.jpg", "disputes")]
    [InlineData("a.jpeg", "avatars")]
    [InlineData("a.webp", "logos")]
    public async Task Save_AnhHopLe_LuuVaTraDuongDanTuongDoiAsync(string fileName, string folder)
    {
        var content = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => Png,
            ".webp" => Webp,
            _ => Jpg,
        };

        var result = await _service.SaveAsync(new MemoryStream(content), fileName, folder, CancellationToken.None);

        result.Url.Should().StartWith($"/uploads/{folder}/").And.EndWith(Path.GetExtension(fileName).ToLowerInvariant());
        result.SizeBytes.Should().Be(content.Length);
        File.ReadAllBytes(Path.Combine(_root, folder, Path.GetFileName(result.Url))).Should().Equal(content);
    }

    [Fact]
    public async Task Save_HaiLanCungTen_RaHaiFileKhacNhauAsync()
    {
        var a = await _service.SaveAsync(new MemoryStream(Png), "x.png", "offers", CancellationToken.None);
        var b = await _service.SaveAsync(new MemoryStream(Png), "x.png", "offers", CancellationToken.None);

        a.Url.Should().NotBe(b.Url);
    }

    [Theory]
    [InlineData("../etc", "a.png")]
    [InlineData("secret", "a.png")]
    [InlineData("", "a.png")]
    [InlineData("offers", "a.gif")]
    [InlineData("offers", "a.exe")]
    [InlineData("offers", "noext")]
    public async Task Save_ThuMucHoacDuoiSai_NemValidationFailedAsync(string folder, string fileName)
    {
        var act = () => _service.SaveAsync(new MemoryStream(Png), fileName, folder, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ValidationFailed && e.StatusCode == 400);
        Directory.Exists(_root).Should().BeFalse();
    }

    [Fact]
    public async Task Save_DuoiPngNhungNoiDungKhongPhaiAnh_NemValidationFailedAsync()
    {
        var act = () => _service.SaveAsync(new MemoryStream("<script>alert(1)</script>"u8.ToArray()), "a.png", "offers", CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Save_DuoiJpgNhungNoiDungPng_NemValidationFailedAsync()
    {
        var act = () => _service.SaveAsync(new MemoryStream(Png), "a.jpg", "offers", CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Save_FileRong_NemValidationFailedAsync()
    {
        var act = () => _service.SaveAsync(new MemoryStream(), "a.png", "offers", CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Save_VuotNamMb_NemValidationFailedAsync()
    {
        var big = new byte[ImageUploadService.MaxSizeBytes + 1];
        Png.CopyTo(big, 0);

        var act = () => _service.SaveAsync(new MemoryStream(big), "a.png", "offers", CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Save_DungNamMb_ChapNhanAsync()
    {
        var exact = new byte[ImageUploadService.MaxSizeBytes];
        Png.CopyTo(exact, 0);

        var result = await _service.SaveAsync(new MemoryStream(exact), "a.png", "offers", CancellationToken.None);

        result.SizeBytes.Should().Be(ImageUploadService.MaxSizeBytes);
    }
}
