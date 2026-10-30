namespace FreshRescue.WebClient.Models.Api;

/// <summary>— Application roles used by WebClient authorization.— These values must match the roles returned by the API.
/// </c>.</summary>
public static class AppRoles
{
    public const string Admin = "SystemAdmin";
    public const string BrandManager = "BrandManager";
    public const string StoreStaff = "StoreStaff";
    public const string Customer = "Customer";
}
