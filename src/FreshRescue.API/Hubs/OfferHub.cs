using Microsoft.AspNetCore.SignalR;

namespace FreshRescue.API.Hubs;

/// <summary>
/// Chủ sở hữu: Hiếu (T5-HIEU-01). Map sẵn tại <c>/hubs/offers</c> trong Program.cs.
/// Method và sự kiện theo 03-API-CONTRACT.md §10.
/// </summary>
public class OfferHub : Hub
{
    public static string BatchGroup(Guid saleBatchId) => $"batch:{saleBatchId}";
    public static string StoreGroup(Guid storeId) => $"store:{storeId}";

    /// <summary>
    /// Vào nhóm theo dõi một đợt bán.
    /// </summary>
    [HubMethodName("JoinBatch")]
    public async Task JoinBatchAsync(Guid saleBatchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, BatchGroup(saleBatchId));
    }

    /// <summary>
    /// Rời nhóm theo dõi một đợt bán.
    /// </summary>
    [HubMethodName("LeaveBatch")]
    public async Task LeaveBatchAsync(Guid saleBatchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, BatchGroup(saleBatchId));
    }

    /// <summary>
    /// Theo dõi mọi đợt bán của một chi nhánh.
    /// </summary>
    [HubMethodName("JoinStore")]
    public async Task JoinStoreAsync(Guid storeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, StoreGroup(storeId));
    }

    /// <summary>
    /// Rời nhóm theo dõi chi nhánh.
    /// </summary>
    [HubMethodName("LeaveStore")]
    public async Task LeaveStoreAsync(Guid storeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, StoreGroup(storeId));
    }
}
