using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.UnifiedEntities.Administration;
using tesisproject.backend.Services.Unified.Contracts.Administration;

namespace tesisproject.backend.Data.UnifiedConfigurations;

public sealed class OperationExecutionHistoryConfiguration : IEntityTypeConfiguration<OperationExecutionHistory>
{
    public void Configure(EntityTypeBuilder<OperationExecutionHistory> entity)
    {
        entity.ToTable("OperationExecutionHistory", table =>
        {
            table.HasCheckConstraint("CK_OperationExecutionHistory_ResultJson_IsJson",
                "[ResultJson] IS NULL OR ISJSON([ResultJson]) = 1");
            table.HasCheckConstraint("CK_OperationExecutionHistory_OperationType",
                "[OperationType] IN ('DATA_MIGRATION','BULK_IMPORT','SYNCHRONIZATION','ETL','BACKGROUND_JOB','MANUAL_PROCESS')");
            table.HasCheckConstraint("CK_OperationExecutionHistory_Status",
                "[Status] IN ('RUNNING','SUCCEEDED','PARTIALLY_SUCCEEDED','FAILED')");
        });
        entity.HasKey(x => x.Id);
        entity.Property(x => x.ExecutionId).IsRequired();
        entity.Property(x => x.OperationType).HasMaxLength(40).IsRequired();
        entity.Property(x => x.OperationCode).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Version).HasMaxLength(40);
        entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
        entity.Property(x => x.StartedAt).HasColumnType("datetime2").IsRequired();
        entity.Property(x => x.CompletedAt).HasColumnType("datetime2");
        entity.Property(x => x.ExecutedByName).HasMaxLength(200);
        entity.Property(x => x.Source).HasMaxLength(200);
        entity.Property(x => x.FileName).HasMaxLength(260);
        entity.Property(x => x.FileHash).HasMaxLength(64);
        entity.Property(x => x.ResultJson).HasColumnType("nvarchar(max)");
        entity.Property(x => x.ErrorCode).HasMaxLength(120);
        entity.Property(x => x.ErrorMessage).HasMaxLength(1000);

        entity.HasIndex(x => x.ExecutionId).IsUnique();
        entity.HasIndex(x => new { x.OperationType, x.OperationCode });
        entity.HasIndex(x => x.Status);
        entity.HasIndex(x => x.StartedAt);
        entity.HasIndex(x => x.FileHash);
        entity.HasIndex(x => x.FileHash)
            .IsUnique()
            .HasFilter($"[OperationType] = '{OperationExecutionTypes.BulkImport}' AND [OperationCode] = '{OperationCodes.ProjectsMatrixImport}' AND [Status] = '{OperationExecutionStatuses.Succeeded}' AND [FileHash] IS NOT NULL")
            .HasDatabaseName("UX_OperationExecutionHistory_ProjectsMatrix_SucceededHash");
        entity.HasIndex(x => x.OperationCode)
            .IsUnique()
            .HasFilter($"[OperationType] = '{OperationExecutionTypes.DataMigration}' AND [Status] = '{OperationExecutionStatuses.Succeeded}'")
            .HasDatabaseName("UX_OperationExecutionHistory_DataMigration_Succeeded");
    }
}
