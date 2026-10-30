using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;

namespace FreshRescue.Business.Features.Ordering;

/// <summary>
/// Bộ tính toán chỉ số thống kê thương hiệu theo BR-25 (03-API-CONTRACT.md §7).
/// Dùng chung giữa GET /brand/stats và Sheet Summary của OrderExportService.
/// </summary>
public static class BrandStatsCalculator
{
    public static BrandStatsDto Calculate(IReadOnlyList<Order> orders, IReadOnlyCollection<Store> stores)
    {
        var totalRevenue = orders
            .Where(o => (o.Status is OrderStatus.Redeemed or OrderStatus.NoShow) && o.RefundStatus == RefundStatus.None)
            .Sum(o => o.StorePayoutAmount);

        var totalRescuedPortions = orders
            .Where(o => o.Status == OrderStatus.Redeemed)
            .Sum(o => o.Items.Sum(i => i.Quantity));

        var storeGroups = orders.GroupBy(o => o.StoreId).ToDictionary(g => g.Key, g => g.ToList());

        var byStore = new List<StoreStatsDto>();
        foreach (var store in stores)
        {
            var storeOrders = storeGroups.GetValueOrDefault(store.Id, []);

            var revenue = storeOrders
                .Where(o => (o.Status is OrderStatus.Redeemed or OrderStatus.NoShow) && o.RefundStatus == RefundStatus.None)
                .Sum(o => o.StorePayoutAmount);

            var rescued = storeOrders
                .Where(o => o.Status == OrderStatus.Redeemed)
                .Sum(o => o.Items.Sum(i => i.Quantity));

            var redeemedCount = storeOrders.Count(o => o.Status == OrderStatus.Redeemed);
            var noShowCount = storeOrders.Count(o => o.Status == OrderStatus.NoShow);
            var totalFinished = redeemedCount + noShowCount;
            var noShowRate = totalFinished > 0
                ? Math.Round((double)noShowCount / totalFinished, 2, MidpointRounding.AwayFromZero)
                : 0.0;

            byStore.Add(new StoreStatsDto
            {
                StoreId = store.Id,
                StoreName = store.Name,
                Revenue = revenue,
                RescuedPortions = rescued,
                NoShowRate = noShowRate,
                RatingAverage = store.RatingAverage
            });
        }

        return new BrandStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalRescuedPortions = totalRescuedPortions,
            ByStore = byStore
        };
    }
}
