using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Entities;

namespace tesisproject.backend.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.Property(x => x.Action).HasMaxLength(120).IsRequired();
        entity.Property(x => x.EntityName).HasMaxLength(120);
        entity.Property(x => x.EntityId).HasMaxLength(120);
        entity.Property(x => x.Detail).HasMaxLength(4000);
    }
}

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> entity)
    {
        entity.Property(x => x.Code).IsRequired();
        entity.Property(x => x.Name).IsRequired();
    }
}
