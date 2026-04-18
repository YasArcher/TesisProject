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
    public DbSet<ReportingArticleDetailRow> ArticleDetails => Set<ReportingArticleDetailRow>();
    public DbSet<ReportingQuartileDistributionRow> QuartileDistribution => Set<ReportingQuartileDistributionRow>();
    public DbSet<ReportingVenueMetricRow> VenueMetricsByYear => Set<ReportingVenueMetricRow>();
    public DbSet<ReportingArticleAuthorSummaryRow> ArticleAuthorSummaries => Set<ReportingArticleAuthorSummaryRow>();
    public DbSet<ReportingArticleIndexingDetailRow> ArticleIndexingDetails => Set<ReportingArticleIndexingDetailRow>();

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

        modelBuilder.Entity<ReportingArticleDetailRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_Articles_Detail", "dw");
        });

        modelBuilder.Entity<ReportingQuartileDistributionRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_QuartileDistribution", "dw");
        });

        modelBuilder.Entity<ReportingVenueMetricRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_VenueMetrics_ByYear", "dw");
            entity.Property(x => x.SJR).HasPrecision(18, 3);
            entity.Property(x => x.CiteScore).HasPrecision(18, 3);
        });

        modelBuilder.Entity<ReportingArticleAuthorSummaryRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
        });

        modelBuilder.Entity<ReportingArticleIndexingDetailRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
        });
    }
}
