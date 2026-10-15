using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Sales.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreshRescue.Business.Features.Sales;

public class OfferService : IOfferService
{
    private readonly IUnitOfWork _uow;
    private readonly IOrderService _orderService;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<OfferService> _logger;

    public OfferService(
        IUnitOfWork uow,
        IOrderService orderService,
        IMapper mapper,
        ICurrentUser currentUser,
        ILogger<OfferService> logger)
    {
        _uow = uow;
        _orderService = orderService;
        _mapper = mapper;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<OfferDto> AddToBatchAsync(
        Guid saleBatchId, CreateSaleBatchOfferRequest request, CancellationToken cancellationToken)
    {
        var batch = await _uow.SaleBatches.Query()
            .Include(b => b.Store)
            .Include(b => b.Offers)
            .SingleOrDefaultAsync(b => b.Id == saleBatchId, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đợt bán.");

        OfferRules.EnsureInStoreScope(_currentUser, batch.Store);
        EnsureBatchEditable(batch);

        // BR-11: mẫu phải thuộc thương hiệu của chi nhánh.
        var template = await _uow.OfferTemplates.Query()
            .SingleOrDefaultAsync(t => t.Id == request.OfferTemplateId && t.BrandId == batch.Store.BrandId, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Mẫu mặt hàng không tồn tại hoặc không thuộc thương hiệu của chi nhánh.");

        if (batch.Offers.Any(o => !o.IsDeleted && o.OfferTemplateId == template.Id))
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, $"Mẫu '{template.Name}' đã có trong đợt bán này.");
        }

        var offer = OfferRules.CreateFromTemplate(
            template, batch.Id, request.ActualPrice, request.TotalQuantity, request.MaxPerCustomer);

        await _uow.Offers.AddAsync(offer, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Thêm offer {OfferId} từ mẫu {TemplateId} vào đợt {SaleBatchId}.", offer.Id, template.Id, batch.Id);
        return _mapper.Map<OfferDto>(offer);
    }

    public async Task<OfferDto> UpdateAsync(Guid offerId, UpdateOfferRequest request, CancellationToken cancellationToken)
    {
        var offer = await LoadOfferAsync(offerId, cancellationToken);
        OfferRules.EnsureInStoreScope(_currentUser, offer.SaleBatch.Store);
        EnsureBatchEditable(offer.SaleBatch);

        if (request.ActualPrice != offer.ActualPrice)
        {
            // OQ-14: từ đơn Paid đầu tiên, giá đã nằm trong đơn và QR — không đổi nữa.
            if (await HasPaidOrdersAsync(offer.SaleBatchId, cancellationToken))
            {
                throw AppException.Conflict(ErrorCodes.BatchLocked, "Đợt bán đã có đơn thanh toán, không đổi được giá.");
            }

            // Template có thể đã bị xoá mềm sau khi tạo offer — vẫn cần biên độ brand của nó.
            var template = await _uow.OfferTemplates.Query()
                .IgnoreQueryFilters()
                .SingleAsync(t => t.Id == offer.OfferTemplateId, cancellationToken);

            OfferRules.EnsurePriceInBand(
                offer.OfferType, offer.OriginalPrice, request.ActualPrice,
                template.BrandMinDiscountPercent, template.BrandMaxDiscountPercent);

            offer.ActualPrice = request.ActualPrice;
        }

        offer.MaxPerCustomer = request.MaxPerCustomer;

        await TrySaveAsync(offerId, cancellationToken);
        return _mapper.Map<OfferDto>(offer);
    }

    public async Task<OfferDto> AdjustQuantityAsync(
        Guid offerId, AdjustOfferQuantityRequest request, CancellationToken cancellationToken)
    {
        var offer = await LoadOfferAsync(offerId, cancellationToken);
        OfferRules.EnsureInStoreScope(_currentUser, offer.SaleBatch.Store);
        EnsureBatchEditable(offer.SaleBatch);

        // Chỉ OrderService được ghi RemainingQuantity (một câu UPDATE có điều kiện). Hạ dưới số đã
        // giữ chỗ / đã bán thì nó ném 409 OFFER_HAS_ORDERS (BR-01); hạ về đúng số đã bán là ngừng bán (BR-24).
        await _orderService.AdjustOfferStockAsync(offerId, request.TotalQuantity, cancellationToken);

        // UPDATE ghi thẳng vào CSDL nên bản đang theo dõi đã cũ: nạp lại để trả số liệu mới.
        _uow.ClearChangeTracker();
        var updated = await LoadOfferAsync(offerId, cancellationToken);
        return _mapper.Map<OfferDto>(updated);
    }

    public async Task DeleteAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var offer = await LoadOfferAsync(offerId, cancellationToken);
        OfferRules.EnsureInStoreScope(_currentUser, offer.SaleBatch.Store);

        // BR-24: xoá mềm sẽ bị query filter giấu đi và đơn cũ trỏ vào khoảng không.
        if (await _uow.OrderItems.Query().AnyAsync(i => i.OfferId == offerId, cancellationToken))
        {
            throw AppException.Conflict(
                ErrorCodes.OfferHasOrders,
                "Mặt hàng đã có đơn, không xoá được. Muốn ngừng bán hãy đặt số lượng về mức đã bán.");
        }

        _uow.Offers.Remove(offer);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Xoá offer {OfferId} khỏi đợt {SaleBatchId}.", offerId, offer.SaleBatchId);
    }

    private async Task<Offer> LoadOfferAsync(Guid offerId, CancellationToken cancellationToken)
        => await _uow.Offers.Query()
            .Include(o => o.SaleBatch).ThenInclude(b => b.Store)
            .SingleOrDefaultAsync(o => o.Id == offerId, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy mặt hàng.");

    private static void EnsureBatchEditable(SaleBatch batch)
    {
        if (batch.Status is SaleBatchStatus.Closed or SaleBatchStatus.Cancelled)
        {
            throw AppException.Conflict(ErrorCodes.BatchLocked, "Đợt bán đã đóng hoặc đã huỷ, không sửa được nữa.");
        }
    }

    private Task<bool> HasPaidOrdersAsync(Guid saleBatchId, CancellationToken cancellationToken)
        => _uow.Orders.Query().AnyAsync(o => o.SaleBatchId == saleBatchId && o.PaidAt != null, cancellationToken);

    private async Task TrySaveAsync(Guid offerId, CancellationToken cancellationToken)
    {
        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _uow.ClearChangeTracker();
            throw AppException.Conflict(
                ErrorCodes.ConcurrencyConflict,
                $"Mặt hàng {offerId} vừa được thay đổi, vui lòng tải lại và thử lại.");
        }
    }
}
