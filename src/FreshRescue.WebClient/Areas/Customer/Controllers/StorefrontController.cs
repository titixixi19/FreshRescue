using FreshRescue.WebClient.Areas.Customer.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Customer.Controllers;

/// <summary>Storefront công khai: lọc offer đang bán (FR-C03) và chi tiết một offer (T3-TV4-02, T4-TV4-01).</summary>
[Area("Customer")]
[AllowAnonymous]
public class StorefrontController : Controller
{
    private const int PageSize = 12;

    private readonly IApiClient _api;
    private readonly TimeProvider _clock;
    private readonly ILogger<StorefrontController> _logger;

    public StorefrontController(IApiClient api, TimeProvider clock, ILogger<StorefrontController> logger)
    {
        _api = api;
        _clock = clock;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> IndexAsync([FromQuery] StorefrontQuery query, CancellationToken cancellationToken)
    {
        if (query.MinPrice > query.MaxPrice)
        {
            (query.MinPrice, query.MaxPrice) = (query.MaxPrice, query.MinPrice);
        }

        var offers = await _api.GetAsync<PagedResponse<ActiveOfferDto>>(
            QueryHelpers.AddQueryString("offers/active", query.ToApiQuery(PageSize)),
            cancellationToken);

        return View(new StorefrontViewModel
        {
            Query = query,
            Offers = offers,
            Categories = await GetCategoriesAsync(cancellationToken),
        });
    }

    [HttpGet]
    public async Task<IActionResult> OfferAsync(Guid id, CancellationToken cancellationToken)
    {
        var offer = await _api.GetAsync<ActiveOfferDto>($"offers/{id}", cancellationToken);
        var now = _clock.GetUtcNow().UtcDateTime;
        var isOnSale = offer.SaleStartTime <= now && now <= offer.SaleEndTime;

        IReadOnlyList<ActiveOfferDto> batchOffers = [offer];
        if (isOnSale)
        {
            var sameStore = await _api.GetAsync<PagedResponse<ActiveOfferDto>>(
                QueryHelpers.AddQueryString(
                    "offers/active",
                    new Dictionary<string, string?>
                    {
                        ["storeId"] = offer.StoreId.ToString(),
                        ["pageSize"] = PagedResponse<ActiveOfferDto>.MaxPageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    }),
                cancellationToken);

            // Một đơn chỉ thuộc một đợt bán (BR-03) — chi nhánh có thể đang mở song song đợt khác.
            batchOffers = sameStore.Items.Where(o => o.SaleBatchId == offer.SaleBatchId).ToList();
            if (!batchOffers.Any(o => o.Id == offer.Id))
            {
                batchOffers = [offer, .. batchOffers];
            }
        }

        return View(new OfferDetailViewModel { Offer = offer, BatchOffers = batchOffers, IsOnSale = isOnSale });
    }

    // Thiếu danh mục thì vẫn bán được — chỉ mất ô lọc, đừng làm sập cả storefront.
    private async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _api.GetAsync<PagedResponse<CategoryDto>>(
                $"categories?pageSize={PagedResponse<CategoryDto>.MaxPageSize}",
                cancellationToken);
            return categories.Items;
        }
        catch (ApiException ex) when (ex.Status != StatusCodes.Status401Unauthorized)
        {
            _logger.LogWarning("Không tải được danh mục, ẩn ô lọc. Status={Status}, ErrorCode={ErrorCode}", ex.Status, ex.ErrorCode);
            return [];
        }
    }
}
