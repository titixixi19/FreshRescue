using System;
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
        // 1. Lay danh sach thuong hieu.
        var query = _unitOfWork.Brands
            .Query()
            .AsNoTracking()
            .Include(x => x.Owner);

        // 2. Loc theo trang thai neu co.
        if (request.Status.HasValue)
        {
            query = query
                .Where(x => x.Status == request.Status.Value)
                .Include(x => x.Owner);
        }

        // 3. Dem tong so ban ghi sau khi loc.
        var totalCount = await query.CountAsync(
            cancellationToken);

        // 4. Sap xep va phan trang.
        var brands = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // 5. Mapping sang DTO.
        var brandDtos = _mapper
            .Map<List<AdminBrandDto>>(brands);

        // 6. Tra ve ket qua phan trang.
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
        // 1. Lay thong tin Admin dang dang nhap.
        var adminId = _currentUser.UserId;

        if (!adminId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Khong xac dinh duoc tai khoan Admin.");
        }

        // 2. Tim Brand theo ID.
        var brand = await _unitOfWork.Brands
            .Query()
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(
                x => x.Id == brandId,
                cancellationToken);

        if (brand == null)
        {
            throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Khong tim thay thuong hieu.");
        }

        // 3. Chi duyet Brand dang cho phe duyet.
        if (brand.Status != BrandStatus.PendingApproval)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Chi co the duyet thuong hieu dang cho phe duyet.");
        }

        // 4. Chuyen trang thai sang Approved.
        brand.Status = BrandStatus.Approved;

        // 5. Luu dau vet nguoi duyet.
        brand.ApprovedAt =
            _timeProvider.GetUtcNow().UtcDateTime;

        brand.ApprovedByUserId = adminId.Value;

        // Xoa ly do trang thai cu.
        brand.StatusReason = null;

        // 6. Luu database.
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        // 7. Tra ve DTO.
        return _mapper.Map<AdminBrandDto>(brand);
    }
    public async Task<AdminBrandDto> RejectAsync(
    Guid brandId,
    RejectBrandRequest request,
    CancellationToken cancellationToken)
    {
        // 1. Xac dinh Admin dang thuc hien.
        var adminId = _currentUser.UserId;

        if (!adminId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Khong xac dinh duoc tai khoan Admin.");
        }

        // 2. Kiem tra ly do tu choi.
        var reason = request.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) ||
            reason.Length > 500)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Ly do tu choi phai co tu 1 den 500 ky tu.");
        }

        // 3. Tim Brand theo ID.
        var brand = await _unitOfWork.Brands
            .Query()
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(
                x => x.Id == brandId,
                cancellationToken);

        if (brand == null)
        {
            throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Khong tim thay thuong hieu.");
        }

        // 4. Chi tu choi Brand dang cho duyet.
        if (brand.Status != BrandStatus.PendingApproval)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Chi co the tu choi thuong hieu dang cho phe duyet.");
        }

        // 5. Chuyen trang thai va luu ly do.
        brand.Status = BrandStatus.Rejected;
        brand.StatusReason = reason;

        // Brand chua duoc duyet nen khong gan
        // ApprovedAt hoac ApprovedByUserId.

        // 6. Luu thay doi vao database.
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        // 7. Tra ve DTO.
        return _mapper.Map<AdminBrandDto>(brand);
    }
    public async Task<AdminBrandDto> SuspendAsync(
    Guid brandId,
    SuspendBrandRequest request,
    CancellationToken cancellationToken)
    {
        // 1. Xac dinh Admin dang thuc hien.
        var adminId = _currentUser.UserId;

        if (!adminId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Khong xac dinh duoc tai khoan Admin.");
        }

        // 2. Kiem tra ly do tam khoa.
        var reason = request.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) ||
            reason.Length > 500)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Ly do tam khoa phai co tu 1 den 500 ky tu.");
        }

        // 3. Tim Brand trong database.
        var brand = await _unitOfWork.Brands
            .Query()
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(
                x => x.Id == brandId,
                cancellationToken);

        if (brand == null)
        {
            throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Khong tim thay thuong hieu.");
        }

        // 4. Chi duoc tam khoa Brand da Approved.
        if (brand.Status != BrandStatus.Approved)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Chi co the tam khoa thuong hieu da duoc phe duyet.");
        }

        // 5. Cap nhat trang thai va ly do.
        brand.Status = BrandStatus.Suspended;
        brand.StatusReason = reason;

        // Giu nguyen ApprovedAt va ApprovedByUserId
        // de bao toan thong tin phe duyet truoc do.

        // 6. Luu thay doi.
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        // 7. Tra ve DTO.
        return _mapper.Map<AdminBrandDto>(brand);
    }
}
