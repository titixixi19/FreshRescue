using FreshRescue.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class OfferTemplateConfiguration : IEntityTypeConfiguration<OfferTemplate>
{
    public void Configure(EntityTypeBuilder<OfferTemplate> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.ImageUrl).HasMaxLength(500);

        builder.Property(t => t.BrandMinDiscountPercent).HasPrecision(5, 2);
        builder.Property(t => t.BrandMaxDiscountPercent).HasPrecision(5, 2);

        builder.HasOne(t => t.Brand)
            .WithMany(b => b.OfferTemplates)
            .HasForeignKey(t => t.BrandId);

        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId);
    }
}
