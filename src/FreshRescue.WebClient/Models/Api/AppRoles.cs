namespace FreshRescue.WebClient.Models.Api;

/// <summary>Chép từ <c>FreshRescue.Entities.Constants.AppRoles</c> — dùng trong <c>[Authorize(Roles = ...)]</c>.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string BrandManager = "BrandManager";
    public const string StoreStaff = "StoreStaff";
    public const string Customer = "Customer";
}
