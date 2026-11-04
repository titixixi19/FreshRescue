using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FreshRescue.Business.Common;

namespace FreshRescue.API.Extensions;

/// <summary>
/// Cài đặt ICurrentUser đọc từ ClaimsPrincipal trong HttpContext (T3-HIEU-03).
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (User == null)
            {
                return [];
            }

            var roles = User.FindAll(ClaimTypes.Role)
                .Concat(User.FindAll("role"))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return roles;
        }
    }

    public Guid? BrandId
    {
        get
        {
            var brandIdStr = User?.FindFirstValue("brand_id");
            return Guid.TryParse(brandIdStr, out var id) ? id : null;
        }
    }

    public Guid? StoreId
    {
        get
        {
            var storeIdStr = User?.FindFirstValue("store_id");
            return Guid.TryParse(storeIdStr, out var id) ? id : null;
        }
    }

    public bool IsInRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
