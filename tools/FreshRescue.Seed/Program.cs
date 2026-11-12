using FreshRescue.Business;
using FreshRescue.Business.Common;
using FreshRescue.API.Extensions;
using FreshRescue.Seed;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.Seeding;
using FreshRescue.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Seeder chỉ được chạy ở môi trường Development. Đặt DOTNET_ENVIRONMENT=Development.");
}

var settingsPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "src", "FreshRescue.API", "appsettings.Development.json");

if (!File.Exists(settingsPath))
{
    throw new FileNotFoundException(
        "Không tìm thấy appsettings.Development.json. Hãy chạy lệnh từ thư mục gốc solution.",
        settingsPath);
}

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(Path.GetDirectoryName(settingsPath)!, "appsettings.json"), optional: false)
    .AddJsonFile(settingsPath, optional: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default.");

var adminEmail = configuration["Seed:AdminEmail"];
var adminPassword = configuration["Seed:AdminPassword"];
var demoPassword = configuration["Seed:DemoPassword"];

if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(demoPassword))
{
    throw new InvalidOperationException(
        "Thiếu Seed:AdminEmail hoặc Seed:DemoPassword. Cấu hình qua biến môi trường Seed__... .");
}

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IConfiguration>(configuration);

// Pha 2 gọi service thật nên cần đồng hồ tua được, người dùng đặt tay và QrService thật qua gRPC (phải chạy sẵn QrService).
// Đăng ký TimeProvider trước AddDataAccessLayer vì nó chỉ TryAdd.
var seedClock = new SeedClock();
var seedUser = new SeedCurrentUser();
services.AddSingleton<TimeProvider>(seedClock);
services.AddSingleton<ICurrentUser>(seedUser);
services.AddQrServiceClient(configuration);

services.AddDataAccessLayer(connectionString);
services.AddBusinessLayer();
services.AddIdentityCore<AppUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<FreshRescueDbContext>();

using var provider = services.BuildServiceProvider(validateScopes: true);
using var scope = provider.CreateScope();

var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
var context = scope.ServiceProvider.GetRequiredService<FreshRescueDbContext>();

if (!await context.Database.CanConnectAsync())
{
    throw new InvalidOperationException("Không kết nối được SQL Server. Kiểm tra ConnectionStrings:Default.");
}

if ((await context.Database.GetPendingMigrationsAsync()).Any())
{
    throw new InvalidOperationException("Database chưa áp dụng đủ migrations. Hãy liên hệ người quản lý schema.");
}

await DbSeeder.SeedIdentityAsync(
    roleManager, userManager, adminEmail, adminPassword ?? "");

await DbSeeder.SeedStaticDataAsync(
    context, userManager, adminEmail, demoPassword,
    scope.ServiceProvider.GetRequiredService<TimeProvider>());

Console.WriteLine("Seeder pha 1 hoàn tất: 4 role, 20 Brand, 20 Store, 20 Category, 25 OfferTemplate, 20 Staff, 20 Customer (tối thiểu).");

var transactions = new TransactionSeeder(
    provider, seedClock, seedUser, message =>
    {
        Console.WriteLine(message);
        return Task.CompletedTask;
    },
    demoPassword);

if (SeedImages.EnsurePlaceholders() is null)
{
    Console.WriteLine("Không tìm thấy src/FreshRescue.API/wwwroot — ảnh mẫu của khiếu nại sẽ không hiển thị. Hãy chạy seeder từ thư mục gốc repo.");
}

if (await transactions.AlreadySeededAsync())
{
    Console.WriteLine("Seeder pha 2 đã chạy trước đó (chi nhánh demo đã có đợt bán), bỏ qua. Muốn dựng lại thì xoá DB rồi seed lại.");
}
else
{
    await transactions.SeedAsync();
    Console.WriteLine("Seeder pha 2 hoàn tất: đợt bán, offer, đơn hàng, thanh toán, vé QR do QrService thật ký, đánh giá, khiếu nại, yêu thích, thông báo.");
}

Console.WriteLine("Seeder dùng email @freshrescue.test và mật khẩu demo được cấp qua Seed__DemoPassword.");
