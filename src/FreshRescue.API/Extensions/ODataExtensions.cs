using FreshRescue.API.OData;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;

namespace FreshRescue.API.Extensions;

/// <summary>OData public search surface, separate from business API endpoints.</summary>
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
            .AddRouteComponents("odata", edmBuilder.GetEdmModel()));
    }
}
