using FluentValidation;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.DataAccess.Repositories;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Engagement;

/// <summary>
/// BR-17: one Dispute per Redeemed order, created <= 2h after RedeemedAt.
/// BR-21/OQ-26: approved Dispute creates a negative Refund payment for TotalAmount.
/// An approved order remains Redeemed. Only SystemAdmin can resolve disputes.
/// </summary>
public sealed class DisputeService : IDisputeService
{
    private static readonly TimeSpan _disputeWindow = TimeSpan.FromHours(2);

    private readonly IUnitOfWork _uow;
    private readonly IDisputeWorkflowRepository _workflow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly IValidator<CreateDisputeRequest> _createValidator;
    private readonly IValidator<RejectDisputeRequest> _rejectValidator;
    private readonly IValidator<MerchantNoteRequest> _merchantValidator;

    public DisputeService(
        IUnitOfWork uow,
        IDisputeWorkflowRepository workflow,
        ICurrentUser currentUser,
        TimeProvider clock,
        IValidator<CreateDisputeRequest> createValidator,
        IValidator<RejectDisputeRequest> rejectValidator,
        IValidator<MerchantNoteRequest> merchantValidator)
    {
        _uow = uow;
        _workflow = workflow;
        _currentUser = currentUser;
        _clock = clock;
        _createValidator = createValidator;
        _rejectValidator = rejectValidator;
        _merchantValidator = merchantValidator;
    }

    public async Task<DisputeDto> CreateAsync(Guid orderId, CreateDisputeRequest request, CancellationToken ct)
    {
        await ValidateAsync(_createValidator, request, ct);
        var userId = RequireRoleUser(AppRoles.Customer);
        var now = UtcNow();

        var order = await _uow.Orders.Query().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đơn hàng.");

        if (order.CustomerUserId != userId)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Bạn không sở hữu đơn hàng này.");
        }

        if (order.Status != OrderStatus.Redeemed || order.RedeemedAt is null)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Chỉ đơn đã nhận hàng mới được khiếu nại.");
        }

        if (now < order.RedeemedAt.Value || now - order.RedeemedAt.Value > _disputeWindow)
        {
            throw AppException.Conflict(ErrorCodes.DisputeWindowPassed, "Đã quá 2 giờ kể từ lúc nhận hàng.");
        }

        if (await _uow.Disputes.Query().AnyAsync(d => d.OrderId == orderId, ct))
        {
            throw AppException.Conflict(ErrorCodes.DisputeAlreadyExists, "Đơn hàng đã có khiếu nại.");
        }

        var dispute = new Dispute
        {
            OrderId = orderId,
            CustomerUserId = userId,
            Reason = request.Reason,
            Description = request.Description.Trim(),
            PhotoUrl = request.PhotoUrl.Trim(),
            Status = DisputeStatus.Open,
        };

        try
        {
            await _uow.Disputes.AddAsync(dispute, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw AppException.Conflict(ErrorCodes.DisputeAlreadyExists, "Đơn hàng đã có khiếu nại.");
        }

        return ToDto(dispute);
    }

    public async Task<PagedResponse<DisputeDto>> GetMineAsync(int page, int pageSize, CancellationToken ct)
    {
        var userId = RequireRoleUser(AppRoles.Customer);
        CheckPaging(page, pageSize);
        return await ToPageAsync(_uow.Disputes.Query()
            .AsNoTracking().Where(d => d.CustomerUserId == userId), page, pageSize, ct);
    }

    public async Task<PagedResponse<DisputeDto>> GetAdminAsync(
        DisputeStatus? status, int page, int pageSize, CancellationToken ct)
    {
        RequireRoleUser(AppRoles.SystemAdmin);
        CheckPaging(page, pageSize);
        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Trạng thái khiếu nại không hợp lệ.");
        }

        var query = _uow.Disputes.Query().AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        return await ToPageAsync(query, page, pageSize, ct);
    }

    public async Task<DisputeDto> ApproveAsync(Guid disputeId, CancellationToken ct)
    {
        var adminId = RequireRoleUser(AppRoles.SystemAdmin);
        var now = UtcNow();
        await using var tx = await _uow.BeginTransactionAsync(ct);

        var dispute = await _uow.Disputes.Query().AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == disputeId, ct)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy khiếu nại.");
        EnsureOpen(dispute);

        var order = await _uow.Orders.Query().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == dispute.OrderId, ct)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đơn hàng.");

        if (order.Status != OrderStatus.Redeemed || order.RefundStatus != RefundStatus.None || order.TotalAmount <= 0)
        {
            throw AppException.Conflict(ErrorCodes.ConcurrencyConflict, "Đơn hàng không đủ điều kiện hoàn tiền.");
        }

        // Atomically claim the dispute before refunding; concurrent approve/reject gets 0 rows.
        if (await _workflow.TryResolveAsync(disputeId, DisputeStatus.Approved, adminId, null, now, ct) != 1)
        {
            throw AppException.Conflict(ErrorCodes.ConcurrencyConflict, "Khiếu nại đã được xử lý bởi request khác.");
        }

        // BR-21: conditionally update only RefundStatus; NEVER change Order.Status.
        if (await _workflow.TryRefundOrderAsync(order.Id, now, ct) != 1)
        {
            throw AppException.Conflict(ErrorCodes.ConcurrencyConflict, "Đơn hàng đã được hoàn tiền hoặc thay đổi.");
        }

        await _uow.Payments.AddAsync(new Payment
        {
            OrderId = order.Id,
            Method = PaymentMethod.Refund,
            Amount = -order.TotalAmount,
            Status = PaymentStatus.Succeeded,
            TransactionRef = $"DISPUTE-{dispute.Id:N}",
            CreatedAt = now,
        }, ct);

        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToDto(dispute) with
        {
            Status = DisputeStatus.Approved,
            ResolvedByUserId = adminId,
            ResolvedAt = now,
        };
    }

    public async Task<DisputeDto> RejectAsync(Guid disputeId, RejectDisputeRequest request, CancellationToken ct)
    {
        await ValidateAsync(_rejectValidator, request, ct);
        var adminId = RequireRoleUser(AppRoles.SystemAdmin);
        var now = UtcNow();
        var dispute = await _uow.Disputes.Query().AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == disputeId, ct)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy khiếu nại.");
        EnsureOpen(dispute);

        var note = request.AdminNote.Trim();
        if (await _workflow.TryResolveAsync(disputeId, DisputeStatus.Rejected, adminId, note, now, ct) != 1)
        {
            throw AppException.Conflict(ErrorCodes.ConcurrencyConflict, "Khiếu nại đã được xử lý bởi request khác.");
        }

        return ToDto(dispute) with
        {
            Status = DisputeStatus.Rejected,
            AdminNote = note,
            ResolvedByUserId = adminId,
            ResolvedAt = now,
        };
    }

    public async Task<DisputeDto> SetMerchantNoteAsync(
        Guid disputeId, MerchantNoteRequest request, CancellationToken ct)
    {
        await ValidateAsync(_merchantValidator, request, ct);
        var userId = _currentUser.UserId
            ?? throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Bạn cần đăng nhập.");
        if (!_currentUser.IsInRole(AppRoles.BrandManager) && !_currentUser.IsInRole(AppRoles.StoreStaff))
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Không có quyền giải trình khiếu nại.");
        }

        var dispute = await _uow.Disputes.Query().AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == disputeId, ct)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy khiếu nại.");
        EnsureOpen(dispute);

        var order = await _uow.Orders.Query().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == dispute.OrderId, ct)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đơn hàng.");

        var allowed = false;
        if (_currentUser.IsInRole(AppRoles.StoreStaff))
        {
            allowed = await _uow.StoreStaff.Query().AnyAsync(
                ss => ss.UserId == userId && ss.StoreId == order.StoreId && ss.IsActive, ct);
        }
        if (!allowed && _currentUser.IsInRole(AppRoles.BrandManager))
        {
            allowed = await _uow.Stores.Query().AnyAsync(
                s => s.Id == order.StoreId && s.Brand.OwnerUserId == userId, ct);
        }
        if (!allowed)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Bạn không thuộc cửa hàng của đơn hàng này.");
        }

        var note = request.MerchantNote.Trim();
        if (await _workflow.TrySetMerchantNoteAsync(disputeId, note, UtcNow(), ct) != 1)
        {
            throw AppException.Conflict(ErrorCodes.ConcurrencyConflict, "Khiếu nại đã được xử lý.");
        }

        return ToDto(dispute) with { MerchantNote = note };
    }

    private static async Task<PagedResponse<DisputeDto>> ToPageAsync(
        IQueryable<Dispute> query, int page, int pageSize, CancellationToken ct)
    {
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResponse<DisputeDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = count,
        };
    }

    private static void CheckPaging(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > PagedResponse<DisputeDto>.MaxPageSize)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Phân trang không hợp lệ.");
        }
    }

    private Guid RequireRoleUser(string role)
    {
        if (!_currentUser.IsInRole(role) || _currentUser.UserId is not Guid userId)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Bạn không có quyền thực hiện thao tác này.");
        }

        return userId;
    }

    private static void EnsureOpen(Dispute dispute)
    {
        if (dispute.Status != DisputeStatus.Open)
        {
            throw AppException.Conflict(ErrorCodes.ConcurrencyConflict, "Khiếu nại đã được xử lý.");
        }
    }

    private DateTime UtcNow() => _clock.GetUtcNow().UtcDateTime;

    private static DisputeDto ToDto(Dispute d) => new(
        d.Id, d.OrderId, d.CustomerUserId, d.Reason, d.Description, d.PhotoUrl,
        d.Status, d.MerchantNote, d.AdminNote, d.ResolvedByUserId, d.ResolvedAt, d.CreatedAt);

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed,
                string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
        }
    }

    private static bool IsUniqueViolation(DbUpdateException e)
        => e.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}
