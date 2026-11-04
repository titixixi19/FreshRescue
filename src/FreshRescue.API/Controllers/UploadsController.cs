using FreshRescue.Business.Features.Uploads;
using FreshRescue.Business.Features.Uploads.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/uploads")]
[Produces("application/json", "application/xml")]
[Authorize]
public class UploadsController : ControllerBase
{
    private readonly IImageUploadService _uploadService;

    public UploadsController(IImageUploadService uploadService)
    {
        _uploadService = uploadService;
    }

    /// <summary>Upload một ảnh dùng chung (OQ-09). Trả đường dẫn tương đối, client tự ghép với host.</summary>
    /// <param name="file">Ảnh .jpg .jpeg .png .webp, tối đa 5 MB</param>
    /// <param name="folder">offers · reviews · disputes · avatars · logos</param>
    /// <param name="cancellationToken">Huỷ yêu cầu</param>
    /// <response code="201">Đã lưu</response>
    /// <response code="400">Thiếu file, sai thư mục / định dạng / dung lượng (VALIDATION_FAILED)</response>
    /// <response code="401">Chưa đăng nhập</response>
    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageUploadService.MaxSizeBytes + 64 * 1024)]
    [ProducesResponseType(typeof(UploadedImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UploadedImageDto>> UploadImageAsync(
        IFormFile file,
        [FromForm] string folder,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await _uploadService.SaveAsync(stream, file.FileName, folder, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
