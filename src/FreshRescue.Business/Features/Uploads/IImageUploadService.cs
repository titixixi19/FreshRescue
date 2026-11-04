using FreshRescue.Business.Features.Uploads.Dtos;

namespace FreshRescue.Business.Features.Uploads;

public interface IImageUploadService
{
    /// <summary>Lưu ảnh vào <c>uploads/{folder}/</c> với tên ngẫu nhiên, trả đường dẫn tương đối.</summary>
    /// <exception cref="Common.AppException">VALIDATION_FAILED khi thư mục, đuôi file, dung lượng hoặc nội dung không hợp lệ.</exception>
    Task<UploadedImageDto> SaveAsync(
        Stream content,
        string fileName,
        string folder,
        CancellationToken cancellationToken);
}
