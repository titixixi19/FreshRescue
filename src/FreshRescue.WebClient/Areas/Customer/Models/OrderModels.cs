using FluentValidation;
using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Areas.Customer.Models;

public class OrdersIndexViewModel
{
    public OrderStatus? Status { get; init; }
    public required PagedResponse<OrderDto> Orders { get; init; }
}

public class OrderDetailsViewModel
{
    public required OrderDto Order { get; init; }

    /// <summary>Hạn huỷ được hoàn 100% của đơn đã thanh toán (BR-13); null với trạng thái khác.</summary>
    public DateTime? FreeCancelDeadline { get; init; }
    public bool CanCancel { get; init; }
    public ReviewForm Review { get; init; } = new();
    public DisputeForm Dispute { get; init; } = new();

    /// <summary>Mở sẵn form khiếu nại khi vừa gửi lỗi, để người dùng thấy lỗi.</summary>
    public bool ShowDispute { get; init; }

    public static OrderDetailsViewModel Create(
        OrderDto order,
        DateTime nowUtc,
        ReviewForm? review = null,
        DisputeForm? dispute = null)
    {
        DateTime? freeCancelDeadline = null;
        if (order is { Status: OrderStatus.Paid, PaidAt: { } paidAt })
        {
            // min(paidAt + 15', pickupStart − 5') — đúng công thức API dùng (03 §4.4).
            var byPayment = paidAt.AddMinutes(15);
            var byPickup = order.PickupStartTime.AddMinutes(-5);
            freeCancelDeadline = byPayment < byPickup ? byPayment : byPickup;
        }

        return new OrderDetailsViewModel
        {
            Order = order,
            FreeCancelDeadline = freeCancelDeadline,
            CanCancel = order.Status == OrderStatus.PendingPayment
                || (order.Status == OrderStatus.Paid && nowUtc <= freeCancelDeadline),
            Review = review ?? new ReviewForm(),
            Dispute = dispute ?? new DisputeForm(),
            ShowDispute = dispute is not null,
        };
    }
}

public class OrderTicketViewModel
{
    public required OrderDto Order { get; init; }
    public required QrTicketDto Ticket { get; init; }
}

/// <summary>Đánh giá đơn đã nhận (FR-C11): sao + bình luận + ảnh tuỳ chọn.</summary>
public class ReviewForm
{
    public int Rating { get; set; } = 5;
    public string? Comment { get; set; }
    public IFormFile? Photo { get; set; }
}

/// <summary>Khiếu nại trong 2 giờ sau khi nhận (FR-C15, BR-17) — ảnh bắt buộc (03 §5).</summary>
public class DisputeForm
{
    public DisputeReason? Reason { get; set; }
    public string? Description { get; set; }
    public IFormFile? Photo { get; set; }
}

public class ReviewFormValidator : AbstractValidator<ReviewForm>
{
    public ReviewFormValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Chọn từ 1 đến 5 sao.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Bình luận tối đa 1000 ký tự.");

        RuleFor(x => x.Photo!)
            .Must(ImageRules.IsAllowed).WithMessage(ImageRules.Message)
            .When(x => x.Photo is not null);
    }
}

public class DisputeFormValidator : AbstractValidator<DisputeForm>
{
    public DisputeFormValidator()
    {
        RuleFor(x => x.Reason)
            .NotNull().WithMessage("Chọn lý do khiếu nại.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mô tả vấn đề bạn gặp.")
            .MaximumLength(1000).WithMessage("Mô tả tối đa 1000 ký tự.");

        RuleFor(x => x.Photo)
            .NotNull().WithMessage("Khiếu nại cần kèm ảnh chụp thực tế.");

        RuleFor(x => x.Photo!)
            .Must(ImageRules.IsAllowed).WithMessage(ImageRules.Message)
            .When(x => x.Photo is not null);
    }
}

/// <summary>Cùng giới hạn với <c>POST /uploads/images</c> (03 §12) — chặn trước khi gửi lên API.</summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const string Accept = ".jpg,.jpeg,.png,.webp";
    public const string Message = "Chỉ nhận ảnh .jpg, .jpeg, .png, .webp, tối đa 5 MB.";

    private static readonly string[] _extensions = [".jpg", ".jpeg", ".png", ".webp"];

    public static bool IsAllowed(IFormFile file)
        => file.Length is > 0 and <= MaxBytes
            && _extensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());
}
