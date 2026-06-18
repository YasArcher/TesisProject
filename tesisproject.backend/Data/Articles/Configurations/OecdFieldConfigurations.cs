// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Articles.Entities;

namespace tesisproject.backend.Data.Articles.Configurations;

public sealed class BroadFieldConfiguration : IEntityTypeConfiguration<BroadField>
{
    public void Configure(EntityTypeBuilder<BroadField> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class SpecificFieldConfiguration : IEntityTypeConfiguration<SpecificField>
{
    public void Configure(EntityTypeBuilder<SpecificField> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Code).HasMaxLength(20);

        entity.HasOne(x => x.BroadField)
            .WithMany(b => b.SpecificFields)
            .HasForeignKey(x => x.BroadFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.BroadFieldId, x.Code })
            .HasDatabaseName("UQ_SpecificField_Broad_Code")
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL AND [Code] <> N''");

        entity.HasIndex(x => new { x.BroadFieldId, x.Name })
            .HasDatabaseName("UQ_SpecificField_Broad_Name")
            .IsUnique();
    }
}

public sealed class DetailedFieldConfiguration : IEntityTypeConfiguration<DetailedField>
{
    public void Configure(EntityTypeBuilder<DetailedField> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Code).HasMaxLength(20);

        entity.HasOne(x => x.SpecificField)
            .WithMany(s => s.DetailedFields)
            .HasForeignKey(x => x.SpecificFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.SpecificFieldId, x.Code })
            .HasDatabaseName("UQ_DetailedField_Specific_Code")
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL AND [Code] <> N''");

        entity.HasIndex(x => new { x.SpecificFieldId, x.Name })
            .HasDatabaseName("UQ_DetailedField_Specific_Name")
            .IsUnique();
    }
}

