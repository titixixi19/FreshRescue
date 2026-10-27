using System.Text.Json;
using System.Text.Json.Serialization;

namespace FreshRescue.WebClient.Services;

/// <summary>Một cấu hình JSON cho cả HttpApiClient lẫn MockApiClient — để hai bản cài đặt đọc cùng một kiểu.</summary>
public static class ApiJson
{
    /// <summary>camelCase + enum dạng chuỗi, đúng 03-API-CONTRACT.md §1.</summary>
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
