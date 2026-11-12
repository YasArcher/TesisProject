using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Articles;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class ArticleStagingBatchConfiguration : IEntityTypeConfiguration<ArticleStagingBatch>
{
    public void Configure(EntityTypeBuilder<ArticleStagingBatch> entity)
    {
        entity.ToTable("ArticleStagingBatch");
        entity.HasKey(x => x.ArticleStagingBatchId);
        entity.Property(x => x.CreatedByUserId).HasMaxLength(450);
        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        entity.HasIndex(x => x.RegistrationMatrixId).IsUnique();
        entity.HasIndex(x => new { x.CreatedByUserId, x.CreatedAt });
        entity.HasOne(x => x.Matrix).WithMany().HasForeignKey(x => x.RegistrationMatrixId).OnDelete(DeleteBehavior.NoAction);
        entity.HasMany(x => x.Rows).WithOne(x => x.Batch).HasForeignKey(x => x.ArticleStagingBatchId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ArticleStagingRowConfiguration : IEntityTypeConfiguration<ArticleStagingRow>
{
    public void Configure(EntityTypeBuilder<ArticleStagingRow> entity)
    {
        entity.ToTable("ArticleStagingRow");
        entity.HasKey(x => x.ArticleStagingRowId);
        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        entity.Property(x => x.DoiNormalized).HasMaxLength(200);
        entity.Property(x => x.ValidationMessage).HasMaxLength(1000);
        entity.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        entity.HasIndex(x => new { x.ArticleStagingBatchId, x.RegistrationMatrixRowId }).IsUnique();
        entity.HasIndex(x => new { x.ArticleStagingBatchId, x.RowNumber }).IsUnique();
        entity.HasIndex(x => x.DoiNormalized).HasFilter("[DoiNormalized] IS NOT NULL");
    }
}
