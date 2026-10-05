using FreshRescue.Entities.Identity;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class SaleBatchConfiguration : IEntityTypeConfiguration<SaleBatch>
{
    public void Configure(EntityTypeBuilder<SaleBatch> builder)
    {
        // BR-15 đã kiểm ở service; CHECK là lưới an toàn khi dữ liệu vào bằng đường khác (seeder, SQL tay).
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SaleBatches_TimeOrder",
            "[SaleStartTime] < [SaleEndTime] AND [SaleEndTime] <= [PickupEndTime] " +
            "AND [PickupStartTime] < [PickupEndTime] AND [SaleStartTime] <= [PickupStartTime]"));

        builder.Property(b => b.Name).HasMaxLength(150);
        builder.Property(b => b.CancelReason).HasMaxLength(500);

        // Truy vấn nóng #2 và #4: offer đang mở bán, job mở / đóng đợt (02 §5).
        builder.HasIndex(b => new { b.Status, b.SaleStartTime });
        builder.HasIndex(b => new { b.Status, b.PickupEndTime });

        builder.HasOne(b => b.Store)
            .WithMany(s => s.SaleBatches)
            .HasForeignKey(b => b.StoreId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId);
    }
}
