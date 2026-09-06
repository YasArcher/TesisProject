using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ProjectExtensionTypeConfiguration : IEntityTypeConfiguration<ProjectExtensionType>
{
    public void Configure(EntityTypeBuilder<ProjectExtensionType> entity)
    {
        entity.Property(x => x.Name)
         .HasMaxLength(200)
         .IsRequired();

        entity.HasIndex(x => x.Name).IsUnique();
    }
}
