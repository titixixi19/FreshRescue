using FreshRescue.DataAccess;
using FreshRescue.DataAccess.Seeding;
using FreshRescue.Entities.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// 1. Đọc cấu hình của FreshRescue.API
var settingsPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "src",
    "FreshRescue.API",
    "appsettings.Development.json");

if (!File.Exists(settingsPath))
{
    throw new FileNotFoundException(
        "Không tìm thấy appsettings.Development.json",
        settingsPath);
}

var configuration = new ConfigurationBuilder()
    .AddJsonFile(settingsPath, optional: false)
    .AddEnvironmentVariables()
    .Build();

// 2. Lấy connection string
var connectionString =
    configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:Default");

// 3. Đăng ký Dependency Injection
var services = new ServiceCollection();

services.AddLogging();

services.AddDataAccessLayer(connectionString);

// 4. Cấu hình ASP.NET Core Identity
services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<FreshRescueDbContext>();

using var provider = services.BuildServiceProvider(
    validateScopes: true);

using var scope = provider.CreateScope();

// 5. Lấy RoleManager và UserManager
var roleManager = scope.ServiceProvider
    .GetRequiredService<RoleManager<AppRole>>();

var userManager = scope.ServiceProvider
    .GetRequiredService<UserManager<AppUser>>();

// 6. Thực hiện Seed
await DbSeeder.SeedIdentityAsync(
    roleManager,
    userManager,
    configuration["Seed:AdminEmail"] ?? "",
    configuration["Seed:AdminPassword"] ?? "");

Console.WriteLine(
    "Seed Role và SystemAdmin hoàn thành.");
