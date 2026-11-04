using System.Globalization;
using FreshRescue.WebClient.Areas.Customer.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Customer.Controllers;

/// <summary>Cửa hàng gần tôi (FR-C02) và chi tiết cửa hàng + đợt đang mở + đánh giá (FR-C04).</summary>
[Area("Customer")]
[AllowAnonymous]
public class StoresController : Controller
{
    // Trung tâm Quận 1 — dùng khi khách chưa cho trình duyệt lấy vị trí.
    private const double DefaultLat = 10.7757;
    private const double DefaultLng = 106.7004;
    private const int PageSize = 12;
    private const int ReviewPageSize = 5;

    private readonly IApiClient _api;

    public StoresController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    [ActionName("Index")]
    public async Task<IActionResult> IndexAsync(
        double? lat, double? lng, double radiusKm = 10, bool hasActiveBatch = false, int page = 1,
        CancellationToken cancellationToken = default)
    {
        var isDefault = lat is null || lng is null;
        var vm = new NearbyStoresViewModel
        {
            Lat = lat ?? DefaultLat,
            Lng = lng ?? DefaultLng,
            RadiusKm = Math.Clamp(radiusKm, 1, 50),
            HasActiveBatch = hasActiveBatch,
            IsDefaultLocation = isDefault,
            Stores = await _api.GetAsync<PagedResponse<StoreNearbyDto>>(
                QueryHelpers.AddQueryString("stores/nearby", new Dictionary<string, string?>
                {
                    ["lat"] = (lat ?? DefaultLat).ToString("0.######", CultureInfo.InvariantCulture),
                    ["lng"] = (lng ?? DefaultLng).ToString("0.######", CultureInfo.InvariantCulture),
                    ["radiusKm"] = Math.Clamp(radiusKm, 1, 50).ToString(CultureInfo.InvariantCulture),
                    ["hasActiveBatch"] = hasActiveBatch ? "true" : null,
                    ["page"] = Math.Max(page, 1).ToString(CultureInfo.InvariantCulture),
                    ["pageSize"] = PageSize.ToString(CultureInfo.InvariantCulture),
                }),
                cancellationToken),
        };

        return View("Index", vm);
    }

    [HttpGet]
    [ActionName("Details")]
    public async Task<IActionResult> DetailsAsync(Guid id, int page = 1, CancellationToken cancellationToken = default)
    {
        var store = await _api.GetAsync<StoreDetailDto>($"stores/{id}", cancellationToken);
        var reviews = await _api.GetAsync<PagedResponse<StoreReviewDto>>(
            $"stores/{id}/reviews?page={Math.Max(page, 1)}&pageSize={ReviewPageSize}", cancellationToken);

        bool? isFavorite = null;
        if (User.IsInRole(AppRoles.Customer))
        {
            var favorites = await _api.GetAsync<PagedResponse<FavoriteStoreDto>>(
                $"me/favorites?pageSize={PagedResponse<FavoriteStoreDto>.MaxPageSize}", cancellationToken);
            isFavorite = favorites.Items.Any(f => f.StoreId == id);
        }

        return View("Details", new StoreDetailViewModel { Store = store, Reviews = reviews, IsFavorite = isFavorite });
    }
}
