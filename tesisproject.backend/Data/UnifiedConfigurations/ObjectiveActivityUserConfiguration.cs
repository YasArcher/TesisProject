using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ObjectiveActivityUserConfiguration : IEntityTypeConfiguration<ObjectiveActivityUser>
{
    public void Configure(EntityTypeBuilder<ObjectiveActivityUser> entity)
    {
        entity.HasIndex(x => new { x.ObjectiveActivityId, x.UserId, x.VisitId }).IsUnique();

        entity.HasIndex(x => x.UserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);
    }
}
