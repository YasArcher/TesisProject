using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Reporting.Models;

namespace tesisproject.backend.Reporting.Data;

public sealed class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options)
    {
    }

    public DbSet<ReportingEtlRunRow> EtlRuns => Set<ReportingEtlRunRow>();
    public DbSet<ScientificProductionKpiRow> ScientificProductionKpis => Set<ScientificProductionKpiRow>();
    public DbSet<LoadQualityKpiRow> LoadQualityKpis => Set<LoadQualityKpiRow>();
    public DbSet<WorkflowKpiRow> WorkflowKpis => Set<WorkflowKpiRow>();
    public DbSet<ArticlesByYearRow> ArticlesByYear => Set<ArticlesByYearRow>();
    public DbSet<IndexingSourceSummaryRow> ArticlesByIndexingSource => Set<IndexingSourceSummaryRow>();
    public DbSet<WorkflowCurrentStageRow> WorkflowCurrentStages => Set<WorkflowCurrentStageRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportingEtlRunRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("EtlRun", "etl", t => t.ExcludeFromMigrations());
        });

        modelBuilder.Entity<ScientificProductionKpiRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_KPI_ProduccionCientifica", "dw");
        });

        modelBuilder.Entity<LoadQualityKpiRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_KPI_CalidadCarga", "dw");
        });

        modelBuilder.Entity<WorkflowKpiRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_KPI_Workflow", "dw");
            entity.Property(x => x.AvgStageDurationSeconds).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ArticlesByYearRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_Articles_ByYear", "dw");
        });

        modelBuilder.Entity<IndexingSourceSummaryRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_Articles_ByIndexingSource", "dw");
        });

        modelBuilder.Entity<WorkflowCurrentStageRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_Workflow_Batches_ByCurrentStage", "dw");
        });
    }
}
