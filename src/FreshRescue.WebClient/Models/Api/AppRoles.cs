namespace FreshRescue.WebClient.Models.Api;

/// <summary>Tên role dùng cho phân quyền ở WebClient; phải khớp đúng chuỗi role API trả trong token.</summary>
public static class AppRoles
{
    public const string Admin = "SystemAdmin";
    public const string BrandManager = "BrandManager";
    public const string StoreStaff = "StoreStaff";
    public const string Customer = "Customer";
}
