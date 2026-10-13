using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.Property(b => b.Name).HasMaxLength(150);

        builder.Property(b => b.Slug).HasMaxLength(160);
        builder.HasIndex(b => b.Slug).IsUnique();

        builder.Property(b => b.LogoUrl).HasMaxLength(500);
        builder.Property(b => b.Description).HasMaxLength(1000);
        builder.Property(b => b.StatusReason).HasMaxLength(500);

        builder.HasIndex(b => b.Status);

        // Brand 1—1 BrandManager: quan hệ một-một khiến OwnerUserId thành unique.
        builder.HasOne(b => b.Owner)
            .WithOne()
            .HasForeignKey<Brand>(b => b.OwnerUserId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(b => b.ApprovedByUserId);
    }
}
