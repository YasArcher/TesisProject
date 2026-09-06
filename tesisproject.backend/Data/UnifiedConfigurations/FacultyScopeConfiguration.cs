using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class FacultyScopeConfiguration : IEntityTypeConfiguration<FacultyScope>
{
    public void Configure(EntityTypeBuilder<FacultyScope> entity)
    {
        entity.HasKey(x => x.FacultyScopeId);

        entity.Property(x => x.Name)
         .HasMaxLength(200)
         .IsRequired();

        entity.HasIndex(x => x.Name).IsUnique();
        entity.HasIndex(x => x.IsActive);

        entity.HasMany(x => x.Faculties)
         .WithOne(x => x.FacultyScope)
         .HasForeignKey(x => x.FacultyScopeId)
         .OnDelete(DeleteBehavior.NoAction);

        entity.HasMany(x => x.UserAssignments)
         .WithOne(x => x.FacultyScope)
         .HasForeignKey(x => x.FacultyScopeId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
