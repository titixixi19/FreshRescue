using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        // BR-01: lưới an toàn cuối cùng — logic chống over-selling thủng thì DB nổ ngay thay vì bán âm.
        builder.ToTable(t => t.HasCheckConstraint("CK_Offers_RemainingQuantity", "[RemainingQuantity] >= 0"));

        builder.Property(o => o.Name).HasMaxLength(200);
        builder.Property(o => o.Description).HasMaxLength(1000);
        builder.Property(o => o.ImageUrl).HasMaxLength(500);

        // Điểm neo của optimistic locking khi đặt hàng (NFR-01, 02 §6.1).
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasIndex(o => new { o.SaleBatchId, o.RemainingQuantity });

        builder.HasOne(o => o.SaleBatch)
            .WithMany(b => b.Offers)
            .HasForeignKey(o => o.SaleBatchId);

        builder.HasOne(o => o.OfferTemplate)
            .WithMany()
            .HasForeignKey(o => o.OfferTemplateId);

        builder.HasOne(o => o.Category)
            .WithMany()
            .HasForeignKey(o => o.CategoryId);
    }
}
