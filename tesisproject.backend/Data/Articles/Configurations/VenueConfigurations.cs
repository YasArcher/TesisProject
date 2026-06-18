// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Articles.Entities;

namespace tesisproject.backend.Data.Articles.Configurations;

public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Type).HasMaxLength(20).HasDefaultValue("Journal").IsRequired();
        entity.Property(x => x.IssnCode).HasMaxLength(20);
        entity.Property(x => x.IssueNumber).HasMaxLength(20);
        entity.Property(x => x.VolumeNumber).HasMaxLength(20);
        entity.Property(x => x.JournalUrl).HasMaxLength(400);

        entity.HasIndex(x => new { x.Name, x.IssnCode })
            .HasDatabaseName("UQ_Venue_Name_Issn")
            .IsUnique()
            .HasFilter("[IssnCode] IS NOT NULL");
    }
}

public sealed class VenueMetricConfiguration : IEntityTypeConfiguration<VenueMetric>
{
    public void Configure(EntityTypeBuilder<VenueMetric> entity)
    {
        entity.HasKey(x => new { x.VenueId, x.Year });
        entity.Property(x => x.Quartile).HasMaxLength(10);
        entity.Property(x => x.SJR).HasPrecision(6, 3);

        entity.HasOne(x => x.Venue)
            .WithMany(v => v.VenueMetrics)
            .HasForeignKey(x => x.VenueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

