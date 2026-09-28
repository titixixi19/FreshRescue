using Microsoft.AspNetCore.SignalR;

namespace FreshRescue.API.Hubs;

/// <summary>
/// Chủ sở hữu: Hiếu (T5-HIEU-01). Map sẵn tại <c>/hubs/offers</c> trong Program.cs.
/// Method và sự kiện theo 03-API-CONTRACT.md §10.
/// </summary>
public class OfferHub : Hub
{
    // TODO(Hieu, 2026-10-13): JoinBatch, LeaveBatch, JoinStore.
}
