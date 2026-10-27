using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.DataAccess.Seeding;

public static class DbSeeder
{
    private const int BrandCount = 20;
    private const int TemplateCount = 25;
    private const int CustomerCount = 20;

    private static readonly (string Name, string Slug)[] _categories =
    [
        ("Bánh ngọt", "banh-ngot"),
        ("Bánh mì", "banh-mi"),
        ("Bánh mặn", "banh-man"),
        ("Đồ chay", "do-chay"),
        ("Cơm hộp", "com-hop"),
        ("Đồ uống", "do-uong"),
        ("Trái cây", "trai-cay"),
        ("Rau củ", "rau-cu"),
        ("Thực phẩm tươi", "thuc-pham-tuoi"),
        ("Đồ ăn nhanh", "do-an-nhanh"),
        ("Mì và bún", "mi-va-bun"),
        ("Sữa và chế phẩm", "sua-va-che-pham"),
        ("Đồ đông lạnh", "do-dong-lanh"),
        ("Món Việt", "mon-viet"),
        ("Món Á", "mon-a"),
        ("Món Âu", "mon-au"),
        ("Salad", "salad"),
        ("Đồ ăn sáng", "do-an-sang"),
        ("Ăn vặt", "an-vat"),
        ("Combo tiết kiệm", "combo-tiet-kiem")
    ];

    public static async Task SeedIdentityAsync(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        string adminEmail,
        string adminPassword)
    {
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

            var result = await roleManager.CreateAsync(new AppRole(roleName));
            EnsureSucceeded(result, $"Tạo role {roleName}");
        }

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            throw new InvalidOperationException("Thiếu Seed:AdminEmail.");
        }

        adminEmail = adminEmail.Trim();
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is not null)
        {
            if (!await userManager.IsInRoleAsync(existingAdmin, AppRoles.SystemAdmin))
            {
                throw new InvalidOperationException(
                    "Email Admin đã tồn tại nhưng không có quyền SystemAdmin. Không tự nâng quyền.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Thiếu Seed:AdminPassword.");
        }

        await EnsureDemoUserAsync(
            userManager,
            adminEmail,
            "FreshRescue System Admin",
            AppRoles.SystemAdmin,
            adminPassword);
    }

    public static async Task SeedStaticDataAsync(
        FreshRescueDbContext db,
        UserManager<AppUser> userManager,
        string adminEmail,
        string demoPassword,
        TimeProvider clock,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(demoPassword))
        {
            throw new InvalidOperationException("Thiếu Seed:DemoPassword.");
        }

        var admin = await userManager.FindByEmailAsync(adminEmail.Trim())
            ?? throw new InvalidOperationException("Chưa tìm thấy tài khoản Admin để duyệt Brand mẫu.");

        var categoryIds = new List<Guid>();
        for (var i = 0; i < _categories.Length; i++)
        {
            var (name, slug) = _categories[i];
            var category = await db.Categories
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);

            if (category is null)
            {
                category = new Category
                {
                    Name = name,
                    Slug = slug,
                    DisplayOrder = i + 1
                };
                db.Categories.Add(category);
                await db.SaveChangesAsync(cancellationToken);
            }
            else if (category.IsDeleted)
            {
                throw new InvalidOperationException($"Category '{slug}' đã bị xóa mềm; không tự khôi phục.");
            }

            categoryIds.Add(category.Id);
        }

        var brandIds = new List<Guid>();
        for (var i = 1; i <= BrandCount; i++)
        {
            var manager = await EnsureDemoUserAsync(
                userManager,
                $"brandmanager{i:00}@freshrescue.test",
                $"Quản lý thương hiệu Demo {i:00}",
                AppRoles.BrandManager,
                demoPassword);

            var slug = $"freshrescue-demo-brand-{i:00}";
            var brand = await db.Brands
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);

            if (brand is null)
            {
                brand = new Brand
                {
                    Name = $"FreshRescue Demo Brand {i:00}",
                    Slug = slug,
                    OwnerUserId = manager.Id,
                    Status = BrandStatus.Approved,
                    ApprovedAt = clock.GetUtcNow().UtcDateTime,
                    ApprovedByUserId = admin.Id
                };
                db.Brands.Add(brand);
                await db.SaveChangesAsync(cancellationToken);
            }
            else if (brand.IsDeleted || brand.OwnerUserId != manager.Id)
            {
                throw new InvalidOperationException($"Brand '{slug}' đã xóa hoặc không thuộc manager mẫu.");
            }
            else if (brand.Status != BrandStatus.Approved)
            {
                throw new InvalidOperationException($"Brand '{slug}' không ở trạng thái Approved.");
            }

            brandIds.Add(brand.Id);

            var storeName = $"Chi nhánh Demo {i:00}";
            var store = await db.Stores
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.BrandId == brand.Id && x.Name == storeName, cancellationToken);

            if (store is null)
            {
                var zeroBased = i - 1;
                store = new Store
                {
                    BrandId = brand.Id,
                    Name = storeName,
                    AddressLine = $"Địa điểm minh họa số {i:00} (không phải địa chỉ thật)",
                    Ward = "Khu vực demo",
                    District = "Quận 1",
                    City = "TP. Hồ Chí Minh",
                    Latitude = 10.765000m + (zeroBased / 5) * 0.003m,
                    Longitude = 106.688000m + (zeroBased % 5) * 0.003m,
                    Phone = $"090000{i:0000}",
                    OpenTime = new TimeOnly(7, 0),
                    CloseTime = new TimeOnly(22, 0),
                    Status = StoreStatus.Active
                };
                db.Stores.Add(store);
                await db.SaveChangesAsync(cancellationToken);
            }
            else if (store.IsDeleted)
            {
                throw new InvalidOperationException($"Store '{storeName}' đã bị xóa mềm.");
            }

            var staff = await EnsureDemoUserAsync(
                userManager,
                $"storestaff{i:00}@freshrescue.test",
                $"Nhân viên chi nhánh Demo {i:00}",
                AppRoles.StoreStaff,
                demoPassword);

            var assignment = await db.StoreStaff
                .SingleOrDefaultAsync(x => x.UserId == staff.Id, cancellationToken);
            if (assignment is null)
            {
                db.StoreStaff.Add(new StoreStaff
                {
                    UserId = staff.Id,
                    StoreId = store.Id,
                    Position = "Nhân viên bán hàng",
                    IsActive = true
                });
                await db.SaveChangesAsync(cancellationToken);
            }
            else if (assignment.StoreId != store.Id)
            {
                throw new InvalidOperationException($"Nhân viên {staff.Email} đang thuộc Store khác.");
            }
        }

        for (var i = 1; i <= CustomerCount; i++)
        {
            await EnsureDemoUserAsync(
                userManager,
                $"customer{i:00}@freshrescue.test",
                $"Khách hàng Demo {i:00}",
                AppRoles.Customer,
                demoPassword);
        }

        for (var i = 1; i <= TemplateCount; i++)
        {
            var brandId = brandIds[(i - 1) % brandIds.Count];
            var categoryId = categoryIds[(i - 1) % categoryIds.Count];
            var name = $"Mặt hàng Demo {i:00}";

            var existing = await db.OfferTemplates
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    x => x.BrandId == brandId && x.Name == name,
                    cancellationToken);
            if (existing is not null)
            {
                if (existing.IsDeleted)
                {
                    throw new InvalidOperationException($"OfferTemplate '{name}' đã bị xóa mềm.");
                }

                continue;
            }

            var isBag = i % 2 == 1;
            db.OfferTemplates.Add(new OfferTemplate
            {
                BrandId = brandId,
                CategoryId = categoryId,
                Name = name,
                Description = "Dữ liệu minh họa cho demo FreshRescue",
                OfferType = isBag ? OfferType.SurpriseBag : OfferType.ClearanceItem,
                OriginalPrice = 100000m,
                SuggestedPrice = isBag ? 30000m : 55000m,
                BrandMinDiscountPercent = isBag ? 65m : 40m,
                BrandMaxDiscountPercent = isBag ? 75m : 50m
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<AppUser> EnsureDemoUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string fullName,
        string role,
        string password)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
            {
                throw new InvalidOperationException(
                    $"Tài khoản {email} đã tồn tại nhưng không có role {role}; không tự nâng quyền.");
            }

            return existing;
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            IsActive = true
        };

        EnsureSucceeded(await userManager.CreateAsync(user, password), $"Tạo {email}");
        EnsureSucceeded(await userManager.AddToRoleAsync(user, role), $"Gán role {role} cho {email}");
        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(x => x.Description));
        throw new InvalidOperationException($"{operation} thất bại: {errors}");
    }
}
