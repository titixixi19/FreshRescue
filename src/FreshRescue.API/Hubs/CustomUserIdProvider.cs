using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace FreshRescue.API.Hubs;

/// <summary>
/// Trích xuất định danh người dùng từ token cho kết nối SignalR.
/// Hỗ trợ cả ClaimTypes.NameIdentifier và "sub".
/// </summary>
public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? connection.User?.FindFirst("sub")?.Value;
    }
}
