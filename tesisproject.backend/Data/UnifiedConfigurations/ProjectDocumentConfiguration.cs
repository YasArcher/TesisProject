using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ProjectDocumentConfiguration : IEntityTypeConfiguration<ProjectDocument>
{
    public void Configure(EntityTypeBuilder<ProjectDocument> entity)
    {
        entity.HasKey(x => x.ProjectDocumentId);

        entity.HasOne(x => x.Project)
         .WithMany(p => p.ProjectDocuments)
         .HasForeignKey(x => x.ProjectId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.Document)
         .WithMany(d => d.ProjectDocuments)
         .HasForeignKey(x => x.DocumentId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(x => x.ProjectId);
        entity.HasIndex(x => x.DocumentId);

        entity.HasIndex(x => new { x.ProjectId, x.DocumentId }).IsUnique();
    }
}
