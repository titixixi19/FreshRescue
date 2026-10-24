using AutoMapper;
using AutoMapper.QueryableExtensions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Catalog;

public class StorefrontService : IStorefrontService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly TimeProvider _clock;

    public StorefrontService(IUnitOfWork uow, IMapper mapper, TimeProvider clock)
    {
        _uow = uow;
        _mapper = mapper;
        _clock = clock;
    }

    public async Task<PagedResponse<ActiveOfferDto>> ListActiveOffersAsync(
        ActiveOfferListQuery query, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var offers = _uow.Offers.Query().Where(o =>
            o.SaleBatch.Status == SaleBatchStatus.Active
            && o.SaleBatch.SaleStartTime <= now
            && now < o.SaleBatch.SaleEndTime
            && o.SaleBatch.Store.Status == StoreStatus.Active
            && o.SaleBatch.Store.Brand.Status == BrandStatus.Approved);

        if (query.StoreId is { } storeId)
        {
            offers = offers.Where(o => o.SaleBatch.StoreId == storeId);
        }

        if (query.CategoryId is { } categoryId)
        {
            offers = offers.Where(o => o.CategoryId == categoryId);
        }

        if (query.OfferType is { } offerType)
        {
            offers = offers.Where(o => o.OfferType == offerType);
        }

        if (query.MinPrice is { } minPrice)
        {
            offers = offers.Where(o => o.ActualPrice >= minPrice);
        }

        if (query.MaxPrice is { } maxPrice)
        {
            offers = offers.Where(o => o.ActualPrice <= maxPrice);
        }

        if (query.InStockOnly)
        {
            offers = offers.Where(o => o.RemainingQuantity > 0);
        }

        // Sắp theo hạn bán trước để khách thấy món sắp hết giờ; Id chốt thứ tự cho phân trang ổn định.
        return await offers
            .OrderBy(o => o.SaleBatch.SaleEndTime)
            .ThenBy(o => o.Name)
            .ThenBy(o => o.Id)
            .ToPagedResponseAsync<Entities.Sales.Offer, ActiveOfferDto>(
                query.Page, query.PageSize, _mapper.ConfigurationProvider, cancellationToken);
    }

    public async Task<ActiveOfferDto> GetOfferAsync(Guid offerId, CancellationToken cancellationToken)
    {
        // Đợt chưa Active (Draft/Scheduled) hoặc đã đóng/huỷ không lộ ra công khai: coi như không tồn tại.
        return await _uow.Offers.Query()
            .Where(o => o.Id == offerId
                && o.SaleBatch.Status == SaleBatchStatus.Active
                && o.SaleBatch.Store.Status == StoreStatus.Active
                && o.SaleBatch.Store.Brand.Status == BrandStatus.Approved)
            .ProjectTo<ActiveOfferDto>(_mapper.ConfigurationProvider)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy mặt hàng.");
    }

    public async Task<StoreDetailDto> GetStoreAsync(Guid storeId, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        return await _uow.Stores.Query()
            .Where(s => s.Id == storeId && s.Status == StoreStatus.Active && s.Brand.Status == BrandStatus.Approved)
            .ProjectTo<StoreDetailDto>(_mapper.ConfigurationProvider, new { now })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy chi nhánh.");
    }

    public async Task<PagedResponse<StoreNearbyDto>> FindNearbyStoresAsync(
        StoreNearbyQuery query, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var lat = (double)query.Lat!.Value;
        var lng = (double)query.Lng!.Value;

        // Bước 1 — hộp bao: lọc bằng chỉ mục (Latitude, Longitude), chưa tính lượng giác nào.
        var box = GeoDistance.BoundingBox(lat, lng, (double)query.RadiusKm);
        var stores = _uow.Stores.Query().Where(s =>
            s.Status == StoreStatus.Active
            && s.Brand.Status == BrandStatus.Approved
            && s.Latitude >= box.MinLat && s.Latitude <= box.MaxLat
            && s.Longitude >= box.MinLng && s.Longitude <= box.MaxLng);

        if (query.CategoryId is { } categoryId)
        {
            stores = stores.Where(s => s.SaleBatches.Any(b =>
                b.Status != SaleBatchStatus.Cancelled && b.Offers.Any(o => o.CategoryId == categoryId)));
        }

        if (query.HasActiveBatch)
        {
            stores = stores.Where(s => s.SaleBatches.Any(b =>
                b.Status == SaleBatchStatus.Active && b.SaleStartTime <= now && now < b.SaleEndTime));
        }

        var candidates = await stores
            .ProjectTo<StoreNearbyDto>(_mapper.ConfigurationProvider, new { now })
            .ToListAsync(cancellationToken);

        // Bước 2 — Haversine chính xác trên tập nhỏ đã lọc, cắt góc hộp bao ngoài bán kính, sắp gần nhất trước.
        var ranked = candidates
            .Select(s => s with
            {
                DistanceKm = (decimal)Math.Round(
                    GeoDistance.HaversineKm(lat, lng, (double)s.Latitude, (double)s.Longitude), 2),
            })
            .Where(s => s.DistanceKm <= query.RadiusKm)
            .OrderBy(s => s.DistanceKm)
            .ThenBy(s => s.Name)
            .ThenBy(s => s.Id)
            .ToList();

        return new PagedResponse<StoreNearbyDto>
        {
            Items = ranked.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = ranked.Count,
        };
    }
}
