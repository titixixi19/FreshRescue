namespace FreshRescue.API.Extensions;

/// <summary>Chủ sở hữu: Hiếu. SignalR <c>OfferHub</c> (03-API-CONTRACT.md §10).</summary>
public static class RealtimeExtensions
{
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}
