using FreshRescue.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class StoreStaffConfiguration : IEntityTypeConfiguration<StoreStaff>
{
    public void Configure(EntityTypeBuilder<StoreStaff> builder)
    {
        builder.Property(s => s.Position).HasMaxLength(100);

        // Một nhân viên chỉ thuộc một chi nhánh (OQ-07) — quan hệ một-một khiến UserId thành unique.
        builder.HasOne(s => s.User)
            .WithOne()
            .HasForeignKey<StoreStaff>(s => s.UserId);

        builder.HasOne(s => s.Store)
            .WithMany(st => st.Staff)
            .HasForeignKey(s => s.StoreId);
    }
}
