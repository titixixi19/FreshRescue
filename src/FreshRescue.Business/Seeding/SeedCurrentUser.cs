using FreshRescue.Business.Common;

namespace FreshRescue.Business.Seeding;

/// <summary>Người dùng hiện tại do seeder đặt tay, thay cho JWT. Mỗi bước nghiệp vụ đóng vai một người.</summary>
public sealed class SeedCurrentUser : ICurrentUser
{
    public Guid? UserId { get; private set; }

    public IReadOnlyList<string> Roles { get; private set; } = [];

    public Guid? BrandId { get; private set; }

    public Guid? StoreId { get; private set; }

    public bool IsInRole(string role) => Roles.Contains(role);

    public void Act(Guid userId, string role, Guid? brandId = null, Guid? storeId = null)
    {
        UserId = userId;
        Roles = [role];
        BrandId = brandId;
        StoreId = storeId;
    }
}
