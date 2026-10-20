
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

[Area("Dashboard")]
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.BrandManager)]
public class ImportExportController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["IsBlocked"] = true;
        ViewData["BlockReason"] =
            "API chưa cung cấp endpoint stream file .xlsx và multipart import.";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Import(IFormFile? file)
    {
        TempData["Error"] =
            "Import đang bị chặn do API backend chưa hỗ trợ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Export()
    {
        TempData["Error"] =
            "Export đang bị chặn do API backend chưa hỗ trợ stream file.";
        return RedirectToAction(nameof(Index));
    }
}
