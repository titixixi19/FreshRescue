using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Catalog;

public sealed class OfferTemplateService : IOfferTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public OfferTemplateService(IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<OfferTemplateDto>> GetAllAsync(
        GetOfferTemplatesRequest request, CancellationToken ct)
    {
        var brandId = await GetReadableBrandIdAsync(ct);
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Phân trang không hợp lệ.");
        }

        var query = _unitOfWork.OfferTemplates.Query().AsNoTracking()
            .Where(x => x.BrandId == brandId);

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }

        if (request.OfferType.HasValue)
        {
            query = query.Where(x => x.OfferType == request.OfferType.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search));
        }

        var count = await query.CountAsync(ct);
        var offset = ((long)request.Page - 1) * request.PageSize;
        var items = offset > int.MaxValue
            ? new List<OfferTemplateDto>()
            : await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
                .Skip((int)offset).Take(request.PageSize)
                .Select(x => new OfferTemplateDto
                {
                    Id = x.Id,
                    BrandId = x.BrandId,
                    CategoryId = x.CategoryId,
                    Name = x.Name,
                    Description = x.Description,
                    ImageUrl = x.ImageUrl,
                    OfferType = x.OfferType,
                    OriginalPrice = x.OriginalPrice,
                    SuggestedPrice = x.SuggestedPrice,
                    BrandMinDiscountPercent = x.BrandMinDiscountPercent,
                    BrandMaxDiscountPercent = x.BrandMaxDiscountPercent,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                }).ToListAsync(ct);

        return new PagedResponse<OfferTemplateDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = count
        };
    }

    public async Task<OfferTemplateDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var brandId = await GetReadableBrandIdAsync(ct);
        var template = await FindOwnedTemplateAsync(id, brandId, ct);
        return ToDto(template);
    }

    public async Task<OfferTemplateDto> CreateAsync(UpsertOfferTemplateRequest request, CancellationToken ct)
    {
        var brandId = await GetOwnedBrandIdAsync(ct);
        await ValidateRequestAsync(request, ct);

        var template = new OfferTemplate
        {
            BrandId = brandId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            OfferType = request.OfferType,
            OriginalPrice = request.OriginalPrice,
            SuggestedPrice = request.SuggestedPrice,
            BrandMinDiscountPercent = request.BrandMinDiscountPercent,
            BrandMaxDiscountPercent = request.BrandMaxDiscountPercent
        };

        await _unitOfWork.OfferTemplates.AddAsync(template, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(template);
    }

    public async Task<OfferTemplateDto> UpdateAsync(
        Guid id, UpsertOfferTemplateRequest request, CancellationToken ct)
    {
        var brandId = await GetOwnedBrandIdAsync(ct);
        var template = await FindOwnedTemplateAsync(id, brandId, ct);
        await ValidateRequestAsync(request, ct);

        template.CategoryId = request.CategoryId;
        template.Name = request.Name.Trim();
        template.Description = request.Description?.Trim();
        template.ImageUrl = request.ImageUrl?.Trim();
        template.OfferType = request.OfferType;
        template.OriginalPrice = request.OriginalPrice;
        template.SuggestedPrice = request.SuggestedPrice;
        template.BrandMinDiscountPercent = request.BrandMinDiscountPercent;
        template.BrandMaxDiscountPercent = request.BrandMaxDiscountPercent;

        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(template);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var brandId = await GetOwnedBrandIdAsync(ct);
        var template = await FindOwnedTemplateAsync(id, brandId, ct);
        _unitOfWork.OfferTemplates.Remove(template);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>
    /// BrandManager thấy mẫu của brand sở hữu; StoreStaff chỉ thấy mẫu của brand chứa
    /// chi nhánh họ đang được phân công. Không tin store_id đơn thuần: kiểm tra StoreStaff còn hoạt động.
    /// </summary>
    private async Task<Guid> GetReadableBrandIdAsync(CancellationToken ct)
    {
        if (_currentUser.IsInRole(AppRoles.BrandManager))
        {
            return await GetOwnedBrandIdAsync(ct);
        }

        if (_currentUser.IsInRole(AppRoles.StoreStaff)
            && _currentUser.UserId is Guid userId
            && _currentUser.StoreId is Guid storeId)
        {
            var brandId = await _unitOfWork.StoreStaff.Query().AsNoTracking()
                .Where(x => x.UserId == userId && x.StoreId == storeId && x.IsActive)
                .Select(x => (Guid?)x.Store.BrandId)
                .FirstOrDefaultAsync(ct);

            if (brandId.HasValue)
            {
                return brandId.Value;
            }
        }

        throw AppException.Forbidden(ErrorCodes.ForbiddenScope,
            "Bạn không có quyền xem mẫu hàng của thương hiệu này.");
    }

    private async Task<Guid> GetOwnedBrandIdAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId || _currentUser.BrandId is not Guid brandId)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope,
                "Bạn không có quyền truy cập mẫu hàng của thương hiệu.");
        }

        var owned = await _unitOfWork.Brands.Query().AsNoTracking()
            .AnyAsync(x => x.Id == brandId && x.OwnerUserId == userId, ct);
        if (!owned)
        {
            throw AppException.NotFound(ErrorCodes.NotFound,
                "Không tìm thấy thương hiệu do bạn quản lý.");
        }

        return brandId;
    }

    private async Task<OfferTemplate> FindOwnedTemplateAsync(Guid id, Guid brandId, CancellationToken ct)
    {
        var template = await _unitOfWork.OfferTemplates.Query()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy mẫu hàng.");

        if (template.BrandId != brandId)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope,
                "Không được thao tác trên mẫu hàng thuộc thương hiệu khác.");
        }

        return template;
    }

    private async Task ValidateRequestAsync(UpsertOfferTemplateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200
            || request.Description?.Length > 1000 || request.ImageUrl?.Length > 500
            || request.CategoryId == Guid.Empty)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Dữ liệu mẫu hàng không hợp lệ.");
        }

        OfferTemplateDiscountPolicy.Validate(request.OfferType, request.OriginalPrice,
            request.SuggestedPrice, request.BrandMinDiscountPercent, request.BrandMaxDiscountPercent);

        var categoryExists = await _unitOfWork.Categories.Query().AsNoTracking()
            .AnyAsync(x => x.Id == request.CategoryId, ct);
        if (!categoryExists)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed,
                "Danh mục không tồn tại hoặc đã bị xóa.");
        }
    }

    private static OfferTemplateDto ToDto(OfferTemplate x) => new()
    {
        Id = x.Id,
        BrandId = x.BrandId,
        CategoryId = x.CategoryId,
        Name = x.Name,
        Description = x.Description,
        ImageUrl = x.ImageUrl,
        OfferType = x.OfferType,
        OriginalPrice = x.OriginalPrice,
        SuggestedPrice = x.SuggestedPrice,
        BrandMinDiscountPercent = x.BrandMinDiscountPercent,
        BrandMaxDiscountPercent = x.BrandMaxDiscountPercent,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
