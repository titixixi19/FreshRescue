using System.Text.Json.Serialization;

namespace FreshRescue.WebClient.Models.Api;

public class BrandStatsDto
{
    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalRescuedPortions")]
    public int TotalRescuedPortions { get; set; }

    [JsonPropertyName("byStore")]
    public IReadOnlyList<BrandStoreStatDto> ByStore { get; set; } = Array.Empty<BrandStoreStatDto>();
}

public class BrandStoreStatDto
{
    [JsonPropertyName("storeId")]
    public Guid StoreId { get; set; }

    [JsonPropertyName("storeName")]
    public string StoreName { get; set; } = string.Empty;

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("rescuedPortions")]
    public int RescuedPortions { get; set; }

    [JsonPropertyName("noShowRate")]
    public decimal NoShowRate { get; set; }

    [JsonPropertyName("ratingAverage")]
    public decimal RatingAverage { get; set; }
}
