using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using Microsoft.Extensions.Primitives;

namespace FreshRescue.WebClient.Services.Mock;

/// <summary>
/// Các phương thức mở rộng giả lập (Mock) cho khu vực Dashboard (phục vụ nhiệm vụ TV5).
/// </summary>
public static class MockApiStoreDashboardExtensions
{
    // ID ổn định giữa các request — bắt buộc để wizard đối chiếu SelectedTemplateIds / StoreId
    // sau khi tải lại từ API (không tin hidden input).
    private static readonly Guid TemplateId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TemplateId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StoreId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StoreId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static object GetBrandOfferTemplates(this MockApiStore store, Dictionary<string, StringValues> query)
    {
        return new PagedResponse<OfferTemplateDto>
        {
            Items = new List<OfferTemplateDto>
            {
                new OfferTemplateDto
                {
                    Id = TemplateId1,
                    Name = "Bánh mì sandwich cứu trợ",
                    OriginalPrice = 30000,
                    SuggestedPrice = 15000
                },
                new OfferTemplateDto
                {
                    Id = TemplateId2,
                    Name = "Cơm hộp suất lẻ cuối ngày",
                    OriginalPrice = 50000,
                    SuggestedPrice = 25000
                }
            },
            Page = 1,
            PageSize = 100,
            TotalCount = 2,
            TotalPages = 1
        };
    }

    public static object GetBrandStores(this MockApiStore store, Dictionary<string, StringValues> query)
    {
        return new PagedResponse<StoreDto>
        {
            Items = new List<StoreDto>
            {
                new StoreDto { Id = StoreId1, Name = "Chi nhánh Quận 1 - FreshRescue" },
                new StoreDto { Id = StoreId2, Name = "Chi nhánh Thủ Đức - FreshRescue" }
            },
            Page = 1,
            PageSize = 100,
            TotalCount = 2,
            TotalPages = 1
        };
    }

    public static object GetStaffSaleBatches(this MockApiStore store, Dictionary<string, StringValues> query)
    {
        return new PagedResponse<SaleBatchDto>
        {
            Items = new List<SaleBatchDto>
            {
                new SaleBatchDto
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Name = "Đợt bán Flash Sale Tối",
                    Status = "Draft",
                    SaleStartTime = DateTime.UtcNow.AddHours(1),
                    SaleEndTime = DateTime.UtcNow.AddHours(3)
                }
            },
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };
    }

    public static object GetStaffSaleBatch(this MockApiStore store, Guid id)
    {
        return new SaleBatchDto
        {
            Id = id,
            Name = "Đợt bán chi tiết",
            Status = "Draft",
            SaleStartTime = DateTime.UtcNow.AddHours(1),
            SaleEndTime = DateTime.UtcNow.AddHours(3),
            Offers = new List<StaffOfferDto>
            {
                new StaffOfferDto
                {
                    Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    Name = "Offer Mẫu 1",
                    ActualPrice = 15000,
                    TotalQuantity = 10,
                    RemainingQuantity = 10,
                    MaxPerCustomer = 2
                }
            }
        };
    }

    public static object CreateStaffSaleBatch(this MockApiStore store, object? body)
    {
        // Trả đúng SaleBatchDto để PostAsync<SaleBatchDto> deserialise ổn định.
        return new SaleBatchDto
        {
            Id = Guid.NewGuid(),
            Name = "Đợt bán mới (Mock)",
            Status = "Draft",
            SaleStartTime = DateTime.UtcNow.AddHours(1),
            SaleEndTime = DateTime.UtcNow.AddHours(3),
            PickupStartTime = DateTime.UtcNow.AddHours(3),
            PickupEndTime = DateTime.UtcNow.AddHours(5)
        };
    }

    public static object ActivateStaffSaleBatch(this MockApiStore store, Guid id)
    {
        return new { Success = true, Message = "Đã kích hoạt đợt bán." };
    }

    public static object CancelStaffSaleBatch(this MockApiStore store, Guid id, object? body)
    {
        return new { Success = true, Message = "Đã hủy đợt bán." };
    }

    public static object GetStaffSaleBatchOrders(this MockApiStore store, Guid id, Dictionary<string, StringValues> query)
    {
        return new PagedResponse<StaffOrderDto>
        {
            Items = new List<StaffOrderDto>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0
        };
    }

    public static object RedeemOrder(this MockApiStore store, object? body)
    {
        return new RedeemOrderResponse
        {
            OrderId = Guid.NewGuid(),
            OrderCode = "ORD-MOCK-001",
            Status = "Redeemed",
            Message = "Xác nhận đổi quà thành công.",
            RedeemedAt = DateTime.UtcNow
        };
    }

    public static object UpdateStaffOfferQuantity(this MockApiStore store, Guid id, object? body)
    {
        return new { Success = true, Message = "Đã cập nhật số lượng." };
    }

    public static object DeleteStaffOffer(this MockApiStore store, Guid id)
    {
        return new { Success = true, Message = "Đã xóa offer." };
    }
}
