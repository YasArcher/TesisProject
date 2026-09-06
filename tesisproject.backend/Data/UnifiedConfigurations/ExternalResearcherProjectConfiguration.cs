using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ExternalResearcherProjectConfiguration : IEntityTypeConfiguration<ExternalResearcherProject>
{
    public void Configure(EntityTypeBuilder<ExternalResearcherProject> entity)
    {
        entity.HasIndex(x => x.CreatedByUserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);
    }
}
