using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ProjectExtensionConfiguration : IEntityTypeConfiguration<ProjectExtension>
{
    public void Configure(EntityTypeBuilder<ProjectExtension> entity)
    {
        entity.HasKey(x => x.ProjectExtensionId);

        entity.HasIndex(x => x.ProjectId);
        entity.HasIndex(x => x.ProjectExtensionTypeId);
        entity.HasIndex(x => x.DocumentId);

        entity.HasOne(x => x.Project)
         .WithMany(p => p.ProjectExtensions)
         .HasForeignKey(x => x.ProjectId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.ProjectExtensionType)
         .WithMany(t => t.ProjectExtensions)
         .HasForeignKey(x => x.ProjectExtensionTypeId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.Document)
         .WithMany()
         .HasForeignKey(x => x.DocumentId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
