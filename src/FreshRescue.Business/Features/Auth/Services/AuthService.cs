using System.Text.RegularExpressions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Auth.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreshRescue.Business.Features.Auth.Services;

public partial class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _clock;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IUnitOfWork uow,
        ITokenService tokenService,
        TimeProvider clock,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _uow = uow;
        _tokenService = tokenService;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken ct)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Email đã được sử dụng trên hệ thống.");
        }

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            UserName = request.Email.Trim().ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = true,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var firstError = createResult.Errors.FirstOrDefault()?.Description ?? "Đăng ký không thành công.";
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, firstError);
        }

        await EnsureRoleExistsAsync(AppRoles.Customer);
        await _userManager.AddToRoleAsync(user, AppRoles.Customer);

        var roles = new[] { AppRoles.Customer };
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user, roles, null, null);
        var refreshTokenString = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = _tokenService.GetRefreshTokenExpiresAt(),
        };

        await _uow.RefreshTokens.AddAsync(refreshTokenEntity, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Khách hàng mới đã đăng ký thành công: {UserId}, Email={Email}", user.Id, user.Email);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = expiresAt,
            User = new UserInfoDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                BrandId = null,
                StoreId = null,
            },
        };
    }

    public async Task<AuthResponseDto> RegisterBrandAsync(RegisterBrandRequest request, CancellationToken ct)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Email đã được sử dụng trên hệ thống.");
        }

        var slug = string.IsNullOrWhiteSpace(request.BrandSlug)
            ? GenerateSlug(request.BrandName)
            : request.BrandSlug.Trim().ToLowerInvariant();

        var slugExists = await _uow.Brands.Query().AnyAsync(b => b.Slug == slug, ct);
        if (slugExists)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Slug hoặc tên thương hiệu đã tồn tại trên hệ thống.");
        }

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            UserName = request.Email.Trim().ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = true,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var firstError = createResult.Errors.FirstOrDefault()?.Description ?? "Đăng ký không thành công.";
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, firstError);
        }

        await EnsureRoleExistsAsync(AppRoles.BrandManager);
        await _userManager.AddToRoleAsync(user, AppRoles.BrandManager);

        var brand = new Brand
        {
            Name = request.BrandName.Trim(),
            Slug = slug,
            Description = request.BrandDescription?.Trim(),
            LogoUrl = request.BrandLogoUrl?.Trim(),
            OwnerUserId = user.Id,
            Status = BrandStatus.PendingApproval,
        };

        await _uow.Brands.AddAsync(brand, ct);

        var roles = new[] { AppRoles.BrandManager };
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user, roles, brand.Id, null);
        var refreshTokenString = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = _tokenService.GetRefreshTokenExpiresAt(),
        };

        await _uow.RefreshTokens.AddAsync(refreshTokenEntity, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Đăng ký thương hiệu mới: BrandId={BrandId}, Name={BrandName}, Owner={UserId}", brand.Id, brand.Name, user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = expiresAt,
            User = new UserInfoDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                BrandId = brand.Id,
                StoreId = null,
            },
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            throw AppException.Unauthorized(ErrorCodes.Unauthenticated, "Email hoặc mật khẩu không chính xác.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            throw AppException.Unauthorized(ErrorCodes.Unauthenticated, "Email hoặc mật khẩu không chính xác.");
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var (brandId, storeId) = await GetUserScopesAsync(user.Id, roles, ct);

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user, roles, brandId, storeId);
        var refreshTokenString = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = _tokenService.GetRefreshTokenExpiresAt(),
        };

        await _uow.RefreshTokens.AddAsync(refreshTokenEntity, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Người dùng {UserId} đăng nhập thành công. Roles={Roles}", user.Id, string.Join(',', roles));

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = expiresAt,
            User = new UserInfoDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                BrandId = brandId,
                StoreId = storeId,
            },
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var storedToken = await _uow.RefreshTokens.Query()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct) ?? throw AppException.Unauthorized(ErrorCodes.Unauthenticated, "Refresh token không hợp lệ.");

        // Phát hiện tái sử dụng token đã thu hồi (OQ-03, 02 §3.1) -> Thu hồi toàn bộ chuỗi token của user
        if (storedToken.RevokedAt != null)
        {
            _logger.LogWarning("Phát hiện token đã thu hồi bị sử dụng lại! UserId={UserId}", storedToken.UserId);

            var activeTokens = await _uow.RefreshTokens.Query()
                .Where(r => r.UserId == storedToken.UserId && r.RevokedAt == null)
                .ToListAsync(ct);

            var revokeTime = _clock.GetUtcNow().UtcDateTime;
            foreach (var token in activeTokens)
            {
                token.RevokedAt = revokeTime;
            }

            await _uow.SaveChangesAsync(ct);
            throw AppException.Unauthorized(ErrorCodes.Unauthenticated, "Phát hiện token bị tái sử dụng trái phép. Toàn bộ phiên đăng nhập đã bị thu hồi.");
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        if (storedToken.ExpiresAt <= now)
        {
            storedToken.RevokedAt = now;
            await _uow.SaveChangesAsync(ct);
            throw AppException.Unauthorized(ErrorCodes.TokenExpired, "Refresh token đã hết hạn. Vui lòng đăng nhập lại.");
        }

        var user = storedToken.User;
        if (!user.IsActive)
        {
            throw AppException.Unauthorized(ErrorCodes.Unauthenticated, "Tài khoản người dùng đã bị khoá.");
        }

        // Xoay vòng Refresh Token (Token Rotation)
        var newRefreshTokenString = _tokenService.GenerateRefreshToken();
        storedToken.RevokedAt = now;
        storedToken.ReplacedByToken = newRefreshTokenString;

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenString,
            ExpiresAt = _tokenService.GetRefreshTokenExpiresAt(),
        };

        await _uow.RefreshTokens.AddAsync(newRefreshTokenEntity, ct);

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var (brandId, storeId) = await GetUserScopesAsync(user.Id, roles, ct);
        var (newAccessToken, expiresAt) = _tokenService.GenerateAccessToken(user, roles, brandId, storeId);

        await _uow.SaveChangesAsync(ct);

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenString,
            ExpiresAt = expiresAt,
            User = new UserInfoDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                BrandId = brandId,
                StoreId = storeId,
            },
        };
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var storedToken = await _uow.RefreshTokens.Query()
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);

        if (storedToken != null && storedToken.RevokedAt == null)
        {
            storedToken.RevokedAt = _clock.GetUtcNow().UtcDateTime;
            await _uow.SaveChangesAsync(ct);
        }
    }

    public async Task<AuthMessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null && user.IsActive)
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            _logger.LogInformation("Password reset token generated for UserId={UserId}", user.Id);
        }

        return new AuthMessageResponse("Nếu email tồn tại trên hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi.");
    }

    public async Task<AuthMessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Email không hợp lệ hoặc token không chính xác.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault()?.Description ?? "Đặt lại mật khẩu không thành công.";
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, error);
        }

        return new AuthMessageResponse("Đặt lại mật khẩu thành công.");
    }

    public async Task<UserInfoDto> GetCurrentUserProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy người dùng.");

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var (brandId, storeId) = await GetUserScopesAsync(user.Id, roles, ct);

        return new UserInfoDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Roles = roles,
            BrandId = brandId,
            StoreId = storeId,
        };
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new AppRole(roleName));
        }
    }

    private async Task<(Guid? BrandId, Guid? StoreId)> GetUserScopesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken ct)
    {
        Guid? brandId = null;
        Guid? storeId = null;

        if (roles.Contains(AppRoles.BrandManager))
        {
            var brand = await _uow.Brands.Query().FirstOrDefaultAsync(b => b.OwnerUserId == userId, ct);
            brandId = brand?.Id;
        }

        if (roles.Contains(AppRoles.StoreStaff))
        {
            var staff = await _uow.StoreStaff.Query().FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive, ct);
            storeId = staff?.StoreId;
        }

        return (brandId, storeId);
    }

    private static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Guid.NewGuid().ToString("n")[..8];
        }

        var slug = text.Trim().ToLowerInvariant();
        slug = VietnameseSignsRegex().Replace(slug, "");
        slug = NonAlphaNumericRegex().Replace(slug, "-");
        slug = MultipleHyphensRegex().Replace(slug, "-").Trim('-');

        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("n")[..8] : slug;
    }

    [GeneratedRegex("[^a-z0-9\\s-]")]
    private static partial Regex VietnameseSignsRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("-+")]
    private static partial Regex MultipleHyphensRegex();
}
