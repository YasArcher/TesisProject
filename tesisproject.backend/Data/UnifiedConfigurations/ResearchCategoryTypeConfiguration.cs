using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ResearchCategoryTypeConfiguration : IEntityTypeConfiguration<ResearchCategoryType>
{
    public void Configure(EntityTypeBuilder<ResearchCategoryType> entity)
    {
        entity.HasMany(t => t.ResearchCategories)
         .WithOne(rc => rc.ResearchCategoryType)
         .HasForeignKey(rc => rc.ResearchCategoryTypeId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
