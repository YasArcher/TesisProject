using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ConvocationRuleConfiguration : IEntityTypeConfiguration<ConvocationRule>
{
    public void Configure(EntityTypeBuilder<ConvocationRule> entity)
    {
        entity.Property(x => x.GroupCode).HasMaxLength(64);
        entity.Property(x => x.Notes).HasMaxLength(256);

        entity.HasIndex(x => x.ConvocationId);
        entity.HasIndex(x => new { x.ConvocationId, x.ProductTypeId });
        entity.HasIndex(x => new { x.ConvocationId, x.GroupCode });
        entity.HasIndex(x => new { x.MinDurationMonths, x.MaxDurationMonths });
        entity.HasIndex(x => x.IsActive);

        entity.HasOne(x => x.Convocation)
         .WithMany(c => c.Rules!)
         .HasForeignKey(x => x.ConvocationId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne<ProductType>()
         .WithMany()
         .HasForeignKey(x => x.ProductTypeId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
