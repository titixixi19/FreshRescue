using FreshRescue.API.Extensions;
using FreshRescue.API.Hubs;
using FreshRescue.API.Middleware;
using FreshRescue.Business;
using FreshRescue.DataAccess;

// ĐÓNG BĂNG — chủ sở hữu: Tùng (07-WORK-BREAKDOWN.md §12.1).
// Cần đăng ký thêm gì thì sửa file extension của phần mình trong Extensions/, không sửa ở đây.

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Default. Chép appsettings.Development.json.example thành "
        + "appsettings.Development.json rồi sửa chuỗi kết nối.");

builder.Services
    .AddDataAccessLayer(connectionString)
    .AddBusinessLayer()
    .AddApiLayer(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Phục vụ ảnh đã upload ở wwwroot/uploads (OQ-09).
app.UseStaticFiles();

app.UseRouting();
app.UseCors(ApiLayerExtensions.WebClientCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OfferHub>("/hubs/offers");

app.Run();

/// <summary>Mở cho WebApplicationFactory trong project test.</summary>
public partial class Program
{
}
