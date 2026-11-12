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

    // Dữ liệu demo trông như thật nhưng hoàn toàn hư cấu: tên thương hiệu tự đặt, không trùng chuỗi có thật;
    // chỉ tên đường là thật (Quận 1), số nhà là bịa. Thương hiệu thứ i khớp danh mục thứ i ở trên.
    private static readonly (string Name, string Slug, string ShortName, string Description)[] _brands =
    [
        ("Tiệm Bánh Mầm Xanh", "mam-xanh", "Mầm Xanh", "Bánh kem, bông lan và tart nướng mỗi sáng."),
        ("Lò Bánh Mì Phố Cũ", "banh-mi-pho-cu", "Phố Cũ", "Bánh mì nướng lò củi, giòn vỏ mềm ruột."),
        ("Tiệm Bánh Mặn Lá Dứa", "banh-man-la-dua", "Lá Dứa", "Bánh bao, pateso, bánh pía làm thủ công."),
        ("Bếp Chay An Nhiên", "bep-chay-an-nhien", "An Nhiên", "Cơm chay, bún chay nấu từ rau nấm tươi."),
        ("Cơm Nhà Mộc Lan", "com-nha-moc-lan", "Mộc Lan", "Cơm văn phòng vị nhà nấu, đổi món mỗi ngày."),
        ("Trà & Bánh Gió Nam", "tra-banh-gio-nam", "Gió Nam", "Trà sữa, trà trái cây và bánh ăn kèm."),
        ("Sạp Trái Cây Vườn Nhà", "trai-cay-vuon-nha", "Vườn Nhà", "Trái cây theo mùa từ nhà vườn miền Tây."),
        ("Rau Sạch Sớm Mai", "rau-sach-som-mai", "Sớm Mai", "Rau củ thu hoạch trong ngày từ Củ Chi."),
        ("Chợ Tươi Mỗi Ngày", "cho-tuoi-moi-ngay", "Chợ Tươi", "Thịt cá tươi sơ chế sẵn, giao trong ngày."),
        ("Bếp Nhanh 247", "bep-nhanh-247", "Bếp Nhanh", "Gà rán, khoai chiên và burger nóng giòn."),
        ("Quán Bún Chiều Hạ", "bun-chieu-ha", "Chiều Hạ", "Bún bò, hủ tiếu, mì nước dùng hầm xương."),
        ("Sữa Chua Mây Trắng", "sua-chua-may-trang", "Mây Trắng", "Sữa chua dẻo, sữa chua uống ủ thủ công."),
        ("Kho Lạnh Biển Xanh", "kho-lanh-bien-xanh", "Biển Xanh", "Hải sản cấp đông và thực phẩm đông lạnh."),
        ("Cơm Tấm Ngói Đỏ", "com-tam-ngoi-do", "Ngói Đỏ", "Cơm tấm sườn nướng than kiểu Sài Gòn."),
        ("Điểm Tâm Kim Hoa", "diem-tam-kim-hoa", "Kim Hoa", "Dimsum, há cảo, xíu mại hấp nóng."),
        ("Bếp Âu Olive", "bep-au-olive", "Olive", "Mì Ý, bò hầm và món Âu phần một người."),
        ("Salad Lá Non", "salad-la-non", "Lá Non", "Salad rau trộn và sandwich ăn kiêng."),
        ("Xôi Sáng Bình Minh", "xoi-sang-binh-minh", "Bình Minh", "Xôi mặn, xôi ngọt và bánh mì chảo buổi sáng."),
        ("Ăn Vặt Góc Phố", "an-vat-goc-pho", "Góc Phố", "Bánh tráng trộn, khoai lắc, chả giò mini."),
        ("Combo Nhà Mình", "combo-nha-minh", "Nhà Mình", "Mâm cơm gia đình 3–4 người, nấu theo ngày."),
    ];

    private static readonly (string AddressLine, string Ward)[] _addresses =
    [
        ("45 Lê Lợi", "Bến Nghé"), ("12 Nguyễn Thái Bình", "Nguyễn Thái Bình"), ("88 Lê Thánh Tôn", "Bến Thành"),
        ("21 Tôn Thất Đạm", "Bến Nghé"), ("150 Calmette", "Nguyễn Thái Bình"), ("34 Phó Đức Chính", "Nguyễn Thái Bình"),
        ("67 Ký Con", "Nguyễn Thái Bình"), ("102 Hàm Nghi", "Bến Thành"), ("19 Nam Kỳ Khởi Nghĩa", "Bến Thành"),
        ("250 Lê Lai", "Bến Thành"), ("76 Nguyễn Thái Học", "Cầu Ông Lãnh"), ("9 Cống Quỳnh", "Phạm Ngũ Lão"),
        ("140 Trần Hưng Đạo", "Cầu Ông Lãnh"), ("58 Bùi Viện", "Phạm Ngũ Lão"), ("33 Đề Thám", "Cô Giang"),
        ("120 Phạm Ngũ Lão", "Phạm Ngũ Lão"), ("27 Lý Tự Trọng", "Bến Nghé"), ("64 Mạc Thị Bưởi", "Bến Nghé"),
        ("15 Pasteur", "Bến Nghé"), ("200 Nguyễn Trãi", "Phạm Ngũ Lão"),
    ];

    private static readonly string[] _managerNames =
    [
        "Nguyễn Thu Hà", "Trần Minh Khoa", "Lê Bảo Ngọc", "Phạm Quốc Huy", "Hoàng Thanh Tâm",
        "Huỳnh Gia Linh", "Phan Đức Thịnh", "Vũ Mai Anh", "Võ Hoàng Long", "Đặng Thùy Dương",
        "Bùi Nhật Nam", "Đỗ Khánh Vy", "Hồ Tuấn Kiệt", "Ngô Phương Thảo", "Dương Anh Tuấn",
        "Lý Ngọc Hân", "Trịnh Văn Phúc", "Mai Hồng Nhung", "Tạ Quang Vinh", "Cao Bích Trâm",
    ];

    private static readonly string[] _staffNames =
    [
        "Nguyễn Văn An", "Trần Thị Bích", "Lê Hoàng Phúc", "Phạm Thị Lan", "Hoàng Minh Đức",
        "Huỳnh Thị Mỹ", "Phan Văn Tài", "Vũ Thị Hồng", "Võ Thành Đạt", "Đặng Thị Kim",
        "Bùi Văn Hùng", "Đỗ Thị Hạnh", "Hồ Minh Trí", "Ngô Thị Thu", "Dương Văn Lộc",
        "Lý Thị Ngân", "Trịnh Minh Quân", "Mai Thị Yến", "Tạ Văn Sơn", "Cao Thị Diễm",
    ];

    private static readonly string[] _customerNames =
    [
        "Nguyễn Hải Yến", "Trần Gia Bảo", "Lê Minh Châu", "Phạm Hoàng Nam", "Hoàng Ngọc Ánh",
        "Huỳnh Tấn Phát", "Phan Thảo Nhi", "Vũ Đình Khôi", "Võ Ngọc Trinh", "Đặng Quang Minh",
        "Bùi Thanh Hương", "Đỗ Hữu Nghĩa", "Hồ Bảo Trân", "Ngô Đức Anh", "Dương Mỹ Linh",
        "Lý Thành Công", "Trịnh Khánh Linh", "Mai Xuân Bách", "Tạ Thu Trang", "Cao Minh Nhật",
    ];

    // Mẫu thứ i thuộc thương hiệu (i-1)%20 và danh mục (i-1)%20; i lẻ là túi mù, i chẵn là món xả kho.
    // Giá gợi ý luôn nằm trong khung BR-07: túi mù 30% giá gốc (giảm 70%), món lẻ 53–55% giá gốc (giảm 45–47%).
    private static readonly (string Name, string Description, decimal OriginalPrice, decimal SuggestedPrice)[] _templates =
    [
        ("Túi mù bánh ngọt cuối ngày", "3–5 chiếc bánh kem, bông lan, tart nướng trong ngày.", 150000m, 45000m),
        ("Combo 5 ổ bánh mì que pate", "Bánh mì que giòn nhân pate gan, nướng buổi chiều.", 50000m, 27000m),
        ("Túi mù bánh mặn", "Bánh bao, pateso, bánh xếp — 4 đến 6 cái.", 120000m, 36000m),
        ("Cơm chay thập cẩm", "Cơm trắng, đậu hũ sốt cà, nấm xào và canh rau.", 50000m, 27000m),
        ("Túi mù cơm hộp 2 phần", "Hai phần cơm hộp món mặn thay đổi theo ngày.", 110000m, 33000m),
        ("Trà sữa trân châu size L", "Pha trong ngày, đá để riêng.", 45000m, 24000m),
        ("Túi mù trái cây 2kg", "Trái cây theo mùa, hơi chín, ăn ngay rất ngọt.", 100000m, 30000m),
        ("Bó rau củ nấu canh 1kg", "Bí đỏ, cà rốt, rau ngót — thu hoạch hôm qua.", 30000m, 16000m),
        ("Túi mù thịt cá sơ chế", "Thịt heo, cá basa đã sơ chế, nấu trong ngày.", 160000m, 48000m),
        ("Gà rán 4 miếng", "Chiên ca chiều, hâm lại vẫn giòn.", 80000m, 44000m),
        ("Túi mù bún & mì 2 phần", "Bún bò, hủ tiếu hoặc mì — nước dùng để riêng.", 100000m, 30000m),
        ("Sữa chua dẻo hộp 6", "Còn hạn 2 ngày, bảo quản lạnh.", 60000m, 33000m),
        ("Túi mù hải sản đông lạnh", "Tôm, mực, chả cá sắp hết hạn trưng bày.", 200000m, 60000m),
        ("Cơm tấm sườn bì chả", "Sườn nướng than, bì chả làm trong ngày.", 55000m, 30000m),
        ("Túi mù dimsum 12 viên", "Há cảo, xíu mại, bánh bao nhân thịt hấp sẵn.", 120000m, 36000m),
        ("Mì Ý sốt bò bằm", "Phần một người, hâm nóng 2 phút là dùng được.", 90000m, 49000m),
        ("Túi mù salad & sandwich", "2 hộp salad rau trộn và 1 sandwich gà.", 110000m, 33000m),
        ("Xôi mặn thập cẩm", "Xôi nếp, lạp xưởng, chà bông, trứng cút.", 35000m, 19000m),
        ("Túi mù ăn vặt", "Bánh tráng trộn, khoai lang lắc, chả giò mini.", 80000m, 24000m),
        ("Combo cơm gia đình 3 người", "3 món mặn, 1 canh và cơm trắng.", 180000m, 99000m),
        ("Túi mù croissant & bánh mì ngọt", "Croissant bơ, bánh mì hoa cúc, bánh cuộn.", 130000m, 39000m),
        ("Bánh mì thịt nguội", "Ổ đặc ruột, thịt nguội, pate và đồ chua.", 30000m, 16000m),
        ("Túi mù bánh pía & bánh kẹp", "Bánh pía sầu riêng, bánh kẹp lá dứa.", 90000m, 27000m),
        ("Bún chay Huế", "Nước lèo nấm, chả chay, đậu hũ chiên.", 45000m, 24000m),
        ("Túi mù cơm văn phòng 3 phần", "Ba phần cơm món mặn và canh trong ngày.", 150000m, 45000m),
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
            var brandInfo = _brands[i - 1];
            var manager = await EnsureDemoUserAsync(
                userManager,
                $"brandmanager{i:00}@freshrescue.test",
                _managerNames[i - 1],
                AppRoles.BrandManager,
                demoPassword);

            var slug = brandInfo.Slug;
            var brand = await db.Brands
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);

            if (brand is null)
            {
                brand = new Brand
                {
                    Name = brandInfo.Name,
                    Slug = slug,
                    Description = brandInfo.Description,
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

            var (addressLine, ward) = _addresses[i - 1];
            // "Mầm Xanh – Lê Lợi": tên ngắn của thương hiệu + tên đường, kiểu các chuỗi F&B hay đặt.
            var streetName = addressLine[(addressLine.IndexOf(' ') + 1)..];
            var storeName = $"{brandInfo.ShortName} – {streetName}";
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
                    AddressLine = addressLine,
                    Ward = ward,
                    District = "Quận 1",
                    City = "TP. Hồ Chí Minh",
                    Latitude = 10.765000m + (zeroBased / 5) * 0.003m,
                    Longitude = 106.688000m + (zeroBased % 5) * 0.003m,
                    Phone = $"0909{i * 48271 % 1000000:000000}",
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
                _staffNames[i - 1],
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
                _customerNames[i - 1],
                AppRoles.Customer,
                demoPassword);
        }

        for (var i = 1; i <= TemplateCount; i++)
        {
            var brandId = brandIds[(i - 1) % brandIds.Count];
            var categoryId = categoryIds[(i - 1) % categoryIds.Count];
            var templateInfo = _templates[i - 1];
            var name = templateInfo.Name;

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
                Description = templateInfo.Description,
                OfferType = isBag ? OfferType.SurpriseBag : OfferType.ClearanceItem,
                OriginalPrice = templateInfo.OriginalPrice,
                SuggestedPrice = templateInfo.SuggestedPrice,
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
