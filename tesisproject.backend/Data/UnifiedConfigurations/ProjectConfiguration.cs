using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.backend.Data.UnifiedEntities.Articles;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> entity)
    {
        entity
                           .HasMany(p => p.Budgets)
                           .WithOne(b => b.Project)
                           .HasForeignKey(b => b.ProjectId)
                           .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne<Faculty>().WithMany().HasForeignKey(x => x.FacultyId);

        entity.HasIndex(x => x.CreatedByUserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);
    }
}
