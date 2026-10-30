using FreshRescue.Business.Features.Catalog.Dtos;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;

namespace FreshRescue.API.Extensions;

/// <summary>
/// Điểm truy vấn OData công khai, tách biệt với API xử lý nghiệp vụ.
/// Content negotiation JSON + XML (T5-TV3-02) cũng gắn vào <see cref="IMvcBuilder"/> ở đây.
/// </summary>
public static class ODataExtensions
{
    public static IMvcBuilder AddODataConfig(this IMvcBuilder builder)
    {
        var edmBuilder = new ODataConventionModelBuilder();
        edmBuilder.EnableLowerCamelCase();
        edmBuilder.EntitySet<StoreODataDto>("Stores");
        edmBuilder.EntitySet<OfferODataDto>("Offers");

        return builder.AddOData(options => options
                .Select()
                .Filter()
                .OrderBy()
                .Expand()
                .Count()
                .SetMaxTop(100)
                .AddRouteComponents("odata", edmBuilder.GetEdmModel()))
            .AddXmlSerializerFormatters();
    }
}