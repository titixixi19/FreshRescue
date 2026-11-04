using FreshRescue.WebClient.Models.Account;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.WebClient.Controllers;

/// <summary>
/// Đăng nhập dùng chung cho mọi vai trò (T3-TV4-02). Khách về storefront, nhân viên / quản lý /
/// admin về Areas/Dashboard.
/// </summary>
public class AccountController : Controller
{
    private readonly IApiClient _api;
    private readonly ApiSession _session;

    public AccountController(IApiClient api, ApiSession session)
    {
        _api = api;
        _session = session;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        return View(new LoginForm { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAsync(LoginForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        AuthResponse auth;
        try
        {
            auth = await _api.PostAsync<AuthResponse>("auth/login", new LoginRequest(form.Email, form.Password), cancellationToken);
        }
        catch (ApiException ex) when (ex.Status is StatusCodes.Status400BadRequest or StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }

        await _session.SignInAsync(auth);
        return RedirectAfterSignIn(auth.User, form.ReturnUrl);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl)
    {
        return View(new RegisterForm { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterAsync(RegisterForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        AuthResponse auth;
        try
        {
            var request = new RegisterCustomerRequest(
                form.FullName,
                form.Email,
                form.Password,
                string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber);
            auth = await _api.PostAsync<AuthResponse>("auth/register", request, cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == StatusCodes.Status400BadRequest)
        {
            AddApiErrors(ex);
            return View(form);
        }

        await _session.SignInAsync(auth);
        TempData["Success"] = $"Chào mừng {auth.User.FullName} đến với FreshRescue!";
        return RedirectAfterSignIn(auth.User, form.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var tokens = await _session.GetTokensAsync();
        if (tokens is not null)
        {
            try
            {
                await _api.PostAsync("auth/logout", new RefreshTokenRequest(tokens.RefreshToken), cancellationToken);
            }
            catch (ApiException)
            {
                // Không thu hồi được refresh token phía API thì vẫn xoá cookie bên dưới — người dùng
                // muốn thoát, đừng bắt họ ở lại chỉ vì API lỗi.
            }
        }

        await _session.SignOutAsync();
        return RedirectToAction("Index", "Storefront", new { area = "Customer" });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ——— Quên / đặt lại mật khẩu (03 §2) ———

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordForm());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ActionName("ForgotPassword")]
    public async Task<IActionResult> ForgotPasswordPostAsync(ForgotPasswordForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await _api.PostAsync("auth/forgot-password", new { email = form.Email }, cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == StatusCodes.Status400BadRequest)
        {
            AddApiErrors(ex);
            return View(form);
        }

        // API luôn trả cùng một câu dù email có tồn tại hay không — tránh dò ra email nào đã đăng ký.
        TempData["Success"] = "Nếu email tồn tại trên hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        return View(new ResetPasswordForm { Email = email ?? string.Empty, Token = token ?? string.Empty });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ActionName("ResetPassword")]
    public async Task<IActionResult> ResetPasswordPostAsync(ResetPasswordForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await _api.PostAsync("auth/reset-password",
                new { email = form.Email, token = form.Token, newPassword = form.NewPassword }, cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == StatusCodes.Status400BadRequest)
        {
            AddApiErrors(ex);
            return View(form);
        }

        TempData["Success"] = "Đã đặt lại mật khẩu. Hãy đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(Login));
    }

    // ——— Đăng ký thương hiệu (03 §2 register-brand, OQ-01) ———

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterBrand()
    {
        return View(new RegisterBrandForm());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ActionName("RegisterBrand")]
    public async Task<IActionResult> RegisterBrandPostAsync(RegisterBrandForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        AuthResponse auth;
        try
        {
            auth = await _api.PostAsync<AuthResponse>("auth/register-brand", new
            {
                fullName = form.FullName,
                email = form.Email,
                password = form.Password,
                phoneNumber = string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber,
                brandName = form.BrandName,
                brandDescription = string.IsNullOrWhiteSpace(form.BrandDescription) ? null : form.BrandDescription
            }, cancellationToken);
        }
        catch (ApiException ex) when (ex.Status == StatusCodes.Status400BadRequest)
        {
            AddApiErrors(ex);
            return View(form);
        }

        await _session.SignInAsync(auth);
        TempData["Success"] = "Đã gửi đăng ký thương hiệu. Bạn có thể xem hồ sơ, nhưng cần Admin duyệt mới mở bán được.";
        return Redirect("/Dashboard/BrandManagement/Profile");
    }

    private IActionResult RedirectAfterSignIn(UserInfo user, string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        // Một người có thể vừa là khách vừa là nhân viên (OQ-03) — đã là khách thì ưu tiên storefront.
        if (user.Roles.Contains(AppRoles.Customer) || user.Roles.Count == 0)
        {
            return RedirectToAction("Index", "Storefront", new { area = "Customer" });
        }

        return Redirect("/Dashboard");
    }

    // Lỗi theo trường của API (khoá camelCase) gắn vào đúng ô của form (khoá PascalCase).
    private void AddApiErrors(ApiException ex)
    {
        if (ex.Errors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return;
        }

        foreach (var (field, messages) in ex.Errors)
        {
            var key = field.Length == 0 ? field : char.ToUpperInvariant(field[0]) + field[1..];
            foreach (var message in messages)
            {
                ModelState.AddModelError(key, message);
            }
        }
    }
}
