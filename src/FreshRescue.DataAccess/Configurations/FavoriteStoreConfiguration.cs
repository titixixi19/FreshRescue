using FreshRescue.Entities.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class FavoriteStoreConfiguration : IEntityTypeConfiguration<FavoriteStore>
{
    public void Configure(EntityTypeBuilder<FavoriteStore> builder)
    {
        builder.HasKey(f => new { f.CustomerUserId, f.StoreId });

        builder.HasOne(f => f.Customer)
            .WithMany()
            .HasForeignKey(f => f.CustomerUserId);

        // Khoá ngoại tự sinh chỉ mục trên StoreId — cần để bắn thông báo đợt bán mới (OQ-29).
        builder.HasOne(f => f.Store)
            .WithMany()
            .HasForeignKey(f => f.StoreId);
    }
}
