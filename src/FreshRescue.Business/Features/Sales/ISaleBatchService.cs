using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Sales.Dtos;

namespace FreshRescue.Business.Features.Sales;

public interface ISaleBatchService
{
    /// <summary>Danh sách đợt bán trong phạm vi của người gọi (FR-S02). <c>Date</c> lọc theo ngày UTC của SaleStartTime.</summary>
    Task<PagedResponse<SaleBatchSummaryDto>> ListAsync(SaleBatchListQuery query, CancellationToken cancellationToken);

    Task<SaleBatchDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Bước 1–3 của FR-S02: tạo đợt ở trạng thái Draft kèm offer chụp từ template (BR-18).</summary>
    Task<SaleBatchDto> CreateAsync(CreateSaleBatchRequest request, CancellationToken cancellationToken);

    /// <summary>Sửa tên và bốn mốc thời gian; bị khoá sau đơn Paid đầu tiên (OQ-14).</summary>
    Task<SaleBatchDto> UpdateAsync(Guid id, UpdateSaleBatchRequest request, CancellationToken cancellationToken);

    /// <summary>Bước 4 của FR-S02: Draft → Scheduled (OQ-12).</summary>
    Task<SaleBatchDto> ActivateAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Huỷ cả đợt (BR-23): chỉ Brand Manager. Huỷ mọi đơn PendingPayment / Paid trong cùng một
    /// giao dịch với việc đổi trạng thái đợt.
    /// </summary>
    Task<SaleBatchDto> CancelAsync(Guid id, CancelSaleBatchRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// FR-X01: Quét các đợt bán Scheduled đã tới SaleStartTime và chuyển sang Active.
    /// Trả về số đợt đã kích hoạt.
    /// </summary>
    Task<int> ActivateDueBatchesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// FR-X02: Quét các đợt bán Active đã tới PickupEndTime và chuyển sang Closed.
    /// Chốt TotalUnsoldQuantity và UnsoldQuantity cho từng Offer (OQ-15).
    /// Bắt buộc xử lý NoShow cho các đơn của đợt trước khi đóng đợt (01 §6.5).
    /// Không đổi trạng thái tại SaleEndTime (01 §6.5). Trả về số đợt đã đóng.
    /// </summary>
    Task<int> CloseDueBatchesAsync(CancellationToken cancellationToken);
}
