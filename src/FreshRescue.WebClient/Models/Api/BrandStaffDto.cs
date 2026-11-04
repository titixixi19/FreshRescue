using System.Text.Json.Serialization;

namespace FreshRescue.WebClient.Models.Api;

public class BrandStaffDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("storeId")]
    public Guid StoreId { get; set; }

    [JsonPropertyName("storeName")]
    public string StoreName { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

public class CreateBrandStaffRequest
{
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("storeId")]
    public Guid StoreId { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }
}
