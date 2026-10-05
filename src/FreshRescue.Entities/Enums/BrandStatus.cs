namespace FreshRescue.Entities.Enums;

/// <summary>Chỉ <see cref="Approved"/> mới được mở đợt bán (BR-14).</summary>
public enum BrandStatus : byte
{
    PendingApproval = 0,
    Approved = 1,
    Rejected = 2,
    Suspended = 3,
}
