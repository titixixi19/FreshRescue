using FreshRescue.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.OrderCode)
            .HasMaxLength(11)
            .IsFixedLength()
            .IsUnicode(false);
        builder.HasIndex(o => o.OrderCode).IsUnique();

        builder.Property(o => o.CancelReason).HasMaxLength(500);

        builder.Property(o => o.RowVersion).IsRowVersion();

        // Truy vấn nóng #3 (job quét đơn quá hạn) và #5 (giới hạn mua BR-06) — 02 §5.
        builder.HasIndex(o => new { o.Status, o.PaymentDeadline });
        builder.HasIndex(o => new { o.CustomerUserId, o.Status });

        builder.HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerUserId);

        builder.HasOne(o => o.Store)
            .WithMany()
            .HasForeignKey(o => o.StoreId);

        builder.HasOne(o => o.SaleBatch)
            .WithMany(b => b.Orders)
            .HasForeignKey(o => o.SaleBatchId);
    }
}
