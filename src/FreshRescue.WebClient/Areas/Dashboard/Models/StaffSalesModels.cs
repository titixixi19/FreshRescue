using System.ComponentModel.DataAnnotations;

namespace FreshRescue.WebClient.Areas.Dashboard.Models;

// —— Offer templates (GET brand/offer-templates) — dùng bước 1 ——

public class OfferTemplateDto
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string OfferType { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal BrandMinDiscountPercent { get; set; }
    public decimal BrandMaxDiscountPercent { get; set; }
}

// —— Sale batch ——

public class SaleBatchDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string? StoreName { get; set; }
    public string? Name { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime SaleStartTime { get; set; }
    public DateTime SaleEndTime { get; set; }
    public DateTime PickupStartTime { get; set; }
    public DateTime PickupEndTime { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? TotalUnsoldQuantity { get; set; }
    public string? CancelReason { get; set; }
    public IReadOnlyList<StaffOfferDto>? Offers { get; set; }
}

public class StaffOfferDto
{
    public Guid Id { get; set; }
    public Guid SaleBatchId { get; set; }
    public Guid OfferTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string OfferType { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal ActualPrice { get; set; }
    public int TotalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public int MaxPerCustomer { get; set; }
}

public class CreateSaleBatchRequest
{
    public string? Name { get; set; }
    public Guid? StoreId { get; set; }
    public DateTime SaleStartTime { get; set; }
    public DateTime SaleEndTime { get; set; }
    public DateTime PickupStartTime { get; set; }
    public DateTime PickupEndTime { get; set; }
    public List<CreateSaleBatchOfferRequest> Offers { get; set; } = new();
}

public class CreateSaleBatchOfferRequest
{
    public Guid OfferTemplateId { get; set; }
    public decimal ActualPrice { get; set; }
    public int TotalQuantity { get; set; }
    public int MaxPerCustomer { get; set; } = 3;
}

public class CancelBatchRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class AdjustOfferQuantityRequest
{
    public int TotalQuantity { get; set; }
}

// —— Staff orders in batch (GET staff/sale-batches/{id}/orders) ——

public class StaffOrderDto
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public IReadOnlyList<StaffOrderItemDto> Items { get; set; } = Array.Empty<StaffOrderItemDto>();
}

public class StaffOrderItemDto
{
    public Guid OfferId { get; set; }
    public string OfferName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

// —— List VMs ——

public class SaleBatchListViewModel
{
    public IReadOnlyList<SaleBatchDto> Items { get; set; } = Array.Empty<SaleBatchDto>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? StatusFilter { get; set; }
    public string? ErrorMessage { get; set; }
}

public class StaffOrderListViewModel
{
    public Guid SaleBatchId { get; set; }
    public string? BatchName { get; set; }
    public IReadOnlyList<StaffOrderDto> Items { get; set; } = Array.Empty<StaffOrderDto>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? StatusFilter { get; set; }
    public string? ErrorMessage { get; set; }
}

// —— Wizard state (POST qua form, không dùng Session) ——

public class WizardOfferLine
{
    public Guid OfferTemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public decimal SuggestedPrice { get; set; }
    public decimal OriginalPrice { get; set; }

    [Range(1, 10000, ErrorMessage = "Số lượng phải ≥ 1.")]
    public int TotalQuantity { get; set; } = 1;

    [Range(0, double.MaxValue, ErrorMessage = "Giá không hợp lệ.")]
    public decimal ActualPrice { get; set; }

    [Range(1, 10)]
    public int MaxPerCustomer { get; set; } = 3;
}

public class SaleBatchWizardViewModel
{
    public int Step { get; set; } = 1;

    /// <summary>BrandManager bắt buộc khi tạo thay chi nhánh.</summary>
    public Guid? StoreId { get; set; }

    public string? Name { get; set; }

    public List<WizardOfferLine> Offers { get; set; } = new();

    /// <summary>Chọn ở bước 1 (template ids).</summary>
    public List<Guid> SelectedTemplateIds { get; set; } = new();

    [DataType(DataType.DateTime)]
    public DateTime? SaleStartTime { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? SaleEndTime { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? PickupStartTime { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? PickupEndTime { get; set; }

    /// <summary>Danh sách template tải từ API (chỉ hiển thị bước 1).</summary>
    public IReadOnlyList<OfferTemplateDto> AvailableTemplates { get; set; } = Array.Empty<OfferTemplateDto>();

    /// <summary>Chi nhánh (BrandManager chọn store).</summary>
    public IReadOnlyList<StoreDto> AvailableStores { get; set; } = Array.Empty<StoreDto>();

    public bool IsBrandManager { get; set; }
    public string? ErrorMessage { get; set; }
}
