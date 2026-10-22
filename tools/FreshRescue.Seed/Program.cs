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
services.AddDataAccessLayer(connectionString);
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
Console.WriteLine("Seeder dùng email @freshrescue.test và mật khẩu demo được cấp qua Seed__DemoPassword.");
