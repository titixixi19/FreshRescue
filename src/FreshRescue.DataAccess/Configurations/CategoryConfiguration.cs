using FreshRescue.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshRescue.DataAccess.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(100);

        builder.Property(c => c.Slug).HasMaxLength(110);
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.IconUrl).HasMaxLength(500);
    }
}
