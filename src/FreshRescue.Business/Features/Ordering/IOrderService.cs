using FreshRescue.Business.Features.Ordering.Dtos;

namespace FreshRescue.Business.Features.Ordering;

public interface IOrderService
{
    /// <summary>Đặt hàng và giữ chỗ tồn kho ngay (BR-04).</summary>
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);
}
