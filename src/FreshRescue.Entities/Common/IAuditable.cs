namespace FreshRescue.Entities.Common;

/// <summary>
/// Entity có dấu thời gian tạo / sửa. Gán tự động trong
/// <c>FreshRescueDbContext.SaveChangesAsync</c> — service không tự gán tay (02-DOMAIN-MODEL.md §1.1).
/// </summary>
public interface IAuditable : IHasCreatedAt
{
    DateTime? UpdatedAt { get; set; }
}
