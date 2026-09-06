using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Articles;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class AcademicTermConfiguration : IEntityTypeConfiguration<AcademicTerm>
{
    public void Configure(EntityTypeBuilder<AcademicTerm> entity)
    {
        entity.HasIndex(x => x.ExternalPeriodId).IsUnique().HasFilter("[ExternalPeriodId] IS NOT NULL");
        entity.Property(x => x.StartDate).HasColumnType("date");
        entity.Property(x => x.EndDate).HasColumnType("date");
        entity.ToTable("AcademicTerms", t => t.HasCheckConstraint("CK_AcademicTerms_Dates",
            "([StartDate] IS NULL AND [EndDate] IS NULL) OR ([StartDate] IS NOT NULL AND [EndDate] IS NOT NULL AND [StartDate] <= [EndDate])"));

        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.HasIndex(x => x.Name);
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

public sealed class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> entity)
    {
        entity.HasIndex(x => x.ExternalFacultyId).IsUnique().HasFilter("[ExternalFacultyId] IS NOT NULL");
        entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2");

        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Acronym).HasMaxLength(40);
        entity.Property(x => x.IsActive).HasDefaultValue(true);
        entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        entity.HasIndex(x => x.Name);

    }
}
