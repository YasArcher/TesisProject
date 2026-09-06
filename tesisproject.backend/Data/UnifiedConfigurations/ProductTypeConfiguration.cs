using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.HasIndex(x => x.Name).IsUnique();
    }
}
