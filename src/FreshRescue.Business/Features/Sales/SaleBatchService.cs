using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Sales.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreshRescue.Business.Features.Sales;

public class SaleBatchService : ISaleBatchService
{
    private readonly IUnitOfWork _uow;
    private readonly IOrderService _orderService;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly ILogger<SaleBatchService> _logger;

    public SaleBatchService(
        IUnitOfWork uow,
        IOrderService orderService,
        IMapper mapper,
        ICurrentUser currentUser,
        TimeProvider clock,
        ILogger<SaleBatchService> logger)
    {
        _uow = uow;
        _orderService = orderService;
        _mapper = mapper;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public Task<PagedResponse<SaleBatchSummaryDto>> ListAsync(
        SaleBatchListQuery query, CancellationToken cancellationToken)
    {
        // Người gọi có thể mang cả hai role (OQ-03): lấy hợp của hai phạm vi.
        Guid? storeId = _currentUser.IsInRole(AppRoles.StoreStaff) ? _currentUser.StoreId : null;
        Guid? brandId = _currentUser.IsInRole(AppRoles.BrandManager) ? _currentUser.BrandId : null;

        var batches = _uow.SaleBatches.Query()
            .Where(b => (storeId != null && b.StoreId == storeId) || (brandId != null && b.Store.BrandId == brandId));

        if (query.Status is { } status)
        {
            batches = batches.Where(b => b.Status == status);
        }

        if (query.Date is { } date)
        {
            var from = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var to = from.AddDays(1);
            batches = batches.Where(b => b.SaleStartTime >= from && b.SaleStartTime < to);
        }

        return batches
            .OrderByDescending(b => b.SaleStartTime)
            .ThenBy(b => b.Id)
            .ToPagedResponseAsync<SaleBatch, SaleBatchSummaryDto>(
                query.Page, query.PageSize, _mapper.ConfigurationProvider, cancellationToken);
    }

    public async Task<SaleBatchDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var batch = await LoadBatchAsync(id, cancellationToken);
        OfferRules.EnsureInStoreScope(_currentUser, batch.Store);
        return _mapper.Map<SaleBatchDto>(batch);
    }

    public async Task<SaleBatchDto> CreateAsync(CreateSaleBatchRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("POST /staff/sale-batches phải yêu cầu đăng nhập.");

        var store = await ResolveStoreAsync(request.StoreId, cancellationToken);
        OfferRules.EnsureInStoreScope(_currentUser, store);

        // BR-14
        if (store.Brand.Status != BrandStatus.Approved)
        {
            throw AppException.Forbidden(ErrorCodes.BrandNotApproved, "Thương hiệu chưa được duyệt nên chưa mở được đợt bán.");
        }

        var saleStart = OfferRules.ToUtc(request.SaleStartTime);
        var saleEnd = OfferRules.ToUtc(request.SaleEndTime);
        var pickupStart = OfferRules.ToUtc(request.PickupStartTime);
        var pickupEnd = OfferRules.ToUtc(request.PickupEndTime);

        // BR-15
        OfferRules.EnsureTimesValid(saleStart, saleEnd, pickupStart, pickupEnd);

        // OQ-11
        await EnsureNoOverlapAsync(store.Id, excludeBatchId: null, saleStart, saleEnd, cancellationToken);

        var templateIds = request.Offers.Select(o => o.OfferTemplateId).ToList();
        var templates = await _uow.OfferTemplates.Query()
            .Where(t => templateIds.Contains(t.Id) && t.BrandId == store.BrandId)
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        if (templates.Count != templateIds.Count)
        {
            throw AppException.NotFound(ErrorCodes.NotFound, "Có mẫu mặt hàng không tồn tại hoặc không thuộc thương hiệu của chi nhánh.");
        }

        var batch = new SaleBatch
        {
            StoreId = store.Id,
            Name = request.Name?.Trim(),
            Status = SaleBatchStatus.Draft,
            SaleStartTime = saleStart,
            SaleEndTime = saleEnd,
            PickupStartTime = pickupStart,
            PickupEndTime = pickupEnd,
            CreatedByUserId = userId,
            Store = store,
        };

        foreach (var line in request.Offers)
        {
            // BR-07 + BR-18 nằm trong CreateFromTemplate.
            var offer = OfferRules.CreateFromTemplate(
                templates[line.OfferTemplateId], batch.Id, line.ActualPrice, line.TotalQuantity, line.MaxPerCustomer);
            batch.Offers.Add(offer);
        }

        await _uow.SaleBatches.AddAsync(batch, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tạo đợt bán {SaleBatchId} cho chi nhánh {StoreId}, {OfferCount} offer, bởi {UserId}.",
            batch.Id, store.Id, batch.Offers.Count, userId);

        return _mapper.Map<SaleBatchDto>(batch);
    }

    public async Task<SaleBatchDto> UpdateAsync(Guid id, UpdateSaleBatchRequest request, CancellationToken cancellationToken)
    {
        var batch = await LoadBatchAsync(id, cancellationToken);
        OfferRules.EnsureInStoreScope(_currentUser, batch.Store);

        if (batch.Status is SaleBatchStatus.Closed or SaleBatchStatus.Cancelled)
        {
            throw AppException.Conflict(ErrorCodes.BatchLocked, "Đợt bán đã đóng hoặc đã huỷ, không sửa được nữa.");
        }

        var saleStart = OfferRules.ToUtc(request.SaleStartTime);
        var saleEnd = OfferRules.ToUtc(request.SaleEndTime);
        var pickupStart = OfferRules.ToUtc(request.PickupStartTime);
        var pickupEnd = OfferRules.ToUtc(request.PickupEndTime);

        var timesChanged = saleStart != batch.SaleStartTime || saleEnd != batch.SaleEndTime
            || pickupStart != batch.PickupStartTime || pickupEnd != batch.PickupEndTime;

        if (timesChanged)
        {
            // OQ-14: từ đơn Paid đầu tiên, không đổi mốc nào — hạn dùng đã nằm trong token QR đã ký (01 §9.4).
            if (await HasPaidOrdersAsync(batch.Id, cancellationToken))
            {
                throw AppException.Conflict(ErrorCodes.BatchLocked, "Đợt bán đã có đơn thanh toán, không đổi được các mốc thời gian.");
            }

            OfferRules.EnsureTimesValid(saleStart, saleEnd, pickupStart, pickupEnd);
            await EnsureNoOverlapAsync(batch.StoreId, batch.Id, saleStart, saleEnd, cancellationToken);

            batch.SaleStartTime = saleStart;
            batch.SaleEndTime = saleEnd;
            batch.PickupStartTime = pickupStart;
            batch.PickupEndTime = pickupEnd;
        }

        batch.Name = request.Name?.Trim();

        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<SaleBatchDto>(batch);
    }

    public async Task<SaleBatchDto> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var batch = await LoadBatchAsync(id, cancellationToken);
        OfferRules.EnsureInStoreScope(_currentUser, batch.Store);

        if (batch.Status != SaleBatchStatus.Draft)
        {
            throw AppException.Conflict(ErrorCodes.BatchLocked, $"Chỉ kích hoạt được đợt ở trạng thái Draft (hiện là {batch.Status}).");
        }

        // BR-14: trạng thái thương hiệu có thể đã đổi từ lúc tạo nháp.
        var brand = await _uow.Brands.GetByIdAsync(batch.Store.BrandId, cancellationToken);
        if (brand is null || brand.Status != BrandStatus.Approved)
        {
            throw AppException.Forbidden(ErrorCodes.BrandNotApproved, "Thương hiệu chưa được duyệt nên chưa kích hoạt được đợt bán.");
        }

        if (!batch.Offers.Any(o => !o.IsDeleted))
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Đợt bán chưa có mặt hàng nào.");
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        if (batch.SaleEndTime <= now)
        {
            throw AppException.BadRequest(ErrorCodes.BatchTimeInvalid, "Khung giờ bán đã qua, hãy sửa lại các mốc thời gian.");
        }

        OfferRules.EnsureTimesValid(batch.SaleStartTime, batch.SaleEndTime, batch.PickupStartTime, batch.PickupEndTime);
        await EnsureNoOverlapAsync(batch.StoreId, batch.Id, batch.SaleStartTime, batch.SaleEndTime, cancellationToken);

        // ActivatedAt do job FR-X01 ghi lúc đợt thật sự mở (Scheduled → Active), không ghi ở đây.
        batch.Status = SaleBatchStatus.Scheduled;
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Kích hoạt đợt bán {SaleBatchId}: Draft → Scheduled.", batch.Id);
        return _mapper.Map<SaleBatchDto>(batch);
    }

    public async Task<SaleBatchDto> CancelAsync(Guid id, CancelSaleBatchRequest request, CancellationToken cancellationToken)
    {
        // BR-23: chỉ Brand Manager. Kiểm ở service để mọi đường gọi tới đều bị chặn.
        if (!_currentUser.IsInRole(AppRoles.BrandManager))
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Chỉ Brand Manager được huỷ cả đợt bán.");
        }

        var batch = await LoadBatchAsync(id, cancellationToken);
        OfferRules.EnsureInStoreScope(_currentUser, batch.Store);

        if (batch.Status is SaleBatchStatus.Closed or SaleBatchStatus.Cancelled)
        {
            throw AppException.Conflict(ErrorCodes.BatchLocked, $"Đợt bán đang ở trạng thái {batch.Status}, không huỷ được.");
        }

        var reason = request.Reason.Trim();

        // CancelBatchOrdersAsync không tự mở giao dịch (IOrderService): trạng thái đợt và các đơn
        // phải cùng thành công hoặc cùng thất bại.
        await using var transaction = await _uow.BeginTransactionAsync(cancellationToken);

        var cancelledOrders = await _orderService.CancelBatchOrdersAsync(batch.Id, reason, cancellationToken);

        batch.Status = SaleBatchStatus.Cancelled;
        batch.CancelReason = reason;
        await _uow.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Huỷ cả đợt bán {SaleBatchId}: {CancelledOrders} đơn bị huỷ, bởi {UserId}.",
            batch.Id, cancelledOrders, _currentUser.UserId);

        return _mapper.Map<SaleBatchDto>(batch);
    }

    private async Task<SaleBatch> LoadBatchAsync(Guid id, CancellationToken cancellationToken)
        => await _uow.SaleBatches.Query()
            .Include(b => b.Store)
            .Include(b => b.Offers)
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đợt bán.");

    /// <summary>Staff lấy chi nhánh từ token; Brand Manager bắt buộc chỉ rõ chi nhánh (BR-11).</summary>
    private async Task<Store> ResolveStoreAsync(Guid? requestedStoreId, CancellationToken cancellationToken)
    {
        var storeId = _currentUser.IsInRole(AppRoles.StoreStaff) && _currentUser.StoreId.HasValue
            ? _currentUser.StoreId.Value
            : requestedStoreId
                ?? throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Brand Manager phải chỉ rõ storeId của chi nhánh mở đợt bán.");

        var store = await _uow.Stores.Query()
            .Include(s => s.Brand)
            .SingleOrDefaultAsync(s => s.Id == storeId, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy chi nhánh.");

        if (store.Status != StoreStatus.Active)
        {
            throw AppException.Conflict(ErrorCodes.BatchLocked, $"Chi nhánh '{store.Name}' đang tạm ngưng hoạt động.");
        }

        return store;
    }

    // Còn một khe race: hai request cùng chi nhánh qua kiểm tra này đồng thời. Chấp nhận được vì
    // đợt bán do người tạo bằng tay, mỗi chi nhánh vài lần một ngày.
    private async Task EnsureNoOverlapAsync(
        Guid storeId, Guid? excludeBatchId, DateTime saleStart, DateTime saleEnd, CancellationToken cancellationToken)
    {
        SaleBatch? overlapping = await _uow.SaleBatches.Query()
            .AsNoTracking()
            .Where(b => b.StoreId == storeId
                && b.Id != excludeBatchId
                && b.Status != SaleBatchStatus.Cancelled
                && b.SaleStartTime < saleEnd
                && saleStart < b.SaleEndTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (overlapping is not null)
        {
            throw AppException.Conflict(
                ErrorCodes.BatchOverlap,
                $"Trùng khung giờ bán với đợt '{overlapping.Name ?? "(không tên)"}' "
                + $"({overlapping.SaleStartTime:HH:mm}–{overlapping.SaleEndTime:HH:mm} UTC).");
        }
    }

    private Task<bool> HasPaidOrdersAsync(Guid saleBatchId, CancellationToken cancellationToken)
        => _uow.Orders.Query().AnyAsync(o => o.SaleBatchId == saleBatchId && o.PaidAt != null, cancellationToken);

    /// <summary>
    /// FR-X01: Quét các đợt bán Scheduled đã tới SaleStartTime và chuyển sang Active.
    /// </summary>
    public async Task<int> ActivateDueBatchesAsync(CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var dueBatches = await _uow.SaleBatches.Query()
            .Where(b => b.Status == SaleBatchStatus.Scheduled && b.SaleStartTime <= now)
            .ToListAsync(cancellationToken);

        if (dueBatches.Count == 0)
        {
            return 0;
        }

        foreach (var batch in dueBatches)
        {
            batch.Status = SaleBatchStatus.Active;
            batch.ActivatedAt = now;
            batch.UpdatedAt = now;

            _logger.LogInformation(
                "Kích hoạt đợt bán {SaleBatchId} ('{BatchName}') tại {Now}. SaleStartTime={SaleStartTime}",
                batch.Id, batch.Name, now, batch.SaleStartTime);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã kích hoạt {Count} đợt bán sang Active (FR-X01).", dueBatches.Count);
        return dueBatches.Count;
    }

    /// <summary>
    /// FR-X02: Quét các đợt bán Active đã tới PickupEndTime và chuyển sang Closed.
    /// Lưu ý: Không đổi trạng thái tại SaleEndTime (01 §6.5).
    /// Trước khi đóng đợt, bắt buộc chạy đánh dấu NoShow cho các đơn của đợt (01 §6.5).
    /// Chốt TotalUnsoldQuantity và UnsoldQuantity cho từng Offer (OQ-15).
    /// </summary>
    public async Task<int> CloseDueBatchesAsync(CancellationToken cancellationToken)
    {
        // 01-REQUIREMENTS.md §6.5: FR-X04 (đánh dấu NoShow) phải chạy trước khi đóng đợt
        await _orderService.ProcessNoShowsAsync(cancellationToken);

        var now = _clock.GetUtcNow().UtcDateTime;

        var dueBatches = await _uow.SaleBatches.Query()
            .Include(b => b.Offers)
            .Where(b => b.Status == SaleBatchStatus.Active && b.PickupEndTime <= now)
            .ToListAsync(cancellationToken);

        if (dueBatches.Count == 0)
        {
            return 0;
        }

        foreach (var batch in dueBatches)
        {
            batch.Status = SaleBatchStatus.Closed;
            batch.ClosedAt = now;
            batch.UpdatedAt = now;

            var totalUnsold = 0;
            foreach (var offer in batch.Offers)
            {
                offer.UnsoldQuantity = offer.RemainingQuantity;
                offer.UpdatedAt = now;
                totalUnsold += offer.RemainingQuantity;
            }

            batch.TotalUnsoldQuantity = totalUnsold;

            _logger.LogInformation(
                "Đóng đợt bán {SaleBatchId} ('{BatchName}') tại {Now}. PickupEndTime={PickupEndTime}, TotalUnsoldQuantity={TotalUnsold}",
                batch.Id, batch.Name, now, batch.PickupEndTime, totalUnsold);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã đóng {Count} đợt bán sang Closed (FR-X02).", dueBatches.Count);
        return dueBatches.Count;
    }
}
