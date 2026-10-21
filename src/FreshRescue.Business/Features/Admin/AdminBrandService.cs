
using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Admin.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Admin;

public class AdminBrandService : IAdminBrandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AdminBrandService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResponse<AdminBrandDto>> GetAllAsync(
        GetAdminBrandsRequest request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Brands
            .Query()
            .AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(
                x => x.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var brands = await query
            .Include(x => x.Owner)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var brandDtos = _mapper.Map<List<AdminBrandDto>>(
            brands);

        return new PagedResponse<AdminBrandDto>
        {
            Items = brandDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AdminBrandDto> ApproveAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        var adminId = _currentUser.UserId;

        if (!adminId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Không xác định được tài khoản quản trị viên.");
        }

        var brand = await _unitOfWork.Brands
            .Query()
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(
                x => x.Id == brandId,
                cancellationToken) ?? throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Không tìm thấy thương hiệu.");
        if (brand.Status != BrandStatus.PendingApproval)
        {
            throw AppException.Conflict(
                ErrorCodes.ValidationFailed,
                "Chỉ có thể phê duyệt thương hiệu đang chờ phê duyệt.");
        }

        brand.Status = BrandStatus.Approved;
        brand.ApprovedAt = _timeProvider.GetUtcNow().UtcDateTime;
        brand.ApprovedByUserId = adminId.Value;
        brand.StatusReason = null;

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return _mapper.Map<AdminBrandDto>(brand);
    }

    public async Task<AdminBrandDto> RejectAsync(
        Guid brandId,
        RejectBrandRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = _currentUser.UserId;

        if (!adminId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Không xác định được tài khoản quản trị viên.");
        }

        var reason = request.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) ||
            reason.Length > 500)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Lý do từ chối phải có từ 1 đến 500 ký tự.");
        }

        var brand = await _unitOfWork.Brands
            .Query()
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(
                x => x.Id == brandId,
                cancellationToken) ?? throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Không tìm thấy thương hiệu.");
        if (brand.Status != BrandStatus.PendingApproval)
        {
            throw AppException.Conflict(
                ErrorCodes.ValidationFailed,
                "Chỉ có thể từ chối thương hiệu đang chờ phê duyệt.");
        }

        brand.Status = BrandStatus.Rejected;
        brand.StatusReason = reason;

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return _mapper.Map<AdminBrandDto>(brand);
    }

    public async Task<AdminBrandDto> SuspendAsync(
        Guid brandId,
        SuspendBrandRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = _currentUser.UserId;

        if (!adminId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Không xác định được tài khoản quản trị viên.");
        }

        var reason = request.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) ||
            reason.Length > 500)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Lý do tạm khóa phải có từ 1 đến 500 ký tự.");
        }

        var brand = await _unitOfWork.Brands
            .Query()
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(
                x => x.Id == brandId,
                cancellationToken) ?? throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Không tìm thấy thương hiệu.");
        if (brand.Status != BrandStatus.Approved)
        {
            throw AppException.Conflict(
                ErrorCodes.ValidationFailed,
                "Chỉ có thể tạm khóa thương hiệu đã được phê duyệt.");
        }

        brand.Status = BrandStatus.Suspended;
        brand.StatusReason = reason;

        // Giữ nguyên thông tin phê duyệt để phục vụ lịch sử kiểm tra.

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return _mapper.Map<AdminBrandDto>(brand);
    }
}
