using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Helpers;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Đợt bán Staff/Brand — FR-S02, wizard 4 bước (01-REQUIREMENTS §5.2).
/// Paths: staff/sale-batches (03-API-CONTRACT §6).
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.BrandManager + "," + AppRoles.StoreStaff)]
public class SaleBatchesController : Controller
{
    private readonly IApiClient _api;

    public SaleBatchesController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    [ActionName("Index")]
    public async Task<IActionResult> IndexAsync(
        string? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        var vm = new SaleBatchListViewModel { StatusFilter = status, Page = page };

        try
        {
            var query = new Dictionary<string, string?>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = "20"
            };
            if (!string.IsNullOrWhiteSpace(status))
            {
                query["status"] = status.Trim();
            }

            var path = QueryHelpers.AddQueryString("staff/sale-batches", query);
            var data = await _api.GetAsync<PagedResponse<SaleBatchDto>>(path, cancellationToken);
            vm.Items = data.Items;
            vm.Page = data.Page;
            vm.PageSize = data.PageSize;
            vm.TotalCount = data.TotalCount;
            vm.TotalPages = data.TotalPages;
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = FormatApiError(ex);
        }

        return View(vm);
    }

    // ——— Wizard step 1: chọn OfferTemplate ———

    [HttpGet]
    [ActionName("CreateStep1")]
    public async Task<IActionResult> CreateStep1Async(CancellationToken cancellationToken = default)
    {
        var vm = new SaleBatchWizardViewModel
        {
            Step = 1,
            IsBrandManager = User.IsInRole(AppRoles.BrandManager)
        };

        try
        {
            // 1. Tải danh sách chi nhánh (Stores) phục vụ cho BrandManager theo contract §6.1
            if (vm.IsBrandManager)
            {
                var storesData = await _api.GetAsync<PagedResponse<StoreDto>>("brand/stores", cancellationToken);
                vm.AvailableStores = storesData?.Items ?? new List<StoreDto>();
            }

            // 2. Tải danh sách mẫu mặt hàng (Offer Templates)
            var templatesData = await _api.GetAsync<PagedResponse<OfferTemplateDto>>("brand/offer-templates?page=1&pageSize=100", cancellationToken);
            vm.AvailableTemplates = templatesData?.Items ?? new List<OfferTemplateDto>();
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Không thể tải dữ liệu ban đầu từ hệ thống." : ex.Message;
        }

        return View(vm);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("CreateStep1")]
    public async Task<IActionResult> CreateStep1PostAsync(
        SaleBatchWizardViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.IsBrandManager = User.IsInRole(AppRoles.BrandManager);
        model.Step = 1;

        if (model.IsBrandManager &&
            (!model.StoreId.HasValue || model.StoreId.Value == Guid.Empty))
        {
            ModelState.AddModelError(
                nameof(model.StoreId),
                "Vui lòng chọn chi nhánh.");
        }

        if (model.SelectedTemplateIds is null ||
            model.SelectedTemplateIds.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Vui lòng chọn ít nhất một mẫu mặt hàng.");
        }

        if (!ModelState.IsValid)
        {
            await LoadStep1LookupsAsync(model, cancellationToken);
            return View("CreateStep1", model);
        }

        // Không tin hidden input AvailableTemplates[*] — tải lại từ API và đối chiếu ID.
        IReadOnlyList<OfferTemplateDto> apiTemplates;
        try
        {
            apiTemplates = await LoadAllTemplatesAsync(cancellationToken);
        }
        catch (ApiException ex)
        {
            model.ErrorMessage = FormatApiError(ex);
            await LoadStep1LookupsAsync(model, cancellationToken);
            return View("CreateStep1", model);
        }

        // Form không chọn ô nào thì model binder để null.
        var selectedIds = (model.SelectedTemplateIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        var selectedTemplates = apiTemplates
            .Where(t => selectedIds.Contains(t.Id))
            .ToList();

        if (selectedTemplates.Count != selectedIds.Count)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không tìm thấy đầy đủ các mẫu đã chọn. Vui lòng tải lại trang và chọn lại.");
            await LoadStep1LookupsAsync(model, cancellationToken);
            return View("CreateStep1", model);
        }

        // Tạo Offers từ dữ liệu API (nguồn đáng tin cậy).
        model.Offers = selectedTemplates.Select(t => new WizardOfferLine
        {
            OfferTemplateId = t.Id,
            TemplateName = t.Name,
            OriginalPrice = t.OriginalPrice,
            SuggestedPrice = t.SuggestedPrice,
            ActualPrice = t.SuggestedPrice,
            TotalQuantity = 1,
            MaxPerCustomer = 3
        }).ToList();

        model.Step = 2;
        // Trả View trực tiếp — CreateStep2 chỉ có [HttpPost], không Redirect GET.
        return View("CreateStep2", model);
    }

    // ——— Step 2: số lượng + giá ———

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("CreateStep2")]
    public async Task<IActionResult> CreateStep2Async(
        SaleBatchWizardViewModel model,
        string nav,
        CancellationToken cancellationToken = default)
    {
        model.IsBrandManager = User.IsInRole(AppRoles.BrandManager);

        if (string.Equals(nav, "back", StringComparison.OrdinalIgnoreCase))
        {
            model.Step = 1;
            // Khôi phục selection từ Offers; giữ StoreId (hidden). Tải lookups để hiển thị.
            model.SelectedTemplateIds = (model.Offers ?? new List<WizardOfferLine>())
                .Select(o => o.OfferTemplateId)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            await LoadStep1LookupsAsync(model, cancellationToken);
            return View("CreateStep1", model);
        }

        if (model.Offers is null || model.Offers.Count == 0)
        {
            TempData["Error"] = "Thiếu danh sách offer. Bắt đầu lại từ bước 1.";
            return RedirectToAction("CreateStep1"); // Gọi trực tiếp theo tên Action được định tuyến
        }

        for (var i = 0; i < model.Offers.Count; i++)
        {
            if (model.Offers[i].TotalQuantity < 1)
            {
                ModelState.AddModelError($"Offers[{i}].TotalQuantity", "Số lượng phải ≥ 1.");
            }

            if (model.Offers[i].ActualPrice < 0)
            {
                ModelState.AddModelError($"Offers[{i}].ActualPrice", "Giá không hợp lệ.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.Step = 2;
            return View(model);
        }

        model.Step = 3;
        return View("CreateStep3", model);
    }

    // ——— Step 3: 4 mốc thời gian ———

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("CreateStep3")]
    public IActionResult CreateStep3Async(SaleBatchWizardViewModel model, string nav)
    {
        model.IsBrandManager = User.IsInRole(AppRoles.BrandManager);

        if (string.Equals(nav, "back", StringComparison.OrdinalIgnoreCase))
        {
            model.Step = 2;
            return View("CreateStep2", model);
        }

        if (model.SaleStartTime is null || model.SaleEndTime is null
            || model.PickupStartTime is null || model.PickupEndTime is null)
        {
            ModelState.AddModelError(string.Empty, "Đủ 4 mốc thời gian là bắt buộc.");
        }
        else
        {
            // BR-15 (client-side soft check; API vẫn validate)
            var ss = model.SaleStartTime.Value;
            var se = model.SaleEndTime.Value;
            var ps = model.PickupStartTime.Value;
            var pe = model.PickupEndTime.Value;
            if (!(ss < se && se <= pe && ps < pe && ss <= ps))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Khung giờ không hợp lệ (BR-15): SaleStart < SaleEnd ≤ PickupEnd; PickupStart < PickupEnd; SaleStart ≤ PickupStart.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.Step = 3;
            return View(model);
        }

        model.Step = 4;
        return View("CreateStep4", model);
    }

    // ——— Step 4: tạo Draft (+ tùy chọn kích hoạt) ———

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("CreateStep4")]
    public async Task<IActionResult> CreateStep4Async(
        SaleBatchWizardViewModel model,
        string nav,
        bool activateAfterCreate,
        CancellationToken cancellationToken = default)
    {
        model.IsBrandManager = User.IsInRole(AppRoles.BrandManager);

        if (string.Equals(nav, "back", StringComparison.OrdinalIgnoreCase))
        {
            model.Step = 3;
            return View("CreateStep3", model);
        }

        if (model.Offers is null || model.Offers.Count == 0
    || model.SaleStartTime is null || model.SaleEndTime is null
    || model.PickupStartTime is null || model.PickupEndTime is null)
        {
            TempData["Error"] = "Dữ liệu wizard không đủ. Bắt đầu lại.";
            return RedirectToAction("CreateStep1"); // Gọi trực tiếp theo tên Action được định tuyến
        }

        // BR-15 trên giờ tường VN (cùng ý nghĩa người dùng vừa nhập ở bước 3).
        var ssVn = model.SaleStartTime.Value;
        var seVn = model.SaleEndTime.Value;
        var psVn = model.PickupStartTime.Value;
        var peVn = model.PickupEndTime.Value;
        if (!(ssVn < seVn && seVn <= peVn && psVn < peVn && ssVn <= psVn))
        {
            model.Step = 4;
            model.ErrorMessage =
                "Khung giờ không hợp lệ (BR-15): SaleStart < SaleEnd ≤ PickupEnd; PickupStart < PickupEnd; SaleStart ≤ PickupStart.";
            return View(model);
        }

        // datetime-local = giờ VN → UTC đúng nghĩa trước khi gửi API.
        DateTime saleStartUtc;
        DateTime saleEndUtc;
        DateTime pickupStartUtc;
        DateTime pickupEndUtc;
        try
        {
            saleStartUtc = Display.FromVietnamToUtc(ssVn);
            saleEndUtc = Display.FromVietnamToUtc(seVn);
            pickupStartUtc = Display.FromVietnamToUtc(psVn);
            pickupEndUtc = Display.FromVietnamToUtc(peVn);
        }
        catch (ArgumentException)
        {
            model.Step = 4;
            model.ErrorMessage = "Thời gian nằm ngoài phạm vi hợp lệ của múi giờ Việt Nam.";
            return View(model);
        }

        var request = new CreateSaleBatchRequest
        {
            Name = string.IsNullOrWhiteSpace(model.Name) ? null : model.Name.Trim(),
            StoreId = model.IsBrandManager ? model.StoreId : null,
            SaleStartTime = saleStartUtc,
            SaleEndTime = saleEndUtc,
            PickupStartTime = pickupStartUtc,
            PickupEndTime = pickupEndUtc,
            Offers = model.Offers.Select(o => new CreateSaleBatchOfferRequest
            {
                OfferTemplateId = o.OfferTemplateId,
                ActualPrice = o.ActualPrice,
                TotalQuantity = o.TotalQuantity,
                MaxPerCustomer = o.MaxPerCustomer
            }).ToList()
        };

        try
        {
            var created = await _api.PostAsync<SaleBatchDto>(
                "staff/sale-batches",
                request,
                cancellationToken);

            if (activateAfterCreate && created.Id != Guid.Empty)
            {
                await _api.PostAsync(
                    $"staff/sale-batches/{created.Id}/activate",
                    null,
                    cancellationToken);
                TempData["Success"] = "Đã tạo đợt bán và kích hoạt (Draft → Scheduled).";
            }
            else
            {
                TempData["Success"] = "Đã tạo đợt bán ở trạng thái Draft.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            model.Step = 4;
            model.ErrorMessage = FormatApiError(ex);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Activate")]
    public async Task<IActionResult> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.PostAsync($"staff/sale-batches/{id}/activate", null, cancellationToken);
            TempData["Success"] = "Đã kích hoạt đợt bán.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.BrandManager)]
    [ValidateAntiForgeryToken]
    [ActionName("Cancel")]
    public async Task<IActionResult> CancelAsync(
        Guid id,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Huỷ đợt bắt buộc nhập lý do (BR-23).";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _api.PostAsync(
                $"staff/sale-batches/{id}/cancel",
                new CancelBatchRequest { Reason = reason.Trim() },
                cancellationToken);
            TempData["Success"] = "Đã huỷ đợt bán.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadStep1LookupsAsync(
        SaleBatchWizardViewModel vm,
        CancellationToken cancellationToken)
    {
        try
        {
            vm.AvailableTemplates = await LoadAllTemplatesAsync(cancellationToken);

            if (vm.IsBrandManager)
            {
                var storesPath = QueryHelpers.AddQueryString("brand/stores", new Dictionary<string, string?>
                {
                    ["page"] = "1",
                    ["pageSize"] = "100"
                });
                var stores = await _api.GetAsync<PagedResponse<StoreDto>>(storesPath, cancellationToken);
                vm.AvailableStores = stores.Items;
            }
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = FormatApiError(ex);
        }
    }

    private async Task<IReadOnlyList<OfferTemplateDto>> LoadAllTemplatesAsync(
        CancellationToken cancellationToken)
    {
        var path = QueryHelpers.AddQueryString("brand/offer-templates", new Dictionary<string, string?>
        {
            ["page"] = "1",
            ["pageSize"] = "100"
        });
        var page = await _api.GetAsync<PagedResponse<OfferTemplateDto>>(path, cancellationToken);
        return page.Items;
    }

    private static string FormatApiError(ApiException ex)
    {
        if (!string.IsNullOrWhiteSpace(ex.Message))
        {
            return ex.Message;
        }

        return "Thao tác API thất bại.";
    }

}
