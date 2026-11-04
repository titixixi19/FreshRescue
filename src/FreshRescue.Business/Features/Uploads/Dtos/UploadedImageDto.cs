namespace FreshRescue.Business.Features.Uploads.Dtos;

/// <summary>Kết quả upload. <see cref="Url"/> là đường dẫn tương đối (OQ-09).</summary>
public record UploadedImageDto(string Url, long SizeBytes);
