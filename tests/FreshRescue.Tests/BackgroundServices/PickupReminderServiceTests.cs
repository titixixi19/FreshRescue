using FreshRescue.API.BackgroundServices;
using FreshRescue.Business.Features.Engagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FreshRescue.Tests.BackgroundServices;

/// <summary>
/// Kiểm thử tiến trình nền nhắc nhở nhận hàng (FR-X05, T4-HIEU-01..03, T5-HIEU-02).
/// </summary>
public class PickupReminderServiceTests
{
    [Fact]
    public async Task ProcessPickupRemindersAsync_GoiNotificationServiceThanhCongAsync()
    {
        // Arrange
        var mockNotificationService = new Mock<INotificationService>();
        mockNotificationService
            .Setup(s => s.CreatePickupRemindersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(INotificationService)))
            .Returns(mockNotificationService.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var service = new PickupReminderService(
            scopeFactoryMock.Object,
            TimeProvider.System,
            NullLogger<PickupReminderService>.Instance);

        // Act
        await service.ProcessPickupRemindersAsync(CancellationToken.None);

        // Assert
        mockNotificationService.Verify(s => s.CreatePickupRemindersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
