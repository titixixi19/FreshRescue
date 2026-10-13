using FreshRescue.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(150);
        builder.Property(s => s.AddressLine).HasMaxLength(300);
        builder.Property(s => s.Ward).HasMaxLength(100);
        builder.Property(s => s.District).HasMaxLength(100);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.Phone).HasMaxLength(20);

        builder.Property(s => s.Latitude).HasPrecision(9, 6);
        builder.Property(s => s.Longitude).HasPrecision(9, 6);
        builder.Property(s => s.RatingAverage).HasPrecision(2, 1);

        // Truy vấn nóng #1 "cửa hàng gần tôi": lọc theo thành phố rồi khoanh hộp toạ độ (02 §5.1).
        builder.HasIndex(s => new { s.City, s.IsDeleted });
        builder.HasIndex(s => new { s.Latitude, s.Longitude });

        builder.HasOne(s => s.Brand)
            .WithMany(b => b.Stores)
            .HasForeignKey(s => s.BrandId);
    }
}
