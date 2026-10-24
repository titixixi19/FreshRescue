using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using FluentValidation;
using FluentValidation.AspNetCore;
using FreshRescue.WebClient.Extensions;
using FreshRescue.WebClient.Filters;
using Microsoft.Extensions.WebEncoders;

// Chủ sở hữu: Đức Anh (07-WORK-BREAKDOWN.md §12.3). Cần đăng ký thêm gì cho Areas/Dashboard thì nhắn trước.

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(o => o.Filters.Add<ApiExceptionFilter>())
    // JSON trả cho JS dùng cùng quy ước với API: enum dạng chuỗi (03-API-CONTRACT.md §1).
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Razor mặc định mã hoá mọi ký tự ngoài ASCII thành &#x...; — tiếng Việt có dấu thành chuỗi thực thể
// dài gấp mấy lần. Vẫn mã hoá < > & " như thường, nên không mở lỗ XSS.
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services
    .AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<Program>();

builder.Services
    .AddCookieAuth()
    .AddApiClient(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
