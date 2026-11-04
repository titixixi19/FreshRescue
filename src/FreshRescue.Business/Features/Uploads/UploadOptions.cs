namespace FreshRescue.Business.Features.Uploads;

/// <summary>Cấu hình lưu ảnh upload (OQ-09). <see cref="RootPath"/> do tầng API gán = <c>wwwroot/uploads</c>.</summary>
public class UploadOptions
{
    public const string SectionName = "Uploads";

    public string RootPath { get; set; } = string.Empty;
}
