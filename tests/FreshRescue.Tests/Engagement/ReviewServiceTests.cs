using AutoMapper;
using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Business.Features.Engagement.Validators;
using FreshRescue.Business.Mapping;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.Repositories;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace FreshRescue.Tests.Engagement;

public sealed class ReviewServiceTests
{
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _otherCustomerId = Guid.NewGuid();
    private readonly FreshRescueDbContext _db;
    private readonly Store _store;
    private readonly Order _order;
    private readonly Mock<IStoreRatingRepository> _ratingRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new FreshRescueDbContext(options, TimeProvider.System);

        _store = new Store
        {
            Name = "Chi nhánh demo",
            AddressLine = "123 Example",
            Ward = "Ward 1",
            District = "District 1",
            City = "HCM",
        };
        _order = CreateOrder(_store.Id, _customerId, OrderStatus.Redeemed);
        _db.Stores.Add(_store);
        _db.Orders.Add(_order);
        _db.SaveChanges();

        _currentUser.SetupGet(x => x.UserId).Returns(_customerId);
        _ratingRepository
            .Setup(x => x.AddRatingAsync(It.IsAny<Guid>(), It.IsAny<byte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<ReviewProfile>()).CreateMapper();
        _service = new ReviewService(
            new UnitOfWork(_db),
            _ratingRepository.Object,
            _currentUser.Object,
            new CreateReviewRequestValidator(),
            mapper);
    }

    [Fact]
    public async Task CreateAsync_RedeemedOwner_CreatesReviewAndCallsAtomicRatingAsync()
    {
        var result = await _service.CreateAsync(
            _order.Id,
            new CreateReviewRequest(5, "Tốt", "/uploads/reviews/a.jpg"),
            CancellationToken.None);

        var stored = await _db.Reviews.SingleAsync();
        stored.OrderId.Should().Be(_order.Id);
        stored.CustomerUserId.Should().Be(_customerId);
        result.StoreId.Should().Be(_store.Id);
        result.Rating.Should().Be(5);
        result.Comment.Should().Be("Tốt");
        result.CreatedAt.Should().NotBe(default);
        (await _db.Reviews.CountAsync()).Should().Be(1);
        _ratingRepository.Verify(
            x => x.AddRatingAsync(_store.Id, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PendingPayment_ReturnsValidationFailedAsync()
    {
        _order.Status = OrderStatus.PendingPayment;
        await _db.SaveChangesAsync();

        await AssertAppErrorAsync(
            ErrorCodes.ValidationFailed,
            () => _service.CreateAsync(_order.Id, new CreateReviewRequest(4, null, null), CancellationToken.None));

        (await _db.Reviews.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_OtherCustomersOrder_ReturnsForbiddenAsync()
    {
        _order.CustomerUserId = _otherCustomerId;
        await _db.SaveChangesAsync();

        await AssertAppErrorAsync(
            ErrorCodes.ForbiddenScope,
            () => _service.CreateAsync(_order.Id, new CreateReviewRequest(5, null, null), CancellationToken.None));

        (await _db.Reviews.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ExistingReview_ReturnsReviewAlreadyExistsAsync()
    {
        _db.Reviews.Add(new Review
        {
            OrderId = _order.Id,
            CustomerUserId = _customerId,
            StoreId = _store.Id,
            Rating = 5,
        });
        await _db.SaveChangesAsync();

        await AssertAppErrorAsync(
            ErrorCodes.ReviewAlreadyExists,
            () => _service.CreateAsync(_order.Id, new CreateReviewRequest(4, null, null), CancellationToken.None));

        _ratingRepository.Verify(
            x => x.AddRatingAsync(It.IsAny<Guid>(), It.IsAny<byte>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateAsync_InvalidRating_ReturnsValidationFailedAsync(byte rating)
    {
        await AssertAppErrorAsync(
            ErrorCodes.ValidationFailed,
            () => _service.CreateAsync(_order.Id, new CreateReviewRequest(rating, null, null), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_UnknownOrder_ReturnsNotFoundAsync()
    {
        await AssertAppErrorAsync(
            ErrorCodes.NotFound,
            () => _service.CreateAsync(Guid.NewGuid(), new CreateReviewRequest(5, null, null), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_StoreUpdateMissing_DoesNotSucceedAsync()
    {
        _ratingRepository
            .Setup(x => x.AddRatingAsync(_store.Id, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await AssertAppErrorAsync(
            ErrorCodes.NotFound,
            () => _service.CreateAsync(_order.Id, new CreateReviewRequest(5, null, null), CancellationToken.None));

        // EF InMemory ignores transactions; SQL Server integration must verify rollback.
    }

    [Fact]
    public async Task GetStoreReviewsAsync_PageOne_ReturnsPagedResponseAsync()
    {
        var otherOrder = CreateOrder(_store.Id, _otherCustomerId, OrderStatus.Redeemed);
        _db.Orders.Add(otherOrder);
        _db.Reviews.AddRange(
            new Review { OrderId = _order.Id, CustomerUserId = _customerId, StoreId = _store.Id, Rating = 4 },
            new Review { OrderId = otherOrder.Id, CustomerUserId = _otherCustomerId, StoreId = _store.Id, Rating = 5 });
        await _db.SaveChangesAsync();

        var result = await _service.GetStoreReviewsAsync(_store.Id, 1, 1, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetStoreReviewsAsync_UnknownStore_ReturnsNotFoundAsync()
    {
        await AssertAppErrorAsync(
            ErrorCodes.NotFound,
            async () => await _service.GetStoreReviewsAsync(Guid.NewGuid(), 1, 20, CancellationToken.None));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetStoreReviewsAsync_InvalidPagination_ReturnsValidationFailedAsync(int page, int pageSize)
    {
        await AssertAppErrorAsync(
            ErrorCodes.ValidationFailed,
            async () => await _service.GetStoreReviewsAsync(_store.Id, page, pageSize, CancellationToken.None));
    }

    private static Order CreateOrder(Guid storeId, Guid customerId, OrderStatus status)
        => new()
        {
            OrderCode = "FR-ABCDEFGH",
            CustomerUserId = customerId,
            StoreId = storeId,
            SaleBatchId = Guid.NewGuid(),
            Status = status,
        };

    private static async Task AssertAppErrorAsync(string errorCode, Func<Task> action)
    {
        var assertion = await action.Should().ThrowAsync<AppException>();
        assertion.Which.ErrorCode.Should().Be(errorCode);
    }
}
