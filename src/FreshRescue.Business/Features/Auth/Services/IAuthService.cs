using FreshRescue.Business.Features.Auth.Dtos;

namespace FreshRescue.Business.Features.Auth.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken ct);

    Task<AuthResponseDto> RegisterBrandAsync(RegisterBrandRequest request, CancellationToken ct);

    Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct);

    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct);

    Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct);

    Task<AuthMessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct);

    Task<AuthMessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);

    Task<UserInfoDto> GetCurrentUserProfileAsync(Guid userId, CancellationToken ct);
}
