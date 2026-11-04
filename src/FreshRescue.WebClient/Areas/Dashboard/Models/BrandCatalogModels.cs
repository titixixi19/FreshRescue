namespace FreshRescue.WebClient.Areas.Dashboard.Models;

// —— Mẫu mặt hàng: form tạo / sửa (FR-B04) ——

public class OfferTemplateFormViewModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public IFormFile? Image { get; set; }
    public Guid CategoryId { get; set; }
    public string OfferType { get; set; } = "SurpriseBag";
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal BrandMinDiscountPercent { get; set; }
    public decimal BrandMaxDiscountPercent { get; set; }

    public IReadOnlyList<CategoryOption> Categories { get; set; } = Array.Empty<CategoryOption>();
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<string> FieldErrors { get; set; } = Array.Empty<string>();

    public bool IsEdit => Id.HasValue;
}

public class CategoryOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// —— Hồ sơ thương hiệu (FR-B01) ——

public class BrandProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StatusReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BrandProfileViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public IFormFile? Logo { get; set; }
    public string? Description { get; set; }

    public string? Status { get; set; }
    public string? StatusReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<string> FieldErrors { get; set; } = Array.Empty<string>();
}

// —— Danh mục (FR-A03) ——

public class AdminCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
}

public class CategoryFormViewModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<string> FieldErrors { get; set; } = Array.Empty<string>();
}

public class CategoryListViewModel
{
    public IReadOnlyList<AdminCategoryDto> Items { get; set; } = Array.Empty<AdminCategoryDto>();
    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? ErrorMessage { get; set; }
    public CategoryFormViewModel NewCategory { get; set; } = new();
}
