using System.Diagnostics;
using FreshRescue.WebClient.Models;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.WebClient.Controllers;

public class HomeController : Controller
{
    /// <summary>Trang chủ của site chính là storefront.</summary>
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Storefront", new { area = "Customer" });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
