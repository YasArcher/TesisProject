using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using tesisproject.backend.Data.Entities;

namespace tesisproject.backend.Data.Configurations;

public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> entity)
    {
        entity.ToTable("WorkflowDefinition");
        entity.HasKey(x => x.WorkflowDefinitionId);
        entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
        entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(500);
        entity.Property(x => x.IsActive).HasDefaultValue(true);
        entity.HasIndex(x => x.Key).IsUnique();
    }
}

public sealed class WorkflowStageDefinitionConfiguration : IEntityTypeConfiguration<WorkflowStageDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowStageDefinition> entity)
    {
        entity.ToTable("WorkflowStageDefinition");
        entity.HasKey(x => x.WorkflowStageDefinitionId);
        entity.Property(x => x.StageKey).HasMaxLength(100).IsRequired();
        entity.Property(x => x.StageName).HasMaxLength(150).IsRequired();
        entity.Property(x => x.StageGroupKey).HasMaxLength(100);
        entity.Property(x => x.StageGroupName).HasMaxLength(150);
        entity.Property(x => x.IsActive).HasDefaultValue(true);
        entity.Property(x => x.CanEditData).HasDefaultValue(false);
        entity.Property(x => x.CanReturn).HasDefaultValue(true);
        entity.Property(x => x.CanApprove).HasDefaultValue(true);
        entity.Property(x => x.CanProcessBatch).HasDefaultValue(false);
        entity.Property(x => x.IsFinalStage).HasDefaultValue(false);

        entity.HasOne(x => x.WorkflowDefinition)
            .WithMany(x => x.Stages)
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.WorkflowDefinitionId, x.DisplayOrder }).IsUnique();
        entity.HasIndex(x => new { x.WorkflowDefinitionId, x.StageKey }).IsUnique();
    }
}

public sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> entity)
    {
        entity.ToTable("WorkflowInstance");
        entity.HasKey(x => x.WorkflowInstanceId);
        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();

        entity.HasOne(x => x.WorkflowDefinition)
            .WithMany(x => x.Instances)
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Batch)
            .WithOne(x => x.WorkflowInstance)
            .HasForeignKey<WorkflowInstance>(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.CurrentStageDefinition)
            .WithMany()
            .HasForeignKey(x => x.CurrentStageDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.SubmittedByUser)
            .WithMany()
            .HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(x => x.ImportBatchId).IsUnique();
    }
}

public sealed class WorkflowStageInstanceConfiguration : IEntityTypeConfiguration<WorkflowStageInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowStageInstance> entity)
    {
        entity.ToTable("WorkflowStageInstance");
        entity.HasKey(x => x.WorkflowStageInstanceId);
        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        entity.Property(x => x.Notes).HasMaxLength(1000);

        entity.HasOne(x => x.WorkflowInstance)
            .WithMany(x => x.StageInstances)
            .HasForeignKey(x => x.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.WorkflowStageDefinition)
            .WithMany(x => x.StageInstances)
            .HasForeignKey(x => x.WorkflowStageDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.AssignedToUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(x => new { x.WorkflowInstanceId, x.WorkflowStageDefinitionId }).IsUnique();
    }
}

public sealed class WorkflowActionLogConfiguration : IEntityTypeConfiguration<WorkflowActionLog>
{
    public void Configure(EntityTypeBuilder<WorkflowActionLog> entity)
    {
        entity.ToTable("WorkflowActionLog");
        entity.HasKey(x => x.WorkflowActionLogId);
        entity.Property(x => x.ActionType).HasMaxLength(50).IsRequired();
        entity.Property(x => x.FromStatus).HasMaxLength(30);
        entity.Property(x => x.ToStatus).HasMaxLength(30);
        entity.Property(x => x.Comments).HasMaxLength(2000);

        entity.HasOne(x => x.WorkflowInstance)
            .WithMany(x => x.ActionLogs)
            .HasForeignKey(x => x.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.WorkflowStageInstance)
            .WithMany(x => x.ActionLogs)
            .HasForeignKey(x => x.WorkflowStageInstanceId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
