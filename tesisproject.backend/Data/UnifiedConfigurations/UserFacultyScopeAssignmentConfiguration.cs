using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class UserFacultyScopeAssignmentConfiguration : IEntityTypeConfiguration<UserFacultyScopeAssignment>
{
    public void Configure(EntityTypeBuilder<UserFacultyScopeAssignment> entity)
    {
        entity.HasKey(x => new { x.IdentityUserId, x.FacultyScopeId });

        entity.HasIndex(x => x.IdentityUserId);
        entity.HasIndex(x => x.FacultyScopeId);
        entity.HasIndex(x => x.IsActive);

        entity.HasOne(x => x.FacultyScope)
         .WithMany(s => s.UserAssignments)
         .HasForeignKey(x => x.FacultyScopeId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.User)
         .WithMany()
         .HasForeignKey(x => x.IdentityUserId)
         .HasPrincipalKey(u => u.IdUser)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
