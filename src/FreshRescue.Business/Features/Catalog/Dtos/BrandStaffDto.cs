namespace FreshRescue.Business.Features.Catalog.Dtos;

/// <summary>
/// Thông tin nhân viên chi nhánh trả về cho Brand Manager (03-API-CONTRACT.md §7).
/// </summary>
public record BrandStaffDto
{
    /// <summary>Id của bản ghi StoreStaff (dùng cho DELETE).</summary>
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string FullName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public Guid StoreId { get; init; }

    public string StoreName { get; init; } = default!;

    public string? Position { get; init; }

    public bool IsActive { get; init; }
}
