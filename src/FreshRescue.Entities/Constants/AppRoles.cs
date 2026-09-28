namespace FreshRescue.Entities.Constants;

/// <summary>
/// Bốn role cố định của hệ thống (02-DOMAIN-MODEL.md §3.1). Dùng hằng số này,
/// không viết chuỗi "Customer" rải rác trong code (06-CODING-STANDARDS.md §7).
/// </summary>
public static class AppRoles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string BrandManager = "BrandManager";
    public const string StoreStaff = "StoreStaff";
    public const string Customer = "Customer";
}
