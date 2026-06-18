using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Entities;

namespace tesisproject.backend.Data.Configurations;

public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> entity)
    {
        entity.ToTable("ImportBatch");
        entity.HasKey(x => x.ImportBatchId);
        entity.Property(x => x.BatchCode).HasMaxLength(100).IsRequired();
        entity.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
        entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        entity.Property(x => x.FileName).HasMaxLength(260);
        entity.Property(x => x.SourceReference).HasMaxLength(300);
        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        entity.Property(x => x.CreatedBy).HasMaxLength(150);
        entity.Property(x => x.Notes).HasMaxLength(500);

        entity.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasMany(x => x.Rows)
            .WithOne(x => x.Batch)
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(x => x.Errors)
            .WithOne(x => x.Batch)
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ImportBatchRowConfiguration : IEntityTypeConfiguration<ImportBatchRow>
{
    public void Configure(EntityTypeBuilder<ImportBatchRow> entity)
    {
        entity.ToTable("ImportBatchRow");
        entity.HasKey(x => x.ImportBatchRowId);
        entity.Property(x => x.RowStatus).HasMaxLength(30).IsRequired();

        entity.HasMany(x => x.Values)
            .WithOne(x => x.Row)
            .HasForeignKey(x => x.ImportBatchRowId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(x => x.Errors)
            .WithOne(x => x.Row)
            .HasForeignKey(x => x.ImportBatchRowId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(x => new { x.ImportBatchId, x.RowNumber });
        entity.HasIndex(x => new { x.ImportBatchId, x.RowStatus });
    }
}

public sealed class ImportBatchRowValueConfiguration : IEntityTypeConfiguration<ImportBatchRowValue>
{
    public void Configure(EntityTypeBuilder<ImportBatchRowValue> entity)
    {
        entity.ToTable("ImportBatchRowValue");
        entity.HasKey(x => x.ImportBatchRowValueId);
        entity.Property(x => x.ValueType).HasMaxLength(50);
        entity.Property(x => x.ValidationMessage).HasMaxLength(500);

        entity.HasOne(x => x.Field)
            .WithMany()
            .HasForeignKey(x => x.FieldId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.ImportBatchRowId);
        entity.HasIndex(x => new { x.ImportBatchRowId, x.FieldId });
    }
}

public sealed class ImportBatchErrorConfiguration : IEntityTypeConfiguration<ImportBatchError>
{
    public void Configure(EntityTypeBuilder<ImportBatchError> entity)
    {
        entity.ToTable("ImportBatchError");
        entity.HasKey(x => x.ImportBatchErrorId);
        entity.Property(x => x.ErrorCode).HasMaxLength(100).IsRequired();
        entity.Property(x => x.ErrorMessage).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Severity).HasMaxLength(20).IsRequired();

        entity.HasOne(x => x.Field)
            .WithMany()
            .HasForeignKey(x => x.FieldId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(x => x.ImportBatchId);
        entity.HasIndex(x => x.ImportBatchRowId);
        entity.HasIndex(x => new { x.ImportBatchId, x.Severity });
    }
}
