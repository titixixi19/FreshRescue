using AutoMapper;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Sales;

namespace FreshRescue.Business.Features.Catalog;

/// <summary>
/// Khi chiếu <see cref="Store"/> phải truyền thời điểm hiện tại:
/// <c>ProjectTo&lt;T&gt;(config, new { now })</c> — "đang mở bán" tính theo biến <c>now</c> này.
/// </summary>
public class StorefrontProfile : Profile
{
    public StorefrontProfile()
    {
        // Tham số hoá của AutoMapper: được thay bằng giá trị truyền vào ProjectTo.
        var now = DateTime.MinValue;

        CreateMap<Offer, ActiveOfferDto>()
            .ForMember(d => d.StoreId, o => o.MapFrom(s => s.SaleBatch.StoreId))
            .ForMember(d => d.StoreName, o => o.MapFrom(s => s.SaleBatch.Store.Name))
            .ForMember(d => d.DistanceKm, o => o.Ignore())
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.DiscountPercent, o => o.MapFrom(s => s.OriginalPrice > 0
                ? Math.Round((1 - s.ActualPrice / s.OriginalPrice) * 100, 1)
                : 0))
            .ForMember(d => d.SaleStartTime, o => o.MapFrom(s => s.SaleBatch.SaleStartTime))
            .ForMember(d => d.SaleEndTime, o => o.MapFrom(s => s.SaleBatch.SaleEndTime))
            .ForMember(d => d.PickupStartTime, o => o.MapFrom(s => s.SaleBatch.PickupStartTime))
            .ForMember(d => d.PickupEndTime, o => o.MapFrom(s => s.SaleBatch.PickupEndTime));

        CreateMap<Store, StoreNearbyDto>()
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand.Name))
            .ForMember(d => d.DistanceKm, o => o.Ignore())
            .ForMember(d => d.ActiveBatchCount, o => o.MapFrom(s => s.SaleBatches.Count(b =>
                b.Status == SaleBatchStatus.Active && b.SaleStartTime <= now && now < b.SaleEndTime)))
            .ForMember(d => d.MinPrice, o => o.MapFrom(s => s.SaleBatches
                .Where(b => b.Status == SaleBatchStatus.Active && b.SaleStartTime <= now && now < b.SaleEndTime)
                .SelectMany(b => b.Offers)
                .Where(x => !x.IsDeleted && x.RemainingQuantity > 0)
                .Min(x => (decimal?)x.ActualPrice)));

        CreateMap<Store, StoreDetailDto>()
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand.Name))
            .ForMember(d => d.ActiveBatches, o => o.MapFrom(s => s.SaleBatches
                .Where(b => b.Status == SaleBatchStatus.Active && b.SaleStartTime <= now && now < b.SaleEndTime)
                .OrderBy(b => b.SaleStartTime)));

        CreateMap<SaleBatch, StoreActiveBatchDto>()
            .ForMember(d => d.Offers, o => o.MapFrom(s => s.Offers.Where(x => !x.IsDeleted).OrderBy(x => x.Name)));
    }
}
