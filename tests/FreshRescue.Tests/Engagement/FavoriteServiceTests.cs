using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Engagement;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Tests.Engagement;

/// <summary>
/// Kiểm thử đơn vị tính năng Cửa hàng yêu thích (FR-C12, T5-HIEU-03).
/// </summary>
public class FavoriteServiceTests
{
    private readonly EngagementFixture _f = new();

    [Fact]
    public async Task AddFavoriteAsync_StoreHopLe_ThemVaoYeuThichThanhCongAsync()
    {
        // Arrange
        var service = _f.FavoriteService();

        // Act
        var result = await service.AddFavoriteAsync(_f.Store.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var saved = await _f.Context.FavoriteStores
            .SingleOrDefaultAsync(fs => fs.CustomerUserId == _f.CustomerId && fs.StoreId == _f.Store.Id);
        saved.Should().NotBeNull();
        saved!.CreatedAt.Should().Be(EngagementFixture.Now);
    }

    [Fact]
    public async Task AddFavoriteAsync_StoreDaCo_IdempotentKhongTaoTrungAsync()
    {
        // Arrange
        _f.Context.FavoriteStores.Add(new FavoriteStore
        {
            CustomerUserId = _f.CustomerId,
            StoreId = _f.Store.Id,
            CreatedAt = EngagementFixture.Now.AddHours(-1)
        });
        await _f.Context.SaveChangesAsync();

        var service = _f.FavoriteService();

        // Act
        var result = await service.AddFavoriteAsync(_f.Store.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var count = await _f.Context.FavoriteStores
            .CountAsync(fs => fs.CustomerUserId == _f.CustomerId && fs.StoreId == _f.Store.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddFavoriteAsync_StoreKhongTonTai_NemNotFoundAsync()
    {
        // Arrange
        var service = _f.FavoriteService();
        var nonExistentStoreId = Guid.NewGuid();

        // Act
        var act = () => service.AddFavoriteAsync(nonExistentStoreId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.NotFound);
        ex.Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task AddFavoriteAsync_StoreBiXoaMem_NemNotFoundAsync()
    {
        // Arrange
        var deletedStore = new Store
        {
            Id = Guid.NewGuid(),
            BrandId = _f.BrandId,
            Name = "Chi nhánh đã đóng",
            IsDeleted = true,
            DeletedAt = EngagementFixture.Now.AddDays(-1),
        };
        _f.Context.Stores.Add(deletedStore);
        await _f.Context.SaveChangesAsync();

        var service = _f.FavoriteService();

        // Act
        var act = () => service.AddFavoriteAsync(deletedStore.Id, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_StoreDaYeuThich_XoaThanhCongAsync()
    {
        // Arrange
        _f.Context.FavoriteStores.Add(new FavoriteStore
        {
            CustomerUserId = _f.CustomerId,
            StoreId = _f.Store.Id,
            CreatedAt = EngagementFixture.Now.AddHours(-1)
        });
        await _f.Context.SaveChangesAsync();

        var service = _f.FavoriteService();

        // Act
        var result = await service.RemoveFavoriteAsync(_f.Store.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var exists = await _f.Context.FavoriteStores
            .AnyAsync(fs => fs.CustomerUserId == _f.CustomerId && fs.StoreId == _f.Store.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveFavoriteAsync_StoreChuaYeuThich_IdempotentTraVeTrueAsync()
    {
        // Arrange
        var service = _f.FavoriteService();

        // Act
        var result = await service.RemoveFavoriteAsync(_f.Store.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyFavoritesAsync_TraVeDanhSachChiNhanhPhanTrangAsync()
    {
        // Arrange
        var store2 = new Store
        {
            Id = Guid.NewGuid(),
            BrandId = _f.BrandId,
            Brand = _f.Brand,
            Name = "Tous Les Jours Lê Thánh Tôn",
            AddressLine = "59 Lê Thánh Tôn",
            Ward = "Bến Nghé",
            District = "Quận 1",
            City = "TP. Hồ Chí Minh",
            RatingAverage = 4.5m,
        };
        _f.Context.Stores.Add(store2);

        _f.Context.FavoriteStores.AddRange(
            new FavoriteStore
            {
                CustomerUserId = _f.CustomerId,
                Store = _f.Store,
                StoreId = _f.Store.Id,
                CreatedAt = EngagementFixture.Now.AddHours(-2),
            },
            new FavoriteStore
            {
                CustomerUserId = _f.CustomerId,
                Store = store2,
                StoreId = store2.Id,
                CreatedAt = EngagementFixture.Now.AddHours(-1),
            });
        await _f.Context.SaveChangesAsync();

        var service = _f.FavoriteService();

        // Act
        var result = await service.GetMyFavoritesAsync(page: 1, pageSize: 10, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        // Mới nhất trước: store2 trước, sau đó tới store1
        result.Items[0].StoreId.Should().Be(store2.Id);
        result.Items[0].StoreName.Should().Be("Tous Les Jours Lê Thánh Tôn");
        result.Items[0].BrandName.Should().Be("Tous Les Jours");
        result.Items[1].StoreId.Should().Be(_f.Store.Id);
    }

    [Fact]
    public async Task GetMyFavoritesAsync_KhongLayCuaUserKhacAsync()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        _f.Context.FavoriteStores.Add(new FavoriteStore
        {
            CustomerUserId = otherUserId,
            Store = _f.Store,
            StoreId = _f.Store.Id,
            CreatedAt = EngagementFixture.Now,
        });
        await _f.Context.SaveChangesAsync();

        var service = _f.FavoriteService();

        // Act
        var result = await service.GetMyFavoritesAsync(page: 1, pageSize: 10, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
