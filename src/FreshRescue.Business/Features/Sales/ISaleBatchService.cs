namespace FreshRescue.Business.Features.Sales;

public interface ISaleBatchService
{
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
