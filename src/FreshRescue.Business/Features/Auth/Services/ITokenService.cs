using FreshRescue.Entities.Identity;

namespace FreshRescue.Business.Features.Auth.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(
        AppUser user,
        IReadOnlyList<string> roles,
        Guid? brandId,
        Guid? storeId);

    string GenerateRefreshToken();

    DateTime GetRefreshTokenExpiresAt();
}
