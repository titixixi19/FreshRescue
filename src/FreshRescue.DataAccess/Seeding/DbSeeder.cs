using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace FreshRescue.DataAccess.Seeding;

public static class DbSeeder
{
    public static async Task SeedIdentityAsync(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        string adminEmail,
        string adminPassword)
    {
        // Tạo 4 role cố định của FreshRescue.
        string[] roles =
        [
            AppRoles.SystemAdmin,
            AppRoles.BrandManager,
            AppRoles.StoreStaff,
            AppRoles.Customer
        ];

        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(
                new AppRole(roleName));

            EnsureSucceeded(result, $"Tạo role {roleName}");
        }

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            throw new InvalidOperationException(
                "Thiếu Seed:AdminEmail.");
        }

        adminEmail = adminEmail.Trim();

        // Không tự cấp quyền Admin cho tài khoản đã tồn tại.
        var existingAdmin = await userManager.FindByEmailAsync(
            adminEmail);

        if (existingAdmin != null)
        {
            if (!await userManager.IsInRoleAsync(
                    existingAdmin, AppRoles.SystemAdmin))
            {
                throw new InvalidOperationException(
                    "Email đã tồn tại nhưng chưa có quyền SystemAdmin. " +
                    "Không tự nâng quyền cho tài khoản có sẵn.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "Thiếu Seed:AdminPassword.");
        }

        var admin = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "FreshRescue System Admin",
            EmailConfirmed = true,
            IsActive = true
        };

        // Identity tự hash password.
        var createResult = await userManager.CreateAsync(
            admin, adminPassword);

        EnsureSucceeded(createResult, "Tạo tài khoản Admin");

        // Gán SystemAdmin sau khi tạo user thành công.
        var roleResult = await userManager.AddToRoleAsync(
            admin, AppRoles.SystemAdmin);

        EnsureSucceeded(roleResult, "Gán quyền SystemAdmin");
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(x => x.Description));

        throw new InvalidOperationException(
            $"{operation} thất bại: {errors}");
    }
}
