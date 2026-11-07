using System.ComponentModel.DataAnnotations;

namespace FreshRescue.WebClient.Areas.Dashboard.Models;

// —— Brand stores (03-API-CONTRACT §7 /brand/stores) ——

public class StoreDto
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string City { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Phone { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public string Status { get; set; } = "Active";
    public decimal? RatingAverage { get; set; }
    public int RatingCount { get; set; }
}

public class CreateStoreRequest
{
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string City { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Phone { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
}

public class UpdateStoreRequest
{
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string City { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Phone { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public string Status { get; set; } = "Active";
}

public class StoreFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Tên chi nhánh bắt buộc.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ bắt buộc.")]
    [StringLength(300)]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Ward { get; set; }

    [StringLength(100)]
    public string? District { get; set; }

    [Required(ErrorMessage = "Thành phố bắt buộc.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Vĩ độ không hợp lệ.")]
    public decimal Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Kinh độ không hợp lệ.")]
    public decimal Longitude { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public string Status { get; set; } = "Active";
}

public class StoreListViewModel
{
    public IReadOnlyList<StoreDto> Items { get; set; } = Array.Empty<StoreDto>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? ErrorMessage { get; set; }
}

// —— Admin users (03 §8 /admin/users) ——

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}

public class UserListViewModel
{
    public IReadOnlyList<AdminUserDto> Items { get; set; } = Array.Empty<AdminUserDto>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? Query { get; set; }
    public string? ErrorMessage { get; set; }
}

// —— Admin brands (03 §8 /admin/brands) ——

public class AdminBrandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "PendingApproval";
    public Guid? OwnerUserId { get; set; }
    public string? OwnerEmail { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class RejectBrandRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class SuspendBrandRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class BrandListViewModel
{
    public IReadOnlyList<AdminBrandDto> Items { get; set; } = Array.Empty<AdminBrandDto>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? StatusFilter { get; set; }
    public string? ErrorMessage { get; set; }
}
