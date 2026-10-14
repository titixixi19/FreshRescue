namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>Thân <c>POST /orders/{id}/payment</c> — cổng thanh toán giả có hai nút (OQ-17).</summary>
public record PayOrderRequest(PaymentOutcome Outcome);

/// <summary>Bắt đầu từ 1 để thân rỗng không lặng lẽ thành <c>Success</c>.</summary>
public enum PaymentOutcome
{
    Success = 1,
    Failure = 2,
}
