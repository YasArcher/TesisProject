using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Articles;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class FacultyScopeFacultyConfiguration : IEntityTypeConfiguration<FacultyScopeFaculty>
{
    public void Configure(EntityTypeBuilder<FacultyScopeFaculty> entity)
    {
        entity.HasKey(x => new { x.FacultyScopeId, x.FacultyId });

        entity.Property(x => x.FacultyId).IsRequired();

        entity.HasIndex(x => x.FacultyId);
        entity.HasIndex(x => x.IsActive);

        entity.HasOne<Faculty>().WithMany().HasForeignKey(x => x.FacultyId);
    }
}
