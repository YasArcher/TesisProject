using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Entities;

namespace tesisproject.backend.Data.Configurations;

public sealed class ReportingPerformanceMetricConfiguration : IEntityTypeConfiguration<ReportingPerformanceMetric>
{
    public void Configure(EntityTypeBuilder<ReportingPerformanceMetric> entity)
    {
        entity.Property(x => x.Operation).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Module).HasMaxLength(80).IsRequired();
        entity.Property(x => x.UserId).HasMaxLength(450);
        entity.Property(x => x.UserName).HasMaxLength(256);
        entity.Property(x => x.Roles).HasMaxLength(500);
        entity.Property(x => x.FilterSummaryJson).HasMaxLength(4000);
        entity.Property(x => x.ErrorMessage).HasMaxLength(1000);
        entity.HasIndex(x => x.StartedAtUtc);
        entity.HasIndex(x => x.Operation);
        entity.HasIndex(x => x.UserId);
        entity.HasIndex(x => new { x.Operation, x.StartedAtUtc });
    }
}
