using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Auth.Dtos;
using FreshRescue.Business.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

/// <summary>
/// Các endpoint xác thực và quản lý tài khoản người dùng (03-API-CONTRACT.md §2).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json", "application/xml")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService authService, ICurrentUser currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    /// <summary>Đăng ký tài khoản khách hàng mới.</summary>
    /// <response code="201">Đăng ký thành công, trả về token và thông tin người dùng</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc email đã tồn tại</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> RegisterAsync(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken ct)
    {
        var response = await _authService.RegisterCustomerAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Đăng ký thương hiệu mới (tạo BrandManager và hồ sơ Brand chờ duyệt).</summary>
    /// <response code="201">Đăng ký thành công, trả về token và thông tin người dùng</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc email/slug đã tồn tại</response>
    [HttpPost("register-brand")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> RegisterBrandAsync(
        [FromBody] RegisterBrandRequest request,
        CancellationToken ct)
    {
        var response = await _authService.RegisterBrandAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Đăng nhập bằng email và mật khẩu.</summary>
    /// <response code="200">Đăng nhập thành công, trả về Access Token và Refresh Token</response>
    /// <response code="401">Sai email hoặc mật khẩu</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Đổi Refresh Token lấy cặp token mới (xoay vòng token).</summary>
    /// <response code="200">Làm mới token thành công</response>
    /// <response code="401">Refresh token không hợp lệ, đã hết hạn hoặc bị tái sử dụng</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var response = await _authService.RefreshTokenAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Đăng xuất và thu hồi Refresh Token.</summary>
    /// <response code="200">Đăng xuất thành công</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(AuthMessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthMessageResponse>> LogoutAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        await _authService.LogoutAsync(request, ct);
        return Ok(new AuthMessageResponse("Đăng xuất thành công."));
    }

    /// <summary>Yêu cầu đặt lại mật khẩu qua email.</summary>
    /// <response code="200">Luôn trả về thông báo đã tiếp nhận yêu cầu</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthMessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthMessageResponse>> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken ct)
    {
        var response = await _authService.ForgotPasswordAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Đặt lại mật khẩu mới bằng token đã gửi qua email.</summary>
    /// <response code="200">Đặt lại mật khẩu thành công</response>
    /// <response code="400">Token không hợp lệ hoặc mật khẩu mới không đúng định dạng</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthMessageResponse>> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        CancellationToken ct)
    {
        var response = await _authService.ResetPasswordAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Lấy thông tin hồ sơ người dùng hiện tại từ token.</summary>
    /// <response code="200">Hồ sơ người dùng kèm danh sách role, brandId, storeId</response>
    /// <response code="401">Chưa xác thực</response>
    /// <response code="404">Không tìm thấy người dùng</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInfoDto>> GetMeAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw AppException.Unauthorized(ErrorCodes.Unauthenticated, "Không xác định được danh tính người dùng.");
        }

        var profile = await _authService.GetCurrentUserProfileAsync(userId, ct);
        return Ok(profile);
    }
}
