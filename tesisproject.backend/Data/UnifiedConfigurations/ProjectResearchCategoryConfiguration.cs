using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ProjectResearchCategoryConfiguration : IEntityTypeConfiguration<ProjectResearchCategory>
{
    public void Configure(EntityTypeBuilder<ProjectResearchCategory> entity)
    {
        entity.HasKey(prc => prc.ProjectResearchCategoryId);

        entity.HasIndex(prc => new { prc.ProjectId, prc.ResearchCategoryId })
         .IsUnique();

        entity.HasOne(prc => prc.Project)
         .WithMany(p => p.ProjectResearchCategories)
         .HasForeignKey(prc => prc.ProjectId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(prc => prc.ResearchCategory)
         .WithMany(rc => rc.ProjectResearchCategories)
         .HasForeignKey(prc => prc.ResearchCategoryId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
