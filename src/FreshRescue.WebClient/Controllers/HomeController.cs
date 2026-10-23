using System.Security.Claims;
using FreshRescue.WebClient.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Tổng quan Dashboard (TV5). Phân quyền bằng role — không chỉ ẩn menu.
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.BrandManager + "," + AppRoles.StoreStaff)]
public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var vm = new DashboardHomeViewModel
        {
            DisplayName = User.FindFirst("name")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.Identity?.Name
                ?? "Người dùng",
            IsAdmin = User.IsInRole(AppRoles.Admin),
            IsBrandManager = User.IsInRole(AppRoles.BrandManager),
            IsStoreStaff = User.IsInRole(AppRoles.StoreStaff),
            BrandId = ParseGuidClaim("brand_id"),
            StoreId = ParseGuidClaim("store_id")
        };

        return View(vm);
    }

    private Guid? ParseGuidClaim(string type)
    {
        var raw = User.FindFirst(type)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}

public class DashboardHomeViewModel
{
    public string DisplayName { get; set; } = "";
    public bool IsAdmin { get; set; }
    public bool IsBrandManager { get; set; }
    public bool IsStoreStaff { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? StoreId { get; set; }
}
