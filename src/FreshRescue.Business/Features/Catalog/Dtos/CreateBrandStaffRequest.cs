namespace FreshRescue.Business.Features.Catalog.Dtos;

/// <summary>
/// Yêu cầu tạo tài khoản nhân viên chi nhánh (03-API-CONTRACT.md §7).
/// </summary>
public record CreateBrandStaffRequest
{
    public string FullName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public string Password { get; init; } = default!;

    public string? PhoneNumber { get; init; }

    public Guid StoreId { get; init; }

    public string? Position { get; init; }
}
