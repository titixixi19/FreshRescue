using FreshRescue.API.Hubs;
using FreshRescue.Entities.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FreshRescue.Tests.Realtime;

public class OfferRealtimeNotifierTests
{
    private readonly Mock<IHubContext<OfferHub>> _hubContextMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly OfferRealtimeNotifier _notifier;

    public OfferRealtimeNotifierTests()
    {
        _hubContextMock = new Mock<IHubContext<OfferHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _hubClientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _notifier = new OfferRealtimeNotifier(_hubContextMock.Object, NullLogger<OfferRealtimeNotifier>.Instance);
    }

    [Fact]
    public async Task NotifyOfferQuantityChanged_PhatSuKienDungGroupBatchVaDungTenEventAsync()
    {
        // Arrange
        var offerId = Guid.NewGuid();
        var saleBatchId = Guid.NewGuid();
        var remaining = 5;

        // Act
        await _notifier.NotifyOfferQuantityChangedAsync(offerId, saleBatchId, remaining);

        // Assert
        _hubClientsMock.Verify(c => c.Group($"batch:{saleBatchId}"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("OfferQuantityChanged", It.Is<object?[]>(args => args.Length == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifySaleBatchActivated_PhatSuKienChoStoreGroupVaAllAsync()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Act
        await _notifier.NotifySaleBatchActivatedAsync(batchId, storeId, "Cửa hàng Landmark", 3);

        // Assert
        _hubClientsMock.Verify(c => c.Group($"store:{storeId}"), Times.Once);
        _hubClientsMock.Verify(c => c.All, Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("SaleBatchActivated", It.Is<object?[]>(args => args.Length == 1), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task NotifySaleBatchClosed_PhatSuKienChoBatchGroupVaAllAsync()
    {
        // Arrange
        var batchId = Guid.NewGuid();

        // Act
        await _notifier.NotifySaleBatchClosedAsync(batchId);

        // Assert
        _hubClientsMock.Verify(c => c.Group($"batch:{batchId}"), Times.Once);
        _hubClientsMock.Verify(c => c.All, Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("SaleBatchClosed", It.Is<object?[]>(args => args.Length == 1), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task NotifyOrderStatusChanged_PhatSuKienRiengChoChuDonAsync()
    {
        // Arrange
        var customerUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act
        await _notifier.NotifyOrderStatusChangedAsync(customerUserId, orderId, OrderStatus.Paid.ToString());

        // Assert
        _hubClientsMock.Verify(c => c.User(customerUserId.ToString()), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("OrderStatusChanged", It.Is<object?[]>(args => args.Length == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OfferHub_JoinVaLeaveBatch_GoiDungGroupManagerAsync()
    {
        // Arrange
        var hub = new OfferHub();
        var groupManagerMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.ConnectionId).Returns("conn-123");

        hub.Groups = groupManagerMock.Object;
        hub.Context = contextMock.Object;

        var batchId = Guid.NewGuid();

        // Act
        await hub.JoinBatchAsync(batchId);
        await hub.LeaveBatchAsync(batchId);

        // Assert
        groupManagerMock.Verify(g => g.AddToGroupAsync("conn-123", $"batch:{batchId}", default), Times.Once);
        groupManagerMock.Verify(g => g.RemoveFromGroupAsync("conn-123", $"batch:{batchId}", default), Times.Once);
    }

    [Fact]
    public async Task OfferHub_JoinVaLeaveStore_GoiDungGroupManagerAsync()
    {
        // Arrange
        var hub = new OfferHub();
        var groupManagerMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.ConnectionId).Returns("conn-456");

        hub.Groups = groupManagerMock.Object;
        hub.Context = contextMock.Object;

        var storeId = Guid.NewGuid();

        // Act
        await hub.JoinStoreAsync(storeId);
        await hub.LeaveStoreAsync(storeId);

        // Assert
        groupManagerMock.Verify(g => g.AddToGroupAsync("conn-456", $"store:{storeId}", default), Times.Once);
        groupManagerMock.Verify(g => g.RemoveFromGroupAsync("conn-456", $"store:{storeId}", default), Times.Once);
    }
}
