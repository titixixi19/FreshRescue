using System.Diagnostics;
using FreshRescue.WebClient.Models;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.WebClient.Controllers;

/// <summary>
/// Trang chủ gốc (/) — chuyển vào storefront Customer.
/// Không chứa Dashboard.
/// </summary>
public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(
            actionName: "Index",
            controllerName: "Storefront",
            routeValues: new { area = "Customer" });
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
