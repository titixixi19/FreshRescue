namespace FreshRescue.Business.Common;

/// <summary>
/// Người dùng của request hiện tại, đọc từ claim JWT (03-API-CONTRACT.md §1.5).
/// Cài đặt nằm ở tầng API, do Hiếu viết cùng phần xác thực (T3-HIEU-03).
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    /// <summary>Luôn là mảng — một user có thể mang nhiều role (OQ-03).</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>Chỉ có khi mang role BrandManager.</summary>
    Guid? BrandId { get; }

    /// <summary>Chỉ có khi mang role StoreStaff. Bỏ qua khi người dùng đang mua hàng.</summary>
    Guid? StoreId { get; }

    bool IsInRole(string role);
}
