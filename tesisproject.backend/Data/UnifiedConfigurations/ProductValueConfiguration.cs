using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ProductValueConfiguration : IEntityTypeConfiguration<ProductValue>
{
    public void Configure(EntityTypeBuilder<ProductValue> entity)
    {
        entity.HasIndex(v => new { v.ProductId, v.AttributeDefinitionId })
         .IsUnique();

        entity.HasOne(v => v.Product)
         .WithMany(p => p.Values!)
         .HasForeignKey(v => v.ProductId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(v => v.AttributeDefinition)
         .WithMany(d => d.ProductValues!)
         .HasForeignKey(v => v.AttributeDefinitionId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
