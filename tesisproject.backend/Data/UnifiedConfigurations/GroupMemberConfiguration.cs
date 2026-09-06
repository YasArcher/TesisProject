using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Auth;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> entity)
    {
        entity.HasIndex(x => x.UserId);
        entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).HasPrincipalKey(x => x.IdUser).OnDelete(DeleteBehavior.NoAction);
    }
}
