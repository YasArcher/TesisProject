using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ResearchCategoryConfiguration : IEntityTypeConfiguration<ResearchCategory>
{
    public void Configure(EntityTypeBuilder<ResearchCategory> entity)
    {
        entity.HasOne(rc => rc.ParentCategory)
         .WithMany(rc => rc.SubCategories)
         .HasForeignKey(rc => rc.ParentCategoryId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
