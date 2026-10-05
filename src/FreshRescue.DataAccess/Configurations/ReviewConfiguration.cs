using FreshRescue.Entities.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.PhotoUrl).HasMaxLength(500);

        // Mỗi đơn đúng một đánh giá (BR-12) — quan hệ một-một khiến OrderId thành unique.
        builder.HasOne(r => r.Order)
            .WithOne(o => o.Review)
            .HasForeignKey<Review>(r => r.OrderId);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerUserId);

        builder.HasOne(r => r.Store)
            .WithMany()
            .HasForeignKey(r => r.StoreId);
    }
}
