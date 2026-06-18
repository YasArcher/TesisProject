using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Entities;

namespace tesisproject.backend.Data.Configurations;

public sealed class AcademicTermConfiguration : IEntityTypeConfiguration<AcademicTerm>
{
    public void Configure(EntityTypeBuilder<AcademicTerm> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class PublicationStatusConfiguration : IEntityTypeConfiguration<PublicationStatus>
{
    public void Configure(EntityTypeBuilder<PublicationStatus> entity)
    {
        entity.HasKey(x => x.PublicationStatusId);
        entity.Property(x => x.Name).HasMaxLength(20).IsRequired();
        entity.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class ResearchLineConfiguration : IEntityTypeConfiguration<ResearchLine>
{
    public void Configure(EntityTypeBuilder<ResearchLine> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class IndexingSourceConfiguration : IEntityTypeConfiguration<IndexingSource>
{
    public void Configure(EntityTypeBuilder<IndexingSource> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
        entity.Property(x => x.IsActive).HasDefaultValue(true);
        entity.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Code).HasMaxLength(40);
        entity.Property(x => x.IsActive).HasDefaultValue(true);
        entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        entity.HasIndex(x => x.Name).IsUnique();
        entity.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL AND [Code] <> N''");
    }
}
