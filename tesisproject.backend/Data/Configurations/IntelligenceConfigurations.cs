using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Entities;

namespace tesisproject.backend.Data.Configurations;

public sealed class IntelligenceTrainingRunConfiguration : IEntityTypeConfiguration<IntelligenceTrainingRun>
{
    public void Configure(EntityTypeBuilder<IntelligenceTrainingRun> entity)
    {
        entity.Property(x => x.Trigger).HasMaxLength(60).IsRequired();
        entity.Property(x => x.Status).HasMaxLength(60).IsRequired();
        entity.Property(x => x.ActiveModelVersion).HasMaxLength(160).IsRequired();
        entity.Property(x => x.PromotedAlgorithm).HasMaxLength(160).IsRequired();
        entity.Property(x => x.SelectionReason).HasMaxLength(800).IsRequired();
        entity.Property(x => x.Summary).HasMaxLength(800).IsRequired();
        entity.Property(x => x.DatasetName).HasMaxLength(180).IsRequired();
        entity.Property(x => x.Target).HasMaxLength(180).IsRequired();
        entity.Property(x => x.ValidationStrategy).HasMaxLength(800).IsRequired();
        entity.Property(x => x.FeatureWindow).HasMaxLength(120).IsRequired();
        entity.Property(x => x.BestAlgorithm).HasMaxLength(160).IsRequired();
        entity.Property(x => x.BestMetric).HasMaxLength(40).IsRequired();
        entity.Property(x => x.BestMetricValue).HasPrecision(18, 4);
        entity.Property(x => x.RetrainingPolicy).HasMaxLength(800).IsRequired();
        entity.Property(x => x.CreatedByUserId).HasMaxLength(450);
        entity.Property(x => x.CreatedBy).HasMaxLength(256);
        entity.HasIndex(x => x.RunId).IsUnique();
        entity.HasIndex(x => x.StartedAt);
        entity.HasIndex(x => x.ActiveModelVersion);
    }
}

public sealed class IntelligenceTrainingAlgorithmMetricConfiguration : IEntityTypeConfiguration<IntelligenceTrainingAlgorithmMetric>
{
    public void Configure(EntityTypeBuilder<IntelligenceTrainingAlgorithmMetric> entity)
    {
        entity.Property(x => x.Algorithm).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Family).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Purpose).HasMaxLength(600).IsRequired();
        entity.Property(x => x.MetricName).HasMaxLength(40).IsRequired();
        entity.Property(x => x.Mae).HasPrecision(18, 4);
        entity.Property(x => x.Rmse).HasPrecision(18, 4);
        entity.Property(x => x.Mape).HasPrecision(18, 4);
        entity.Property(x => x.Score).HasPrecision(18, 4);
        entity.Property(x => x.Status).HasMaxLength(60).IsRequired();
        entity.Property(x => x.ThesisUse).HasMaxLength(800).IsRequired();

        entity.HasOne(x => x.TrainingRun)
            .WithMany(x => x.AlgorithmMetrics)
            .HasForeignKey(x => x.IntelligenceTrainingRunId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.IntelligenceTrainingRunId, x.IsBest });
    }
}
