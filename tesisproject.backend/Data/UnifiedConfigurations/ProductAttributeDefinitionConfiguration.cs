using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ProductAttributeDefinitionConfiguration : IEntityTypeConfiguration<ProductAttributeDefinition>
{
    public void Configure(EntityTypeBuilder<ProductAttributeDefinition> entity)
    {
        entity.HasIndex(x => new { x.ProductTypeId, x.ProductAttributeId })
            .IsUnique()
            .HasDatabaseName("UX_ProductAttributeDefinitions_Type_Attribute");
    }
}
