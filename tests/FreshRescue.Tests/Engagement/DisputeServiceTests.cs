using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Business.Features.Engagement.Validators;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.Repositories;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace FreshRescue.Tests.Engagement;

public sealed class DisputeServiceTests
{
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly FreshRescueDbContext _db;
    private readonly Order _order;
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IDisputeWorkflowRepository> _workflow = new();
    private readonly DisputeService _service;
    private readonly DateTime _now = new(2026, 10, 29, 3, 0, 0, DateTimeKind.Utc);

    public DisputeServiceTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new FreshRescueDbContext(options, TimeProvider.System);
        var store = new Store
        {
            Name = "Demo",
            AddressLine = "1 Đường A",
            Ward = "Ward",
            District = "District",
            City = "Hanoi",
        };
        _order = new Order
        {
            OrderCode = "FR-TEST1234",
            CustomerUserId = _customerId,
            StoreId = store.Id,
            Status = OrderStatus.Redeemed,
            RedeemedAt = _now.AddMinutes(-30),
            RefundStatus = RefundStatus.None,
            TotalAmount = 70000,
        };
        _db.AddRange(store, _order);
        _db.SaveChanges();
        _user.SetupGet(x => x.UserId).Returns(_customerId);
        _user.Setup(x => x.IsInRole(AppRoles.Customer)).Returns(true);
        _workflow.Setup(x => x.TryResolveAsync(
                It.IsAny<Guid>(), It.IsAny<DisputeStatus>(), It.IsAny<Guid>(),
                It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _workflow.Setup(x => x.TryRefundOrderAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var clock = new Mock<TimeProvider>();
        clock.Setup(x => x.GetUtcNow()).Returns(new DateTimeOffset(_now));
        _service = new DisputeService(
            new UnitOfWork(_db), _workflow.Object, _user.Object, clock.Object,
            new CreateDisputeRequestValidator(), new RejectDisputeRequestValidator(),
            new MerchantNoteRequestValidator());
    }

    [Fact]
    public async Task Create_RedeemedWithin2Hours_CreatesOpenDisputeAsync()
    {
        var result = await _service.CreateAsync(_order.Id, ValidRequest(), CancellationToken.None);
        result.Status.Should().Be(DisputeStatus.Open);
        result.CustomerUserId.Should().Be(_customerId);
        (await _db.Disputes.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Create_TooLate_ReturnsDisputeWindowPassedAsync()
    {
        _order.RedeemedAt = _now.AddHours(-3);
        await _db.SaveChangesAsync();
        await ShouldFailAsync(ErrorCodes.DisputeWindowPassed,
            () => _service.CreateAsync(_order.Id, ValidRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task Create_NotRedeemed_ReturnsValidationFailedAsync()
    {
        _order.Status = OrderStatus.Paid;
        await _db.SaveChangesAsync();
        await ShouldFailAsync(ErrorCodes.ValidationFailed,
            () => _service.CreateAsync(_order.Id, ValidRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task Create_WrongCustomer_ReturnsForbiddenAsync()
    {
        _order.CustomerUserId = Guid.NewGuid();
        await _db.SaveChangesAsync();
        await ShouldFailAsync(ErrorCodes.ForbiddenScope,
            () => _service.CreateAsync(_order.Id, ValidRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task Create_Duplicate_ReturnsDisputeAlreadyExistsAsync()
    {
        await _service.CreateAsync(_order.Id, ValidRequest(), CancellationToken.None);
        await ShouldFailAsync(ErrorCodes.DisputeAlreadyExists,
            () => _service.CreateAsync(_order.Id, ValidRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task Create_NoPhoto_ReturnsValidationFailedAsync()
    {
        await ShouldFailAsync(ErrorCodes.ValidationFailed,
            () => _service.CreateAsync(
                _order.Id, new CreateDisputeRequest(DisputeReason.Spoiled, "Hỏng", ""), CancellationToken.None));
    }

    [Fact]
    public async Task Approve_RefundsFullAmount_WithoutChangingOrderStatusAsync()
    {
        var dispute = await _service.CreateAsync(_order.Id, ValidRequest(), CancellationToken.None);
        AsAdmin();
        var approved = await _service.ApproveAsync(dispute.Id, CancellationToken.None);
        approved.Status.Should().Be(DisputeStatus.Approved);
        var payment = await _db.Payments.SingleAsync();
        payment.Amount.Should().Be(-70000);
        payment.Method.Should().Be(PaymentMethod.Refund);
        payment.Status.Should().Be(PaymentStatus.Succeeded);
        _order.Status.Should().Be(OrderStatus.Redeemed);
        _workflow.Verify(x => x.TryRefundOrderAsync(
            _order.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_DoesNotRefundAsync()
    {
        var dispute = await _service.CreateAsync(_order.Id, ValidRequest(), CancellationToken.None);
        AsAdmin();
        var result = await _service.RejectAsync(
            dispute.Id, new RejectDisputeRequest("Bằng chứng không đủ"), CancellationToken.None);
        result.Status.Should().Be(DisputeStatus.Rejected);
        (await _db.Payments.CountAsync()).Should().Be(0);
        _workflow.Verify(x => x.TryRefundOrderAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Approve_AlreadyRefunded_RefusesAsync()
    {
        var dispute = await _service.CreateAsync(_order.Id, ValidRequest(), CancellationToken.None);
        _order.RefundStatus = RefundStatus.Refunded;
        await _db.SaveChangesAsync();
        AsAdmin();
        await ShouldFailAsync(ErrorCodes.ConcurrencyConflict,
            () => _service.ApproveAsync(dispute.Id, CancellationToken.None));
    }

    private void AsAdmin()
    {
        _user.SetupGet(x => x.UserId).Returns(_adminId);
        _user.Setup(x => x.IsInRole(AppRoles.SystemAdmin)).Returns(true);
    }

    private static CreateDisputeRequest ValidRequest()
        => new(DisputeReason.Spoiled, "Thực phẩm bị hỏng", "/uploads/disputes/a.jpg");

    private static async Task ShouldFailAsync(string code, Func<Task> action)
    {
        var ex = await Assert.ThrowsAsync<AppException>(action);
        ex.ErrorCode.Should().Be(code);
    }
}
