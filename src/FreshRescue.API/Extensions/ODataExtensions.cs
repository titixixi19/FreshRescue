namespace FreshRescue.API.Extensions;

/// <summary>
/// Chủ sở hữu: TV3 (T4-TV3-02). <c>/odata/Stores</c> và <c>/odata/Offers</c>, chỉ đọc,
/// <c>$top</c> tối đa 100 (03-API-CONTRACT.md §9). Content negotiation XML (T5-TV3-02) cũng
/// gắn vào <see cref="IMvcBuilder"/> ở đây.
/// </summary>
public static class ODataExtensions
{
    public static IMvcBuilder AddODataConfig(this IMvcBuilder builder)
    {
        // TODO(TV3, 2026-10-06): builder.AddOData(...) theo 03-API-CONTRACT.md §9.
        return builder;
    }
}
