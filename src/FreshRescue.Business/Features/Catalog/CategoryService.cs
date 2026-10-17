using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Catalog;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CategoryService(
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResponse<CategoryDto>> GetAllAsync(
        GetCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Categories
            .Query()
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var categories = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<CategoryDto>
        {
            Items = _mapper.Map<List<CategoryDto>>(categories),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CategoryDto> CreateAsync(
    CreateCategoryRequest request,
    CancellationToken cancellationToken)
    {
        var category = _mapper.Map<Category>(request);

        var slugExists = await _unitOfWork.Categories
            .Query()
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.Slug == category.Slug,
                cancellationToken);

        if (slugExists)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Slug danh mục đã tồn tại.");
        }

        await _unitOfWork.Categories.AddAsync(
            category,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }
    public async Task<CategoryDto> UpdateAsync(
    Guid id,
    UpdateCategoryRequest request,
    CancellationToken cancellationToken)
    {
        // 1. Tim Category theo ID.
        // Global Query Filter tu dong loai Category da xoa mem.
        var category = await _unitOfWork.Categories
            .Query()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (category == null)
        {
            throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Khong tim thay danh muc.");
        }

        // 2. Chuan hoa du lieu.
        var newName = request.Name.Trim();
        var newSlug = request.Slug.Trim().ToLowerInvariant();

        // 3. Kiem tra slug trung voi Category khac.
        var slugExists = await _unitOfWork.Categories
            .Query()
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.Slug == newSlug && x.Id != id,
                cancellationToken);

        if (slugExists)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Slug danh muc da ton tai.");
        }

        // 4. Cap nhat Entity.
        category.Name = newName;
        category.Slug = newSlug;
        category.IconUrl = request.IconUrl;
        category.DisplayOrder = request.DisplayOrder;

        // 5. Luu vao database.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Tra ve DTO.
        return _mapper.Map<CategoryDto>(category);
    }
    public async Task DeleteAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        // 1. Tim Category chua bi xoa mem.
        var category = await _unitOfWork.Categories
            .Query()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        // 2. Khong tim thay hoac da bi xoa mem.
        if (category == null)
        {
            throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Khong tim thay danh muc.");
        }

        // 3. Yeu cau xoa Category.
        _unitOfWork.Categories.Remove(category);

        // 4. DbContext tu dong chuyen thanh Soft Delete.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
