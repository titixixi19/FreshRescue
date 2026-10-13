using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.Property(d => d.PhotoUrl).HasMaxLength(500);
        builder.Property(d => d.MerchantNote).HasMaxLength(1000);
        builder.Property(d => d.AdminNote).HasMaxLength(1000);

        builder.HasIndex(d => d.Status);

        // Mỗi đơn một khiếu nại (BR-17) — quan hệ một-một khiến OrderId thành unique.
        builder.HasOne(d => d.Order)
            .WithOne(o => o.Dispute)
            .HasForeignKey<Dispute>(d => d.OrderId);

        builder.HasOne(d => d.Customer)
            .WithMany()
            .HasForeignKey(d => d.CustomerUserId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(d => d.ResolvedByUserId);
    }
}
