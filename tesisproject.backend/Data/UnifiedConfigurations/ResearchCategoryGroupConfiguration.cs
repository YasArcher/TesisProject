using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ResearchCategoryGroupConfiguration : IEntityTypeConfiguration<ResearchCategoryGroup>
{
    public void Configure(EntityTypeBuilder<ResearchCategoryGroup> entity)
    {
        entity.HasMany(g => g.Types)
         .WithOne(t => t.ResearchCategoryGroup)
         .HasForeignKey(t => t.ResearchCategoryGroupId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
