using FreshRescue.Business.Common;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Catalog;

/// <summary>
/// Thương hiệu chưa được duyệt chỉ được xem, không được ghi dữ liệu nghiệp vụ
/// (03-API-CONTRACT.md §2 <c>register-brand</c>, BR-14).
/// </summary>
internal static class BrandApproval
{
    public static async Task EnsureApprovedAsync(IUnitOfWork unitOfWork, Guid brandId, CancellationToken cancellationToken)
    {
        var status = await unitOfWork.Brands.Query().AsNoTracking()
            .Where(b => b.Id == brandId)
            .Select(b => (BrandStatus?)b.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (status is not null and not BrandStatus.Approved)
        {
            throw AppException.Forbidden(ErrorCodes.BrandNotApproved,
                "Thương hiệu chưa được duyệt hoặc đang bị khoá nên chưa thao tác được.");
        }
    }
}
