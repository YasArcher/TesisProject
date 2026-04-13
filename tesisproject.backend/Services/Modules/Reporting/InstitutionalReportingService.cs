using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Reporting.Data;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Services.Modules.Reporting;

public sealed class InstitutionalReportingService : IInstitutionalReportingService
{
    private readonly ReportingDbContext _db;
    private readonly ILogger<InstitutionalReportingService> _logger;

    public InstitutionalReportingService(
        ReportingDbContext db,
        ILogger<InstitutionalReportingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReportingHealthDto> GetHealthAsync(CancellationToken ct = default)
    {
        var health = new ReportingHealthDto
        {
            CanConnect = true,
            DatabaseName = _db.Database.GetDbConnection().Database
        };

        try
        {
            var canConnect = await _db.Database.CanConnectAsync(ct);
            health.CanConnect = canConnect;

            if (!canConnect)
            {
                health.LastEtlStatus = "Sin conexión";
                health.LastEtlNotes = "No fue posible conectar con la base analítica configurada.";
                return health;
            }

            var lastRun = await SafeFirstOrDefaultAsync(
                _db.EtlRuns.AsNoTracking().OrderByDescending(x => x.EtlRunId),
                "etl.EtlRun",
                ct);

            health.LastEtlStatus = lastRun?.Status ?? "Sin ejecuciones";
            health.LastEtlStartedAt = lastRun?.StartedAt;
            health.LastEtlFinishedAt = lastRun?.FinishedAt;
            health.LastEtlNotes = lastRun?.Notes;
            health.DimDateRows = await SafeCountAsync("dw.DimDate", ct);
            health.ArticleRows = await SafeCountAsync("dw.FactArticlePublication", ct);
            health.BatchRows = await SafeCountAsync("dw.FactRegistrationBatch", ct);
            health.WorkflowStageRows = await SafeCountAsync("dw.FactWorkflowStage", ct);

            return health;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar la salud del DW de reportería.");
            health.CanConnect = false;
            health.LastEtlStatus = "Error";
            health.LastEtlNotes = "La conexión de reportería respondió con error. Revise los logs del backend.";
            return health;
        }
    }

    public async Task<InstitutionalReportingDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var health = await GetHealthAsync(ct);
        var production = await SafeFirstOrDefaultAsync(
            _db.ScientificProductionKpis.AsNoTracking(),
            "dw.vw_KPI_ProduccionCientifica",
            ct);
        var loadQuality = await SafeFirstOrDefaultAsync(
            _db.LoadQualityKpis.AsNoTracking(),
            "dw.vw_KPI_CalidadCarga",
            ct);
        var workflow = await SafeFirstOrDefaultAsync(
            _db.WorkflowKpis.AsNoTracking(),
            "dw.vw_KPI_Workflow",
            ct);
        var byYear = await SafeListAsync(
            _db.ArticlesByYear.AsNoTracking().OrderBy(x => x.YearNumber),
            "dw.vw_Articles_ByYear",
            ct);
        var indexing = await SafeListAsync(
            _db.ArticlesByIndexingSource.AsNoTracking().OrderByDescending(x => x.TotalArticles),
            "dw.vw_Articles_ByIndexingSource",
            ct);
        var workflowStages = await SafeListAsync(
            _db.WorkflowCurrentStages.AsNoTracking().OrderByDescending(x => x.BatchId_OLTP).Take(25),
            "dw.vw_Workflow_Batches_ByCurrentStage",
            ct);

        return new InstitutionalReportingDashboardDto
        {
            Health = health,
            ScientificProduction = new ScientificProductionKpiDto
            {
                TotalArticles = production?.TotalArticles ?? 0,
                OpenAccessArticles = production?.OpenAccessArticles ?? 0,
                ProjectResultArticles = production?.ProjectResultArticles ?? 0,
                InterculturalArticles = production?.InterculturalArticles ?? 0
            },
            LoadQuality = new LoadQualityKpiDto
            {
                TotalBatches = loadQuality?.TotalBatches ?? 0,
                TotalRows = loadQuality?.TotalRows ?? 0,
                SuccessfulRows = loadQuality?.SuccessfulRows ?? 0,
                ErrorRows = loadQuality?.ErrorRows ?? 0
            },
            Workflow = new WorkflowKpiDto
            {
                TotalStageExecutions = workflow?.TotalStageExecutions ?? 0,
                AvgStageDurationSeconds = workflow?.AvgStageDurationSeconds,
                ApprovedStages = workflow?.ApprovedStages ?? 0,
                ReturnedStages = workflow?.ReturnedStages ?? 0
            },
            ArticlesByYear = byYear
                .Select(x => new ArticlesByYearDto
                {
                    Year = x.YearNumber,
                    Count = x.TotalArticles
                })
                .ToList(),
            ArticlesByIndexingSource = indexing
                .Select(x => new IndexingSourceSummaryDto
                {
                    IndexingSourceName = x.IndexingSourceName,
                    TotalArticles = x.TotalArticles
                })
                .ToList(),
            WorkflowCurrentStages = workflowStages
                .Select(x => new WorkflowCurrentStageDto
                {
                    BatchId = x.BatchId_OLTP,
                    WorkflowName = x.WorkflowName,
                    StageName = x.StageName,
                    StageGroupName = x.StageGroupName,
                    StageStatus = x.StageStatus,
                    StartDateKey = x.StartDateKey,
                    EndDateKey = x.EndDateKey,
                    StageDurationSeconds = x.StageDurationSeconds,
                    Approved = x.ApprovedFlag,
                    Returned = x.ReturnedFlag
                })
                .ToList()
        };
    }

    public async Task<ReportingHealthDto> RunFullLoadAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync("EXEC etl.sp_RunFullLoad;", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible ejecutar el ETL completo del DW de reportería.");
            throw;
        }

        return await GetHealthAsync(ct);
    }

    private async Task<int> SafeCountAsync(string tableName, CancellationToken ct)
    {
        try
        {
            return tableName switch
            {
                "dw.DimDate" => await _db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dw.DimDate").SingleAsync(ct),
                "dw.FactArticlePublication" => await _db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dw.FactArticlePublication").SingleAsync(ct),
                "dw.FactRegistrationBatch" => await _db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dw.FactRegistrationBatch").SingleAsync(ct),
                "dw.FactWorkflowStage" => await _db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dw.FactWorkflowStage").SingleAsync(ct),
                _ => 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No fue posible contar filas de {ReportingObject}.", tableName);
            return 0;
        }
    }

    private async Task<T?> SafeFirstOrDefaultAsync<T>(IQueryable<T> query, string objectName, CancellationToken ct)
    {
        try
        {
            return await query.FirstOrDefaultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No fue posible leer {ReportingObject}. Se devolverá valor vacío.", objectName);
            return default;
        }
    }

    private async Task<List<T>> SafeListAsync<T>(IQueryable<T> query, string objectName, CancellationToken ct)
    {
        try
        {
            return await query.ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No fue posible leer {ReportingObject}. Se devolverá lista vacía.", objectName);
            return new List<T>();
        }
    }
}
