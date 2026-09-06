using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ConvocationConfiguration : IEntityTypeConfiguration<Convocation>
{
    public void Configure(EntityTypeBuilder<Convocation> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.HasIndex(x => x.IsActive);
    }
}
