using FreshRescue.Entities.Identity;
using FreshRescue.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class QrTicketConfiguration : IEntityTypeConfiguration<QrTicket>
{
    public void Configure(EntityTypeBuilder<QrTicket> builder)
    {
        builder.Property(q => q.Token).HasMaxLength(500);

        builder.HasIndex(q => q.ExpiresAt);

        builder.HasOne(q => q.Order)
            .WithOne(o => o.QrTicket)
            .HasForeignKey<QrTicket>(q => q.OrderId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(q => q.UsedByStaffUserId);
    }
}
