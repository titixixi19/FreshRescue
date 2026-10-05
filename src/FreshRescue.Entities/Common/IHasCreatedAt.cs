namespace FreshRescue.Entities.Common;

/// <summary>
/// Entity chỉ cần dấu thời gian tạo — bản ghi không bao giờ sửa hoặc không cần vết sửa
/// (02-DOMAIN-MODEL.md §1.1). Gán tự động trong <c>FreshRescueDbContext.SaveChangesAsync</c>.
/// </summary>
public interface IHasCreatedAt
{
    DateTime CreatedAt { get; set; }
}
