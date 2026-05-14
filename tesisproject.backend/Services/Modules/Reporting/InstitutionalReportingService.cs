using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.Threading;
using tesisproject.backend.Reporting.Data;
using tesisproject.backend.Reporting.Models;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.Services.Modules.Reporting;

public sealed class InstitutionalReportingService : IInstitutionalReportingService
{
    private readonly ReportingDbContext _db;
    private readonly ILogger<InstitutionalReportingService> _logger;
    private readonly IMemoryCache _cache;
    private static int _cacheVersion;

    static InstitutionalReportingService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public InstitutionalReportingService(
        ReportingDbContext db,
        ILogger<InstitutionalReportingService> logger,
        IMemoryCache cache)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
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

    public async Task<InstitutionalReportingDashboardDto> GetDashboardAsync(
        InstitutionalReportingFilterDto? filter = null,
        CancellationToken ct = default)
    {
        var cacheKey = ReportingCacheKeyBuilder.BuildDashboardKey(filter, Volatile.Read(ref _cacheVersion));
        if (_cache.TryGetValue(cacheKey, out InstitutionalReportingDashboardDto? cached) && cached is not null)
        {
            return cached;
        }

        var dashboard = await BuildDashboardAsync(filter, ct);
        _cache.Set(cacheKey, dashboard, TimeSpan.FromMinutes(5));
        return dashboard;
    }

    public async Task<AuthorReportingDashboardDto> GetAuthorDashboardAsync(
        InstitutionalReportingFilterDto? filter = null,
        CancellationToken ct = default)
    {
        var cacheKey = ReportingCacheKeyBuilder.BuildAuthorKey(filter, Volatile.Read(ref _cacheVersion));
        if (_cache.TryGetValue(cacheKey, out AuthorReportingDashboardDto? cached) && cached is not null)
        {
            return cached;
        }

        var dashboard = await BuildAuthorDashboardAsync(filter, ct);
        _cache.Set(cacheKey, dashboard, TimeSpan.FromMinutes(5));
        return dashboard;
    }

    private async Task<InstitutionalReportingDashboardDto> BuildDashboardAsync(
        InstitutionalReportingFilterDto? filter,
        CancellationToken ct)
    {
        var dashboardWatch = Stopwatch.StartNew();
        var stepWatch = Stopwatch.StartNew();
        var health = await GetHealthAsync(ct);
        LogDashboardStep("salud DW", stepWatch);
        var detailsQuery = ReportingFilterApplicator.ApplyArticleDetailFilters(
            _db.ArticleDetails.AsNoTracking(),
            filter,
            _db.VenueMetricsByYear.AsNoTracking());
        var details = await SafeListAsync(detailsQuery, "dw.vw_Articles_Detail", ct);
        LogDashboardStep("detalle artículos", stepWatch);
        var authorRows = await GetCachedListAsync(
            "author-dashboard-rows",
            LoadAuthorDashboardRowsAsync,
            ct);
        LogDashboardStep("autores ligeros", stepWatch);
        var periodDateSelector = BuildPeriodDateSelector(filter);
        var loadQuality = await SafeFirstOrDefaultAsync(_db.LoadQualityKpis.AsNoTracking(), "dw.vw_KPI_CalidadCarga", ct);
        LogDashboardStep("KPI calidad", stepWatch);
        var workflow = await SafeFirstOrDefaultAsync(_db.WorkflowKpis.AsNoTracking(), "dw.vw_KPI_Workflow", ct);
        LogDashboardStep("KPI workflow", stepWatch);
        var venueMetrics = await GetCachedListAsync(
            "venue-metrics-by-year",
            LoadVenueMetricsAsync,
            ct);
        LogDashboardStep("métricas revistas", stepWatch);
        var workflowStages = await SafeListAsync(
            _db.WorkflowCurrentStages.AsNoTracking().OrderByDescending(x => x.BatchId_OLTP).Take(25),
            "dw.vw_Workflow_Batches_ByCurrentStage",
            ct);
        LogDashboardStep("etapas workflow", stepWatch);
        var articleIndexingDetails = await BuildArticleIndexingDetailsAsync(details, filter, authorRows, venueMetrics, ct);
        LogDashboardStep("detalle indexación", stepWatch);

        if (ReportingFilterApplicator.HasAuthorScopedFilters(filter))
        {
            var authorFilteredArticleKeys = ApplyAuthorFilters(authorRows, filter)
                .Select(x => x.ArticleKey)
                .Distinct()
                .ToHashSet();

            details = details
                .Where(x => authorFilteredArticleKeys.Contains(x.ArticleKey))
                .ToList();

            articleIndexingDetails = articleIndexingDetails
                .Where(x => authorFilteredArticleKeys.Contains(x.ArticleKey))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter?.IndexingSource))
        {
            var indexedArticleKeys = articleIndexingDetails
                .Select(x => x.ArticleKey)
                .Distinct()
                .ToHashSet();

            details = details
                .Where(x => indexedArticleKeys.Contains(x.ArticleKey))
                .ToList();
        }

        var filteredVenueNames = details
            .Select(x => Normalize(x.VenueName, "Sin venue"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filteredArticleKeys = details
            .Select(x => x.ArticleKey)
            .Distinct()
            .ToHashSet();

        var authorOptionRows = authorRows
            .Where(x => filteredArticleKeys.Contains(x.ArticleKey))
            .ToList();

        var filteredAuthorRows = authorRows
            .Where(x => filteredArticleKeys.Contains(x.ArticleKey))
            .ToList();

        var articlesWithAuthorTrace = filteredAuthorRows
            .Select(x => x.ArticleKey)
            .Distinct()
            .Count();

        var articlesWithoutAuthorTrace = Math.Max(0, filteredArticleKeys.Count - articlesWithAuthorTrace);

        var filteredVenueMetrics = venueMetrics
            .Where(x => filteredVenueNames.Contains(Normalize(x.VenueName, "Sin venue")))
            .ToList();

        var filteredQuartileDistribution = filteredVenueMetrics
            .GroupBy(x => Normalize(x.VenueName, "Sin venue"), StringComparer.OrdinalIgnoreCase)
            .Select(g => g
                .OrderByDescending(x => x.YearNumber)
                .First())
            .GroupBy(x => Normalize(x.Quartile, "Sin cuartil"))
            .Select(g => new ReportingQuartileSummaryDto
            {
                Quartile = g.Key,
                TotalVenues = g.Count()
            })
            .OrderBy(x => x.Quartile)
            .ToList();

        var hasScopedInstitutionalFilters =
            ReportingFilterApplicator.HasScopedInstitutionalFilters(filter)
            || ReportingFilterApplicator.HasAuthorScopedFilters(filter);
        var participationSummary = BuildParticipationSummary(details, articleIndexingDetails);

        var dashboard = new InstitutionalReportingDashboardDto
        {
            Health = health,
            FilterOptions = BuildFilterOptions(details, articleIndexingDetails, authorOptionRows, filter),
            ScientificProduction = new ScientificProductionKpiDto
            {
                TotalArticles = details.Sum(x => x.ArticleCount),
                OpenAccessArticles = details.Where(x => x.IsOpenAccess).Sum(x => x.ArticleCount),
                ProjectResultArticles = details.Where(x => x.IsProjectResult).Sum(x => x.ArticleCount),
                InterculturalArticles = details.Where(x => x.HasInterculturalComponent).Sum(x => x.ArticleCount)
            },
            AuthorTraceCoverage = new AuthorTraceCoverageDto
            {
                ArticlesWithAuthorTrace = articlesWithAuthorTrace,
                ArticlesWithoutAuthorTrace = articlesWithoutAuthorTrace
            },
            LoadQuality = new LoadQualityKpiDto
            {
                TotalBatches = hasScopedInstitutionalFilters ? 0 : loadQuality?.TotalBatches ?? 0,
                TotalRows = hasScopedInstitutionalFilters ? 0 : loadQuality?.TotalRows ?? 0,
                SuccessfulRows = hasScopedInstitutionalFilters ? 0 : loadQuality?.SuccessfulRows ?? 0,
                ErrorRows = hasScopedInstitutionalFilters ? 0 : loadQuality?.ErrorRows ?? 0
            },
            Workflow = new WorkflowKpiDto
            {
                TotalStageExecutions = hasScopedInstitutionalFilters ? 0 : workflow?.TotalStageExecutions ?? 0,
                AvgStageDurationSeconds = hasScopedInstitutionalFilters ? null : workflow?.AvgStageDurationSeconds,
                ApprovedStages = hasScopedInstitutionalFilters ? 0 : workflow?.ApprovedStages ?? 0,
                ReturnedStages = hasScopedInstitutionalFilters ? 0 : workflow?.ReturnedStages ?? 0
            },
            ArticlesByYear = details
                .Select(x => new { Date = periodDateSelector(x), x.ArticleCount })
                .Where(x => x.Date.HasValue)
                .GroupBy(x => x.Date!.Value.Year)
                .Select(g => new ArticlesByYearDto { Year = g.Key, Count = g.Sum(x => x.ArticleCount) })
                .OrderBy(x => x.Year)
                .ToList(),
            ArticlesByMonth = details
                .Select(x => new { Date = periodDateSelector(x), x.ArticleCount })
                .Where(x => x.Date.HasValue)
                .GroupBy(x => x.Date!.Value.ToString("yyyy-MM"))
                .Select(g => new ReportingSummaryItemDto { Name = g.Key, TotalArticles = g.Sum(x => x.ArticleCount) })
                .OrderBy(x => x.Name)
                .ToList(),
            ArticlesByDay = details
                .Select(x => new { Date = periodDateSelector(x), x.ArticleCount })
                .Where(x => x.Date.HasValue)
                .GroupBy(x => x.Date!.Value.ToString("yyyy-MM-dd"))
                .Select(g => new ReportingSummaryItemDto { Name = g.Key, TotalArticles = g.Sum(x => x.ArticleCount) })
                .OrderByDescending(x => x.Name)
                .Take(30)
                .ToList(),
            ArticlesByIndexingSource = articleIndexingDetails
                .GroupBy(x => Normalize(x.IndexingSourceName, "Sin base de datos"))
                .Select(g => new IndexingSourceSummaryDto
                {
                    IndexingSourceName = g.Key,
                    TotalArticles = g.Select(x => x.ArticleKey).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalArticles)
                .ThenBy(x => x.IndexingSourceName)
                .ToList(),
            PediIiitArticles = BuildReportableArticleRows(details, articleIndexingDetails, filteredAuthorRows)
                .OrderByDescending(x => x.PublishedDate)
                .ThenBy(x => x.Title)
                .Select(ToArticleIndexingDetailDto)
                .ToList(),
            TddTotalArticles = BuildReportableArticleRows(details, articleIndexingDetails, filteredAuthorRows)
                .OrderBy(x => x.FacultyName)
                .ThenBy(x => x.IndexingSourceName)
                .ThenByDescending(x => x.PublishedDate)
                .Select(ToArticleIndexingDetailDto)
                .ToList(),
            ParticipationSummary = participationSummary,
            ArticlesByPublicationStatus = GroupByName(details, x => x.PublicationStatus, "Sin estado"),
            ArticlesByAcademicTerm = GroupByName(details, x => x.AcademicTerm, "Sin periodo"),
            ArticlesByResearchLine = GroupByName(details, x => x.ResearchLine, "Sin línea"),
            ArticlesByAuthor = BuildAuthorSummary(filteredAuthorRows),
            ArticlesByFaculty = GroupByName(details, x => x.FacultyName, "Sin facultad"),
            ArticlesByVenueType = GroupByName(details, x => x.VenueType, "Sin tipo"),
            ArticlesByQuartile = BuildArticlesByQuartile(details, venueMetrics),
            ArticlesByField = details
                .GroupBy(x => new
                {
                    Broad = Normalize(x.BroadFieldName, "Sin área"),
                    Specific = Normalize(x.SpecificFieldName, "Sin área específica"),
                    Detailed = Normalize(x.DetailedFieldName, "Sin área detallada")
                })
                .Select(g => new ReportingFieldSummaryDto
                {
                    BroadField = g.Key.Broad,
                    SpecificField = g.Key.Specific,
                    DetailedField = g.Key.Detailed,
                    TotalArticles = g.Sum(x => x.ArticleCount)
                })
                .OrderByDescending(x => x.TotalArticles)
                .Take(12)
                .ToList(),
            ArticlesByVenue = details
                .GroupBy(x => new
                {
                    Venue = Normalize(x.VenueName, "Sin venue"),
                    Type = Normalize(x.VenueType, "Sin tipo")
                })
                .Select(g => new ReportingVenueSummaryDto
                {
                    VenueName = g.Key.Venue,
                    VenueType = g.Key.Type,
                    TotalArticles = g.Sum(x => x.ArticleCount)
                })
                .OrderByDescending(x => x.TotalArticles)
                .Take(12)
                .ToList(),
            QuartileDistribution = filteredQuartileDistribution,
            OpenAccessByYear = details
                .Select(x => new { Date = periodDateSelector(x), x.IsOpenAccess, x.ArticleCount })
                .Where(x => x.Date.HasValue)
                .GroupBy(x => x.Date!.Value.Year)
                .Select(g => new ReportingOpenAccessByYearDto
                {
                    Year = g.Key,
                    OpenAccessArticles = g.Where(x => x.IsOpenAccess).Sum(x => x.ArticleCount),
                    NonOpenAccessArticles = g.Where(x => !x.IsOpenAccess).Sum(x => x.ArticleCount)
                })
                .OrderBy(x => x.Year)
                .ToList(),
            VenueMetricsByYear = filteredVenueMetrics
                .OrderByDescending(x => x.YearNumber)
                .ThenBy(x => x.VenueName)
                .Take(25)
                .Select(x => new ReportingVenueMetricDto
                {
                    Year = x.YearNumber,
                    VenueName = Normalize(x.VenueName, "Sin venue"),
                    VenueType = Normalize(x.VenueType, "Sin tipo"),
                    Sjr = x.SJR,
                    CiteScore = x.CiteScore,
                    HIndex = x.HIndex,
                    Quartile = x.Quartile
                })
                .ToList(),
            RecentArticles = details
                .OrderByDescending(x => x.PublishedDate ?? x.CreatedDate)
                .Take(25)
                .Select(x => new ReportingArticleDetailDto
                {
                    ArticleId = x.ArticleId_OLTP,
                    Title = Normalize(x.Title, "Sin título"),
                    Doi = x.Doi,
                    Year = x.ArticleYear,
                    VenueName = x.VenueName,
                    PublicationStatus = x.PublicationStatus,
                    ResearchLine = x.ResearchLine,
                    BroadField = x.BroadFieldName,
                    IsOpenAccess = x.IsOpenAccess,
                    CreatedDate = x.CreatedDate,
                    PublishedDate = x.PublishedDate
                })
                .ToList(),
            WorkflowCurrentStages = workflowStages
                .Where(_ => !hasScopedInstitutionalFilters)
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
        dashboardWatch.Stop();
        _logger.LogInformation(
            "Dashboard de reportería construido en {ElapsedMs} ms. Artículos={Articles}, autoresTrazados={AuthorArticles}, sinAutores={MissingAuthors}.",
            dashboardWatch.ElapsedMilliseconds,
            dashboard.ScientificProduction.TotalArticles,
            dashboard.AuthorTraceCoverage.ArticlesWithAuthorTrace,
            dashboard.AuthorTraceCoverage.ArticlesWithoutAuthorTrace);

        return dashboard;
    }

    public Task<byte[]> GenerateDashboardPdfAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default)
        => GenerateDashboardPdfAsync(new InstitutionalPdfReportRequestDto { Filter = filter ?? new InstitutionalReportingFilterDto() }, ct);

    public async Task<byte[]> GenerateDashboardPdfAsync(InstitutionalPdfReportRequestDto request, CancellationToken ct = default)
    {
        var filter = request.Filter ?? new InstitutionalReportingFilterDto();
        var chartImages = (request.Charts ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.Base64Png))
            .Take(6)
            .ToList();
        var dashboard = await GetDashboardAsync(filter, ct);
        var filterChips = BuildPdfFilterChips(filter);
        var generatedAt = DateTime.Now;
        var includeKpis = filter.IncludePdfKpis;
        var includeFilters = filter.IncludePdfFilters;
        var includeCharts = filter.IncludePdfCharts;
        var includePeriod = filter.IncludePdfPeriod;
        var includeFields = filter.IncludePdfFields;
        var includeVenues = filter.IncludePdfVenues;
        var includeAuthors = filter.IncludePdfAuthors;
        var includePediIiit = filter.IncludePdfPediIiit;
        var includeTddTotal = filter.IncludePdfTddTotal;
        var includeParticipation = filter.IncludePdfParticipation;
        var includeArticles = filter.IncludePdfArticles;
        var authorDashboard = includeAuthors
            ? await GetAuthorDashboardAsync(filter, ct)
            : null;

        return BuildDashboardPdfDocument(
            dashboard,
            filter,
            filterChips,
            generatedAt,
            chartImages,
            includeKpis,
            includeFilters,
            includeCharts,
            includePeriod,
            includeFields,
            includeVenues,
            includeAuthors,
            includePediIiit,
            includeTddTotal,
            includeParticipation,
            includeArticles,
            authorDashboard);
    }

    public async Task<byte[]> GenerateAuthorPdfAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default)
    {
        var effectiveFilter = filter ?? new InstitutionalReportingFilterDto();
        var dashboard = await GetAuthorDashboardAsync(effectiveFilter, ct);
        var filterChips = BuildPdfFilterChips(effectiveFilter);
        var generatedAt = DateTime.Now;

        return BuildAuthorPdfDocument(dashboard, effectiveFilter, filterChips, generatedAt);
    }

    private static byte[] BuildDashboardPdfDocument(
        InstitutionalReportingDashboardDto dashboard,
        InstitutionalReportingFilterDto filter,
        List<(string Label, string Value)> filterChips,
        DateTime generatedAt,
        List<ReportChartImageDto> chartImages,
        bool includeKpis,
        bool includeFilters,
        bool includeCharts,
        bool includePeriod,
        bool includeFields,
        bool includeVenues,
        bool includeAuthors,
        bool includePediIiit,
        bool includeTddTotal,
        bool includeParticipation,
        bool includeArticles,
        AuthorReportingDashboardDto? authorDashboard)
    {
        try
        {
            return BuildDashboardPdfDocumentCore(
                dashboard,
                filter,
                filterChips,
                generatedAt,
                chartImages,
                includeKpis,
                includeFilters,
                includeCharts,
                includePeriod,
                includeFields,
                includeVenues,
                includeAuthors,
                includePediIiit,
                includeTddTotal,
                includeParticipation,
                includeArticles,
                authorDashboard);
        }
        catch when (includeCharts && chartImages.Count > 0)
        {
            return BuildDashboardPdfDocumentCore(
                dashboard,
                filter,
                filterChips,
                generatedAt,
                [],
                includeKpis,
                includeFilters,
                includeCharts,
                includePeriod,
                includeFields,
                includeVenues,
                includeAuthors,
                includePediIiit,
                includeTddTotal,
                includeParticipation,
                includeArticles,
                authorDashboard);
        }
    }

    private static byte[] BuildDashboardPdfDocumentCore(
        InstitutionalReportingDashboardDto dashboard,
        InstitutionalReportingFilterDto filter,
        List<(string Label, string Value)> filterChips,
        DateTime generatedAt,
        List<ReportChartImageDto> chartImages,
        bool includeKpis,
        bool includeFilters,
        bool includeCharts,
        bool includePeriod,
        bool includeFields,
        bool includeVenues,
        bool includeAuthors,
        bool includePediIiit,
        bool includeTddTotal,
        bool includeParticipation,
        bool includeArticles,
        AuthorReportingDashboardDto? authorDashboard)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(22);
                page.DefaultTextStyle(text => text.FontSize(9).FontColor("#45474B"));

                page.Header().Element(header =>
                {
                    header
                        .Background("#313647")
                        .Padding(16)
                        .Column(column =>
                        {
                            column.Spacing(4);
                            column.Item().Text("Universidad Técnica de Ambato").FontSize(9).FontColor("#FFF8D4").SemiBold();
                            column.Item().Text("Reporte institucional de artículos académicos").FontSize(19).FontColor("#FFFFFF").Bold();
                            column.Item().Text($"Generado el {generatedAt:dd/MM/yyyy HH:mm} · Información filtrada según los criterios seleccionados")
                                .FontSize(8)
                                .FontColor("#E3DE61");
                        });
                });

                page.Content().PaddingVertical(14).Column(column =>
                {
                    column.Spacing(12);

                    if (includeKpis)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Resumen ejecutivo", "Indicadores principales del reporte filtrado."));
                        column.Item().Row(row =>
                        {
                            row.Spacing(8);
                            row.RelativeItem().Element(item => PdfKpiCard(item, "Total de artículos", dashboard.ScientificProduction.TotalArticles.ToString("N0"), "Producción académica registrada"));
                            row.RelativeItem().Element(item => PdfKpiCard(item, "Open Access", dashboard.ScientificProduction.OpenAccessArticles.ToString("N0"), "Artículos de acceso abierto"));
                        });
                        column.Item().Row(row =>
                        {
                            row.Spacing(8);
                            row.RelativeItem().Element(item => PdfKpiCard(item, "Resultado de proyecto", dashboard.ScientificProduction.ProjectResultArticles.ToString("N0"), "Producción asociada a proyectos"));
                            row.RelativeItem().Element(item => PdfKpiCard(item, "Interculturalidad", dashboard.ScientificProduction.InterculturalArticles.ToString("N0"), "Artículos con componente intercultural"));
                        });
                    }

                    if (includeFilters)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Filtros aplicados", "Criterios usados para generar este reporte."));
                        column.Item().Element(element => PdfFilterSummary(element, filterChips));
                    }

                    if (includeCharts && chartImages.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Gráficos incluidos", "Imágenes exportadas desde los gráficos visibles del panel al momento de generar el PDF."));
                        foreach (var chartGroup in chartImages.GroupBy(x => string.IsNullOrWhiteSpace(x.Section) ? "General" : x.Section))
                        {
                            column.Item().Element(item => PdfChartGroup(item, chartGroup.Key, chartGroup));
                        }
                    }
                    else if (includeCharts)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Gráficos incluidos", "No se recibieron imágenes de gráficos desde la pantalla actual."));
                        column.Item()
                            .Border(1)
                            .BorderColor("#E7EAEC")
                            .Background("#FFFFFF")
                            .Padding(8)
                            .Text("No fue posible capturar gráficos visibles para este PDF. Abre el panel que contiene los gráficos, espera a que terminen de renderizar y genera nuevamente la vista previa.")
                            .FontSize(8)
                            .FontColor("#45474B");
                    }

                    if (includePeriod && (dashboard.ArticlesByYear.Count > 0 || dashboard.ArticlesByMonth.Count > 0 || dashboard.ArticlesByDay.Count > 0))
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Producción por periodo", "Conteos agrupados por año, mes y día según el periodo seleccionado."));
                        column.Item().Element(item => PdfCompactCard(item, "Por año", dashboard.ArticlesByYear.Select(x => ($"{x.Year}", x.Count)).Take(10), "#435663"));
                        column.Item().Element(item => PdfCompactCard(item, "Por mes", dashboard.ArticlesByMonth.Select(x => (x.Name, x.TotalArticles)).Take(10), "#2F5249"));
                        column.Item().Element(item => PdfCompactCard(item, "Por día", dashboard.ArticlesByDay.Select(x => (x.Name, x.TotalArticles)).Take(10), "#7A1E19"));
                    }

                    if (includeFields && dashboard.ArticlesByField.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Campos de conocimiento", "Distribución por campo amplio, campo específico y campo detallado."));
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.15f);
                                columns.RelativeColumn(1.15f);
                                columns.RelativeColumn(1.15f);
                                columns.ConstantColumn(56);
                            });

                            PdfHeaderCell(table, "Campo amplio");
                            PdfHeaderCell(table, "Campo específico");
                            PdfHeaderCell(table, "Campo detallado");
                            PdfHeaderCell(table, "Artículos");

                            foreach (var item in dashboard.ArticlesByField.Take(12))
                            {
                                PdfBodyCell(table, item.BroadField);
                                PdfBodyCell(table, item.SpecificField);
                                PdfBodyCell(table, item.DetailedField);
                                PdfBodyCell(table, item.TotalArticles.ToString("N0"), alignRight: true);
                            }
                        });
                    }

                    if (includeVenues && (dashboard.ArticlesByVenue.Count > 0 || dashboard.ArticlesByQuartile.Count > 0 || dashboard.ArticlesByIndexingSource.Count > 0))
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Revistas, cuartiles e indexación", "Concentración académica por revista, clasificación e indexación."));
                        column.Item().Element(item => PdfCompactCard(item, "Revistas", dashboard.ArticlesByVenue.Select(x => (x.VenueName, x.TotalArticles)).Take(10), "#435663"));
                        column.Item().Element(item => PdfCompactCard(item, "Cuartiles", dashboard.ArticlesByQuartile.Select(x => (x.Name, x.TotalArticles)).Take(10), "#7A1E19"));
                        column.Item().Element(item => PdfCompactCard(item, "Indexación", dashboard.ArticlesByIndexingSource.Select(x => (x.IndexingSourceName, x.TotalArticles)).Take(10), "#2F5249"));
                    }

                    if (includeAuthors && (dashboard.ArticlesByResearchLine.Count > 0 || dashboard.ArticlesByAuthor.Count > 0 || dashboard.ArticlesByFaculty.Count > 0 || authorDashboard is not null))
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Autores, coautoría y facultades", "Participación académica, trazabilidad autoral y vínculos de coautoría."));
                        column.Item().Element(item => PdfCompactCard(item, "Líneas de investigación", dashboard.ArticlesByResearchLine.Select(x => (x.Name, x.TotalArticles)).Take(10), "#2F5249"));
                        column.Item().Element(item => PdfCompactCard(item, "Autores", dashboard.ArticlesByAuthor.Select(x => (x.Name, x.TotalArticles)).Take(10), "#435663"));
                        column.Item().Element(item => PdfCompactCard(item, "Facultades", dashboard.ArticlesByFaculty.Select(x => (x.Name, x.TotalArticles)).Take(10), "#7A1E19"));

                        if (authorDashboard is not null)
                        {
                            column.Item().Row(row =>
                            {
                                row.Spacing(8);
                                row.RelativeItem().Element(item => PdfKpiCard(item, "Autores únicos", authorDashboard.Kpis.TotalAuthors.ToString("N0"), "Participantes trazados en el resultado"));
                                row.RelativeItem().Element(item => PdfKpiCard(item, "Vínculos autor-artículo", authorDashboard.Kpis.TotalAuthorArticleLinks.ToString("N0"), "Autoría principal y coautorías"));
                            });

                            if (authorDashboard.Coauthors.Count > 0)
                            {
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.1f);
                                        columns.ConstantColumn(56);
                                    });

                                    PdfHeaderCell(table, "Autor");
                                    PdfHeaderCell(table, "Coautor");
                                    PdfHeaderCell(table, "Filiación coautor");
                                    PdfHeaderCell(table, "Vínculos");

                                    foreach (var item in authorDashboard.Coauthors.Take(18))
                                    {
                                        PdfBodyCell(table, ShortenForPdf(item.AuthorName, 46));
                                        PdfBodyCell(table, ShortenForPdf(item.CoauthorName, 46));
                                        PdfBodyCell(table, ShortenForPdf(item.CoauthorAffiliation, 42));
                                        PdfBodyCell(table, item.SharedArticles.ToString("N0"), alignRight: true);
                                    }
                                });
                            }
                        }
                    }

                    if (includePediIiit && dashboard.PediIiitArticles.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "PEDI IIIT", "Detalle por título, base, revista, DOI, autor, línea de investigación, facultad, proyecto y cuartil."));
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.0f);
                                columns.RelativeColumn();
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.1f);
                                columns.RelativeColumn(1.1f);
                                columns.RelativeColumn(1.0f);
                                columns.ConstantColumn(54);
                                columns.ConstantColumn(42);
                                columns.RelativeColumn(1.1f);
                            });

                            PdfHeaderCell(table, "Título de artículo");
                            PdfHeaderCell(table, "Base");
                            PdfHeaderCell(table, "Revista / ISSN");
                            PdfHeaderCell(table, "DOI");
                            PdfHeaderCell(table, "Autor / línea");
                            PdfHeaderCell(table, "Facultad");
                            PdfHeaderCell(table, "Mes");
                            PdfHeaderCell(table, "Proyecto");
                            PdfHeaderCell(table, "Nombre proyecto");

                            foreach (var article in dashboard.PediIiitArticles.Take(16))
                            {
                                PdfBodyCell(table, ShortenForPdf(article.Title, 70));
                                PdfBodyCell(table, ShortenForPdf(article.IndexingSourceName, 20));
                                PdfBodyCell(table, ShortenForPdf($"{article.VenueName} / {article.Issn ?? "S/I"}", 42));
                                PdfBodyCell(table, ShortenForPdf(article.Doi ?? "Sin DOI", 34));
                                PdfBodyCell(table, ShortenForPdf($"{article.AuthorName} / {article.Career}", 44));
                                PdfBodyCell(table, ShortenForPdf(article.Faculty, 34));
                                PdfBodyCell(table, article.PublicationMonth);
                                PdfBodyCell(table, article.IsProjectResult ? "Sí" : "No", alignRight: true);
                                PdfBodyCell(table, ShortenForPdf(GetProjectNameLabel(article), 34));
                            }
                        });
                    }

                    if (includeTddTotal && dashboard.TddTotalArticles.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "TDD Total", "Detalle por publicación, base, revista, autor, cuartil, facultad y mes."));
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2f);
                                columns.RelativeColumn();
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.ConstantColumn(48);
                                columns.RelativeColumn(1.35f);
                                columns.RelativeColumn(1.1f);
                                columns.ConstantColumn(56);
                            });

                            PdfHeaderCell(table, "Título de publicación");
                            PdfHeaderCell(table, "Base de datos");
                            PdfHeaderCell(table, "Revista / ISSN");
                            PdfHeaderCell(table, "Autor");
                            PdfHeaderCell(table, "Cuartil");
                            PdfHeaderCell(table, "Facultad");
                            PdfHeaderCell(table, "Proyecto");
                            PdfHeaderCell(table, "Mes");

                            foreach (var article in dashboard.TddTotalArticles.Take(18))
                            {
                                PdfBodyCell(table, ShortenForPdf(article.Title, 82));
                                PdfBodyCell(table, ShortenForPdf(article.IndexingSourceName, 30));
                                PdfBodyCell(table, ShortenForPdf($"{article.VenueName} / {article.Issn ?? "S/I"}", 38));
                                PdfBodyCell(table, ShortenForPdf(article.AuthorName, 34));
                                PdfBodyCell(table, article.Quartile, alignRight: true);
                                PdfBodyCell(table, ShortenForPdf(article.Faculty, 42));
                                PdfBodyCell(table, ShortenForPdf(GetProjectNameLabel(article), 34));
                                PdfBodyCell(table, article.PublicationMonth);
                            }
                        });
                    }

                    if (includeParticipation && (dashboard.ParticipationSummary.TotalArticles > 0 || dashboard.ParticipationSummary.TotalIndexingLinks > 0))
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Porcentaje de participación", "Resumen proporcional por facultad, base de datos, cuartil y cruce facultad/base."));
                        column.Item().Element(item => PdfParticipationCard(item, "Facultades", dashboard.ParticipationSummary.ByFaculty.Take(10), dashboard.ParticipationSummary.TotalArticles));
                        column.Item().Element(item => PdfParticipationCard(item, "Bases de datos", dashboard.ParticipationSummary.ByIndexingSource.Take(10), dashboard.ParticipationSummary.TotalIndexingLinks));
                        column.Item().Element(item => PdfParticipationCard(item, "Cuartiles", dashboard.ParticipationSummary.ByQuartile.Take(10), dashboard.ParticipationSummary.TotalArticles));

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.35f);
                                columns.RelativeColumn();
                                columns.ConstantColumn(56);
                            });

                            PdfHeaderCell(table, "Facultad");
                            PdfHeaderCell(table, "Base de datos");
                            PdfHeaderCell(table, "Artículos");

                            foreach (var item in dashboard.ParticipationSummary.IndexingByFaculty.Take(18))
                            {
                                PdfBodyCell(table, ShortenForPdf(item.Faculty, 52));
                                PdfBodyCell(table, ShortenForPdf(item.IndexingSourceName, 34));
                                PdfBodyCell(table, item.TotalArticles.ToString("N0"), alignRight: true);
                            }

                            PdfBodyCell(table, "Total general");
                            PdfBodyCell(table, "Vínculos de indexación");
                            PdfBodyCell(table, dashboard.ParticipationSummary.TotalIndexingLinks.ToString("N0"), alignRight: true);
                        });
                    }

                    if (includeArticles && dashboard.RecentArticles.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Artículos incluidos", "Muestra de artículos que componen el reporte filtrado."));
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.2f);
                                columns.ConstantColumn(42);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.ConstantColumn(34);
                            });

                            PdfHeaderCell(table, "Título");
                            PdfHeaderCell(table, "Año");
                            PdfHeaderCell(table, "Revista");
                            PdfHeaderCell(table, "Línea");
                            PdfHeaderCell(table, "OA");

                            foreach (var article in dashboard.RecentArticles.Take(14))
                            {
                                PdfBodyCell(table, ShortenForPdf(article.Title, 90));
                                PdfBodyCell(table, article.Year?.ToString() ?? "S/D");
                                PdfBodyCell(table, ShortenForPdf(article.VenueName ?? "Sin revista", 42));
                                PdfBodyCell(table, ShortenForPdf(article.ResearchLine ?? "Sin línea", 42));
                                PdfBodyCell(table, article.IsOpenAccess ? "Sí" : "No", alignRight: true);
                            }
                        });
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("DIDE · Universidad Técnica de Ambato").FontSize(8).FontColor("#7A7A7A");
                    row.ConstantItem(130).AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor("#7A7A7A");
                        text.CurrentPageNumber().FontSize(8).FontColor("#7A7A7A");
                        text.Span(" de ").FontSize(8).FontColor("#7A7A7A");
                        text.TotalPages().FontSize(8).FontColor("#7A7A7A");
                    });
                });
            });
        }).GeneratePdf();
    }

    private static byte[] BuildAuthorPdfDocument(
        AuthorReportingDashboardDto dashboard,
        InstitutionalReportingFilterDto filter,
        List<(string Label, string Value)> filterChips,
        DateTime generatedAt)
    {
        var selectedAuthor = string.IsNullOrWhiteSpace(filter.AuthorName)
            ? "Vista general de autores"
            : $"Autor: {filter.AuthorName.Trim()}";
        var isFocusedAuthorReport = !string.IsNullOrWhiteSpace(filter.AuthorName);
        var authorLimit = isFocusedAuthorReport ? 12 : 18;
        var publicationLimitPerAuthor = isFocusedAuthorReport ? 16 : 8;
        var authorNodes = dashboard.Authors
            .OrderByDescending(x => x.TotalArticles)
            .ThenBy(x => x.AuthorName)
            .Take(authorLimit)
            .Select(author => new
            {
                Author = author,
                Publications = GetPdfAuthorPublications(dashboard.Publications, author, publicationLimitPerAuthor),
                Coauthors = GetPdfAuthorCoauthors(dashboard.Coauthors, author, 8)
            })
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(22);
                page.DefaultTextStyle(text => text.FontSize(8).FontColor("#45474B"));

                page.Header().Element(header =>
                {
                    header
                        .Background("#2F5249")
                        .Padding(16)
                        .Column(column =>
                        {
                            column.Spacing(4);
                            column.Item().Text("Universidad Técnica de Ambato").FontSize(9).FontColor("#FFF8D4").SemiBold();
                            column.Item().Text("Reporte de autores y coautoría").FontSize(19).FontColor("#FFFFFF").Bold();
                            column.Item().Text($"{selectedAuthor} · Generado el {generatedAt:dd/MM/yyyy HH:mm}")
                                .FontSize(8)
                                .FontColor("#E3DE61");
                        });
                });

                page.Content().PaddingVertical(14).Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Element(section => PdfSectionHeader(section, "Resumen autoral", "Indicadores principales de autores, vínculos y trazabilidad."));
                    column.Item().Row(row =>
                    {
                        row.Spacing(8);
                        row.RelativeItem().Element(item => PdfKpiCard(item, "Autores únicos", dashboard.Kpis.TotalAuthors.ToString("N0"), "Participantes identificados"));
                        row.RelativeItem().Element(item => PdfKpiCard(item, "Artículos trazados", dashboard.Kpis.TotalArticles.ToString("N0"), "Publicaciones con autoría"));
                        row.RelativeItem().Element(item => PdfKpiCard(item, "Vínculos autor-artículo", dashboard.Kpis.TotalAuthorArticleLinks.ToString("N0"), "Autoría principal y coautorías"));
                        row.RelativeItem().Element(item => PdfKpiCard(item, "Promedio autores", dashboard.Kpis.AverageAuthorsPerArticle.ToString("0.##"), "Autores por artículo"));
                    });

                    column.Item().Row(row =>
                    {
                        row.Spacing(8);
                        row.RelativeItem().Element(item => PdfKpiCard(item, "Autoría principal", dashboard.Kpis.PrimaryAuthorLinks.ToString("N0"), "Vínculos como autor principal"));
                        row.RelativeItem().Element(item => PdfKpiCard(item, "Coautorías", dashboard.Kpis.CoauthorLinks.ToString("N0"), "Vínculos como coautor"));
                        row.RelativeItem().Element(item => PdfKpiCard(item, "Con ORCID", dashboard.Kpis.AuthorsWithOrcid.ToString("N0"), "Autores con ORCID registrado"));
                        row.RelativeItem().Element(item => PdfKpiCard(item, "Con filiación", dashboard.Kpis.AuthorsWithAffiliation.ToString("N0"), "Autores con filiación"));
                    });

                    column.Item().Element(section => PdfSectionHeader(section, "Filtros aplicados", "Criterios usados para construir este reporte de autores."));
                    column.Item().Element(element => PdfFilterSummary(element, filterChips));

                    if (authorNodes.Count > 0)
                    {
                        var hierarchySubtitle = isFocusedAuthorReport
                            ? "Lectura jerárquica del autor seleccionado y sus publicaciones."
                            : "Lectura jerárquica de los autores con mayor volumen dentro del alcance actual.";

                        column.Item().Element(section => PdfSectionHeader(section, "Jerarquía de autores y publicaciones", hierarchySubtitle));
                        foreach (var node in authorNodes)
                        {
                            column.Item().Element(item => PdfAuthorHierarchyBlock(
                                item,
                                node.Author,
                                node.Publications,
                                node.Coauthors,
                                publicationLimitPerAuthor));
                        }
                    }

                    if (dashboard.ArticlesByFaculty.Count > 0 || dashboard.ArticlesByIndexingSource.Count > 0 || dashboard.ArticlesByQuartile.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Distribuciones autorales", "Resumen por facultad, base de indexación y cuartil."));
                        column.Item().Element(item => PdfCompactCard(item, "Facultades", dashboard.ArticlesByFaculty.Select(x => (x.Name, x.TotalArticles)).Take(10), "#2F5249"));
                        column.Item().Element(item => PdfCompactCard(item, "Indexación", dashboard.ArticlesByIndexingSource.Select(x => (x.Name, x.TotalArticles)).Take(10), "#435663"));
                        column.Item().Element(item => PdfCompactCard(item, "Cuartiles", dashboard.ArticlesByQuartile.Select(x => (x.Name, x.TotalArticles)).Take(10), "#7A1E19"));
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("DIDE · Universidad Técnica de Ambato").FontSize(8).FontColor("#7A7A7A");
                    row.ConstantItem(130).AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor("#7A7A7A");
                        text.CurrentPageNumber().FontSize(8).FontColor("#7A7A7A");
                        text.Span(" de ").FontSize(8).FontColor("#7A7A7A");
                        text.TotalPages().FontSize(8).FontColor("#7A7A7A");
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void PdfAuthorHierarchyBlock(
        IContainer container,
        AuthorReportingSummaryDto author,
        IReadOnlyList<AuthorPublicationDto> publications,
        IReadOnlyList<AuthorCoauthorDto> coauthors,
        int publicationLimit)
    {
        container
            .Border(1)
            .BorderColor("#DCE4E1")
            .Background("#FFFFFF")
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item()
                    .Background("#F5F7F8")
                    .BorderLeft(4)
                    .BorderColor("#2F5249")
                    .Padding(9)
                    .Row(row =>
                    {
                        row.RelativeItem().Column(authorColumn =>
                        {
                            authorColumn.Spacing(4);
                            authorColumn.Item().Text(ShortenForPdf(author.AuthorName, 84)).FontSize(10).Bold().FontColor("#313647");
                            authorColumn.Item().Text(ShortenForPdf($"{author.Affiliation} · {author.Identification ?? author.Orcid ?? "Sin identificación"}", 120))
                                .FontSize(7)
                                .FontColor("#667078");
                            authorColumn.Item().Text(BuildAuthorPdfReadingLine(author, publications, coauthors))
                                .FontSize(7)
                                .FontColor("#2F5249");
                        });
                        row.ConstantItem(92)
                            .AlignRight()
                            .Background("#EAF2ED")
                            .PaddingVertical(5)
                            .PaddingHorizontal(7)
                            .Text($"{author.TotalArticles:N0} publicaciones")
                            .FontSize(8)
                            .Bold()
                            .FontColor("#2F5249");
                    });

                column.Item().Row(row =>
                {
                    row.Spacing(6);
                    row.RelativeItem().Element(item => PdfMiniMetric(item, "Principal", author.PrimaryAuthorArticles));
                    row.RelativeItem().Element(item => PdfMiniMetric(item, "Coautor", author.CoauthorArticles));
                    row.RelativeItem().Element(item => PdfMiniMetric(item, "Publicaciones listadas", publications.Count));
                });

                if (publications.Count > 0)
                {
                    column.Item().Column(publicationColumn =>
                    {
                        publicationColumn.Spacing(4);
                        for (var index = 0; index < publications.Count; index++)
                        {
                            publicationColumn.Item().Element(item => PdfAuthorPublicationCard(item, publications[index], index + 1));
                        }
                    });

                    if (author.TotalArticles > publicationLimit)
                    {
                        column.Item()
                            .Text($"Se muestran {publications.Count:N0} publicaciones de {author.TotalArticles:N0} asociadas al autor.")
                            .FontSize(7)
                            .FontColor("#667078");
                    }
                }
                else
                {
                    column.Item().Text("Sin publicaciones detalladas disponibles para este autor dentro del alcance actual.")
                        .FontSize(7)
                        .FontColor("#667078");
                }

                if (coauthors.Count > 0)
                {
                    column.Item()
                        .Background("#FAFBFC")
                        .Border(1)
                        .BorderColor("#EEF1F2")
                        .Padding(7)
                        .Column(coauthorColumn =>
                    {
                        coauthorColumn.Spacing(5);
                        coauthorColumn.Item().Text("Coautorías frecuentes").FontSize(7).Bold().FontColor("#313647");
                        coauthorColumn.Item().Row(row =>
                        {
                            row.Spacing(4);
                            foreach (var coauthor in coauthors.Take(5))
                            {
                                row.AutoItem().Element(item => PdfAuthorChip(
                                    item,
                                    $"{ShortenForPdf(coauthor.CoauthorName, 24)} · {coauthor.SharedArticles:N0}",
                                    "#435663",
                                    "#EEF1F2"));
                            }
                        });
                    });
                }
            });
    }

    private static void PdfAuthorPublicationCard(IContainer container, AuthorPublicationDto publication, int index)
    {
        var roleLabel = publication.IsPrimaryAuthor ? "Autor principal" : "Coautor";
        var roleColor = publication.IsPrimaryAuthor ? "#2F5249" : "#7A1E19";
        var roleBackground = publication.IsPrimaryAuthor ? "#EAF2ED" : "#F7EAE8";
        var yearLabel = publication.Year?.ToString() ?? "S/D";
        var quartileLabel = string.IsNullOrWhiteSpace(publication.Quartile) ? "Sin cuartil" : publication.Quartile;

        container
            .Border(1)
            .BorderColor("#EEF1F2")
            .Background(publication.IsPrimaryAuthor ? "#FCFEFC" : "#FFFFFF")
            .Padding(6)
            .Column(column =>
            {
                column.Spacing(4);
                column.Item().Row(row =>
                {
                    row.ConstantItem(24)
                        .AlignCenter()
                        .Background(roleBackground)
                        .PaddingVertical(3)
                        .Text(index.ToString("00"))
                        .FontSize(7)
                        .Bold()
                        .FontColor(roleColor);
                    row.RelativeItem().Text(ShortenForPdf(publication.Title, 118))
                        .FontSize(8)
                        .Bold()
                        .FontColor("#313647");
                    row.ConstantItem(92)
                        .AlignRight()
                        .Text(roleLabel)
                        .FontSize(7)
                        .Bold()
                        .FontColor(roleColor);
                });

                column.Item().Row(row =>
                {
                    row.Spacing(4);
                    row.AutoItem().Element(item => PdfAuthorChip(item, yearLabel, "#435663", "#EEF1F2"));
                    row.AutoItem().Element(item => PdfAuthorChip(item, quartileLabel, "#7A1E19", "#F7EAE8"));
                    row.AutoItem().Element(item => PdfAuthorChip(item, ShortenForPdf(publication.IndexingSourceName, 20), "#2F5249", "#EAF2ED"));
                    row.RelativeItem().Text(ShortenForPdf(publication.VenueName, 60))
                        .FontSize(7)
                        .FontColor("#667078");
                });

                column.Item().Text(ShortenForPdf($"{publication.Faculty} · {publication.ResearchLine}", 112))
                    .FontSize(7)
                    .FontColor("#667078");

                if (!string.IsNullOrWhiteSpace(publication.Doi))
                {
                    column.Item().Text(ShortenForPdf($"DOI: {publication.Doi}", 112))
                        .FontSize(6.5f)
                        .FontColor("#8A949A");
                }
            });
    }

    private static void PdfAuthorChip(IContainer container, string label, string color, string background)
    {
        container
            .Background(background)
            .PaddingHorizontal(5)
            .PaddingVertical(2)
            .Text(label)
            .FontSize(6.5f)
            .Bold()
            .FontColor(color);
    }

    private static void PdfMiniMetric(IContainer container, string label, int value)
    {
        container
            .Border(1)
            .BorderColor("#EEF1F2")
            .Background("#FAFBFC")
            .PaddingVertical(5)
            .PaddingHorizontal(7)
            .Row(row =>
            {
                row.RelativeItem().Text(label).FontSize(7).FontColor("#667078");
                row.ConstantItem(42).AlignRight().Text(value.ToString("N0")).FontSize(8).Bold().FontColor("#313647");
            });
    }

    private static string BuildAuthorPdfReadingLine(
        AuthorReportingSummaryDto author,
        IReadOnlyList<AuthorPublicationDto> publications,
        IReadOnlyList<AuthorCoauthorDto> coauthors)
    {
        var dominantFaculty = GetDominantPdfValue(publications.Select(x => x.Faculty), "sin facultad dominante");
        var dominantQuartile = GetDominantPdfValue(publications.Select(x => x.Quartile), "sin cuartil dominante");
        var coauthorCount = coauthors.Count;
        return $"{author.PrimaryAuthorArticles:N0} como principal · {author.CoauthorArticles:N0} como coautor · {dominantFaculty} · {dominantQuartile} · {coauthorCount:N0} coautorías visibles";
    }

    private static string GetDominantPdfValue(IEnumerable<string?> values, string fallback)
    {
        return values
            .Select(x => Normalize(x, string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key)
            .Select(x => x.Key)
            .FirstOrDefault() ?? fallback;
    }

    private static IReadOnlyList<AuthorPublicationDto> GetPdfAuthorPublications(
        IEnumerable<AuthorPublicationDto> publications,
        AuthorReportingSummaryDto author,
        int take)
    {
        return publications
            .Where(publication => PdfPublicationMatchesAuthor(publication, author))
            .GroupBy(GetPdfPublicationIdentity, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(x => x.IsPrimaryAuthor)
                .ThenByDescending(x => x.PublishedDate ?? x.CreatedDate)
                .First())
            .OrderByDescending(x => x.PublishedDate ?? x.CreatedDate)
            .ThenBy(x => x.Title)
            .Take(take)
            .ToList();
    }

    private static IReadOnlyList<AuthorCoauthorDto> GetPdfAuthorCoauthors(
        IEnumerable<AuthorCoauthorDto> coauthors,
        AuthorReportingSummaryDto author,
        int take)
    {
        return coauthors
            .Where(x => x.AuthorKey == author.AuthorKey || string.Equals(x.AuthorName, author.AuthorName, StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => new { x.CoauthorKey, Name = Normalize(x.CoauthorName, "Sin coautor") })
            .Select(group => group
                .OrderByDescending(x => x.SharedArticles)
                .First())
            .OrderByDescending(x => x.SharedArticles)
            .ThenBy(x => x.CoauthorName)
            .Take(take)
            .ToList();
    }

    private static bool PdfPublicationMatchesAuthor(AuthorPublicationDto publication, AuthorReportingSummaryDto author)
    {
        return publication.AuthorKey == author.AuthorKey
            || string.Equals(publication.AuthorIdentity, author.AuthorIdentity, StringComparison.OrdinalIgnoreCase)
            || string.Equals(publication.AuthorName, author.AuthorName, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPdfPublicationIdentity(AuthorPublicationDto publication)
    {
        if (publication.ArticleKey > 0)
        {
            return publication.ArticleKey.ToString();
        }

        if (!string.IsNullOrWhiteSpace(publication.Doi))
        {
            return publication.Doi.Trim();
        }

        return Normalize(publication.Title, "Sin título");
    }

    public async Task<byte[]> GenerateDashboardExcelAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default)
    {
        var dashboard = await GetDashboardAsync(filter, ct);
        var filterChips = BuildPdfFilterChips(filter);
        var generatedAt = DateTime.Now;

        using var workbook = new XLWorkbook();
        BuildExcelSummaryWorksheet(workbook, dashboard, generatedAt);
        BuildExcelFiltersWorksheet(workbook, filterChips);
        BuildExcelParticipationWorksheet(workbook, dashboard);
        BuildExcelArticleIndexingWorksheet(workbook, "PEDI IIIT", dashboard.PediIiitArticles);
        BuildExcelArticleIndexingWorksheet(workbook, "TDD Total", dashboard.TddTotalArticles);
        BuildExcelRecentArticlesWorksheet(workbook, dashboard);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ReportingHealthDto> RunFullLoadAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync("EXEC etl.sp_RunFullLoad;", ct);
            Interlocked.Increment(ref _cacheVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible ejecutar el ETL completo del DW de reportería.");
            throw;
        }

        return await GetHealthAsync(ct);
    }

    private static void BuildExcelSummaryWorksheet(
        XLWorkbook workbook,
        InstitutionalReportingDashboardDto dashboard,
        DateTime generatedAt)
    {
        var sheet = workbook.Worksheets.Add("Resumen");
        WriteExcelTitle(sheet, "Reporte institucional de artículos académicos");
        sheet.Cell(2, 1).Value = "Generado";
        sheet.Cell(2, 2).Value = generatedAt;
        sheet.Cell(2, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

        var rows = new (string Metric, object Value, string Notes)[]
        {
            ("Total de artículos", dashboard.ScientificProduction.TotalArticles, "Producción académica registrada"),
            ("Open Access", dashboard.ScientificProduction.OpenAccessArticles, "Artículos de acceso abierto"),
            ("Resultado de proyecto", dashboard.ScientificProduction.ProjectResultArticles, "Artículos asociados a proyectos"),
            ("Interculturalidad", dashboard.ScientificProduction.InterculturalArticles, "Artículos con componente intercultural"),
            ("Con trazabilidad autoral", dashboard.AuthorTraceCoverage.ArticlesWithAuthorTrace, "Artículos con autor identificado"),
            ("Sin trazabilidad autoral", dashboard.AuthorTraceCoverage.ArticlesWithoutAuthorTrace, "Pendientes de trazabilidad"),
            ("Lotes de carga", dashboard.LoadQuality.TotalBatches, "Cargas registradas en el DW"),
            ("Filas exitosas", dashboard.LoadQuality.SuccessfulRows, "Registros procesados correctamente"),
            ("Filas con error", dashboard.LoadQuality.ErrorRows, "Registros observados"),
            ("Ejecuciones de flujo", dashboard.Workflow.TotalStageExecutions, "Eventos de workflow trazados"),
            ("Etapas aprobadas", dashboard.Workflow.ApprovedStages, "Etapas aprobadas"),
            ("Etapas devueltas", dashboard.Workflow.ReturnedStages, "Etapas devueltas")
        };

        WriteExcelTable(
            sheet,
            4,
            new[] { "Indicador", "Valor", "Observación" },
            rows.Select(x => new object?[] { x.Metric, x.Value, x.Notes }));
        ApplyExcelWorksheetDefaults(sheet);
    }

    private static void BuildExcelFiltersWorksheet(
        XLWorkbook workbook,
        IReadOnlyList<(string Label, string Value)> filterChips)
    {
        var sheet = workbook.Worksheets.Add("Filtros");
        WriteExcelTitle(sheet, "Filtros aplicados");

        var rows = filterChips.Count > 0
            ? filterChips.Select(x => new object?[] { x.Label, x.Value })
            : new[] { new object?[] { "Sin filtros aplicados", "Reporte institucional completo" } };

        WriteExcelTable(sheet, 3, new[] { "Filtro", "Valor" }, rows);
        ApplyExcelWorksheetDefaults(sheet);
    }

    private static void BuildExcelParticipationWorksheet(
        XLWorkbook workbook,
        InstitutionalReportingDashboardDto dashboard)
    {
        var sheet = workbook.Worksheets.Add("Participacion");
        WriteExcelTitle(sheet, "Porcentaje de participación");

        var row = 3;
        row = WriteExcelParticipationBlock(sheet, row, "Por facultad", dashboard.ParticipationSummary.ByFaculty);
        row = WriteExcelParticipationBlock(sheet, row + 2, "Por base de datos", dashboard.ParticipationSummary.ByIndexingSource);
        row = WriteExcelParticipationBlock(sheet, row + 2, "Por cuartil", dashboard.ParticipationSummary.ByQuartile);

        sheet.Cell(row + 2, 1).Value = "Cruce facultad / base de datos";
        sheet.Cell(row + 2, 1).Style.Font.Bold = true;
        WriteExcelTable(
            sheet,
            row + 3,
            new[] { "Facultad", "Base de datos", "Artículos" },
            dashboard.ParticipationSummary.IndexingByFaculty.Select(x => new object?[]
            {
                ExcelText(x.Faculty),
                ExcelText(x.IndexingSourceName),
                x.TotalArticles
            }));

        ApplyExcelWorksheetDefaults(sheet);
    }

    private static int WriteExcelParticipationBlock(
        IXLWorksheet sheet,
        int startRow,
        string title,
        IEnumerable<ReportingParticipationItemDto> items)
    {
        sheet.Cell(startRow, 1).Value = title;
        sheet.Cell(startRow, 1).Style.Font.Bold = true;
        var lastRow = WriteExcelTable(
            sheet,
            startRow + 1,
            new[] { "Grupo", "Artículos", "Participación" },
            items.Select(x => new object?[] { ExcelText(x.Name), x.TotalArticles, x.Percentage / 100m }));
        sheet.Range(startRow + 2, 3, Math.Max(startRow + 2, lastRow), 3).Style.NumberFormat.Format = "0.00%";
        return lastRow;
    }

    private static void BuildExcelArticleIndexingWorksheet(
        XLWorkbook workbook,
        string sheetName,
        IEnumerable<ReportingArticleIndexingDetailDto> articles)
    {
        var sheet = workbook.Worksheets.Add(sheetName);
        WriteExcelTitle(sheet, sheetName);

        WriteExcelTable(
            sheet,
            3,
            new[]
            {
                "Título", "Base de datos", "Revista", "ISSN", "DOI", "URL revista", "URL publicación",
                "Cédula autor", "Autor", "Participación", "Línea de investigación", "Cuartil", "Facultad", "Mes",
                "Proyecto", "Nombre proyecto", "Interculturalidad", "Campo amplio", "Campo específico", "Campo detallado"
            },
            articles.Select(article => new object?[]
            {
                ExcelText(article.Title),
                ExcelText(article.IndexingSourceName),
                ExcelText(article.VenueName),
                ExcelText(article.Issn),
                ExcelText(article.Doi),
                ExcelText(article.JournalUrl),
                ExcelText(article.PublicationUrl),
                ExcelText(article.AuthorIdentification),
                ExcelText(article.AuthorName),
                ExcelText(article.ParticipantType),
                ExcelText(article.Career),
                ExcelText(article.Quartile),
                ExcelText(article.Faculty),
                ExcelText(article.PublicationMonth),
                article.IsProjectResult ? "Sí" : "No",
                GetProjectNameLabel(article),
                article.HasInterculturalComponent ? "Sí" : "No",
                ExcelText(article.BroadField),
                ExcelText(article.SpecificField),
                ExcelText(article.DetailedField)
            }));

        ApplyExcelWorksheetDefaults(sheet);
    }

    private static void BuildExcelRecentArticlesWorksheet(
        XLWorkbook workbook,
        InstitutionalReportingDashboardDto dashboard)
    {
        var sheet = workbook.Worksheets.Add("Articulos");
        WriteExcelTitle(sheet, "Artículos incluidos");

        WriteExcelTable(
            sheet,
            3,
            new[] { "Título", "Año", "Revista", "DOI", "Estado", "Línea", "Campo amplio", "Open Access", "Fecha registro", "Fecha publicación" },
            dashboard.RecentArticles.Select(article => new object?[]
            {
                ExcelText(article.Title),
                article.Year,
                ExcelText(article.VenueName),
                ExcelText(article.Doi),
                ExcelText(article.PublicationStatus),
                ExcelText(article.ResearchLine),
                ExcelText(article.BroadField),
                article.IsOpenAccess ? "Sí" : "No",
                article.CreatedDate,
                article.PublishedDate
            }));

        sheet.Range(4, 9, Math.Max(4, sheet.LastRowUsed()?.RowNumber() ?? 4), 10)
            .Style.DateFormat.Format = "dd/MM/yyyy";
        ApplyExcelWorksheetDefaults(sheet);
    }

    private static void WriteExcelTitle(IXLWorksheet sheet, string title)
    {
        sheet.Cell(1, 1).Value = title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#313647");
    }

    private static int WriteExcelTable(
        IXLWorksheet sheet,
        int startRow,
        IReadOnlyList<string> headers,
        IEnumerable<object?[]> rows)
    {
        for (var col = 0; col < headers.Count; col++)
        {
            var cell = sheet.Cell(startRow, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.FromHtml("#FFF8D4");
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#313647");
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#45474B");
        }

        var currentRow = startRow + 1;
        foreach (var row in rows)
        {
            for (var col = 0; col < headers.Count; col++)
            {
                var value = col < row.Length ? row[col] : null;
                sheet.Cell(currentRow, col + 1).Value = XLCellValue.FromObject(value ?? string.Empty);
                sheet.Cell(currentRow, col + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                sheet.Cell(currentRow, col + 1).Style.Border.BottomBorderColor = XLColor.FromHtml("#EEF1F2");
            }

            currentRow++;
        }

        return currentRow - 1;
    }

    private static void ApplyExcelWorksheetDefaults(IXLWorksheet sheet)
    {
        sheet.SheetView.FreezeRows(3);
        foreach (var column in sheet.ColumnsUsed())
        {
            column.Width = Math.Min(Math.Max(column.Width, 14), 36);
        }

        sheet.Column(1).Width = 42;
        sheet.Rows().Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        sheet.Rows().Style.Alignment.WrapText = true;
    }

    private static string ExcelText(string? value)
        => string.IsNullOrWhiteSpace(value) ? "Sin dato" : value.Trim();

    private void LogDashboardStep(string step, Stopwatch watch)
    {
        watch.Stop();
        if (watch.ElapsedMilliseconds >= 250)
        {
            _logger.LogInformation(
                "Reporterías DW: etapa {Step} completada en {ElapsedMs} ms.",
                step,
                watch.ElapsedMilliseconds);
        }

        watch.Restart();
    }

    private static void PdfSectionHeader(IContainer container, string title, string subtitle)
    {
        container
            .BorderLeft(4)
            .BorderColor("#F4CE14")
            .Background("#F5F7F8")
            .PaddingVertical(7)
            .PaddingHorizontal(10)
            .Column(column =>
            {
                column.Spacing(2);
                column.Item().Text(title).FontSize(12).Bold().FontColor("#313647");
                column.Item().Text(subtitle).FontSize(8).FontColor("#7A7A7A");
            });
    }

    private static void PdfKpiCard(IContainer container, string label, string value, string helper)
    {
        container
            .Border(1)
            .BorderColor("#E7EAEC")
            .Background("#FFF8D4")
            .Padding(8)
            .Column(column =>
            {
                column.Spacing(3);
                column.Item().Text(label).FontSize(7).SemiBold().FontColor("#495E57");
                column.Item().Text(value).FontSize(17).Bold().FontColor("#313647");
                column.Item().Text(helper).FontSize(7).FontColor("#7A7A7A");
            });
    }

    private static void PdfFilterSummary(IContainer container, List<(string Label, string Value)> filters)
    {
        if (filters.Count == 0)
        {
            container
                .Border(1)
                .BorderColor("#E7EAEC")
                .Background("#FFFFFF")
                .Padding(8)
                .Text("Sin filtros aplicados. El reporte presenta el consolidado institucional disponible.")
                .FontSize(8)
                .FontColor("#45474B");
            return;
        }

        container
            .Border(1)
            .BorderColor("#E7EAEC")
            .Background("#FFFFFF")
            .Padding(8)
            .Column(column =>
            {
                column.Spacing(4);
                foreach (var filter in filters)
                {
                    column.Item().Text(text =>
                    {
                        text.Span($"{filter.Label}: ").SemiBold().FontColor("#2F5249");
                        text.Span(filter.Value).FontColor("#45474B");
                    });
                }
            });
    }

    private static void PdfCompactCard(IContainer container, string title, IEnumerable<(string Name, int Count)> rows, string accentColor)
    {
        container
            .Border(1)
            .BorderColor("#E7EAEC")
            .Background("#FFFFFF")
            .Padding(7)
            .Column(column =>
            {
                column.Spacing(4);
                column.Item()
                    .Background(accentColor)
                    .PaddingVertical(4)
                    .PaddingHorizontal(6)
                    .Text(title)
                    .FontSize(8)
                    .SemiBold()
                    .FontColor("#FFF8D4");

                var materialized = rows.ToList();
                if (materialized.Count == 0)
                {
                    column.Item().Text("Sin datos disponibles").FontSize(7).FontColor("#7A7A7A");
                    return;
                }

                foreach (var row in materialized)
                {
                    column.Item().Row(itemRow =>
                    {
                        itemRow.RelativeItem().Text(ShortenForPdf(row.Name, 38)).FontSize(7).FontColor("#45474B");
                        itemRow.ConstantItem(34).AlignRight().Text(row.Count.ToString("N0")).FontSize(7).Bold().FontColor("#313647");
                    });
                }
            });
    }

    private static void PdfParticipationCard(
        IContainer container,
        string title,
        IEnumerable<ReportingParticipationItemDto> rows,
        int total)
    {
        container
            .Border(1)
            .BorderColor("#E7EAEC")
            .Background("#FFFFFF")
            .Padding(7)
            .Column(column =>
            {
                column.Spacing(4);
                column.Item()
                    .Background("#2F5249")
                    .PaddingVertical(4)
                    .PaddingHorizontal(6)
                    .Text($"{title} · Total {total:N0}")
                    .FontSize(8)
                    .SemiBold()
                    .FontColor("#FFF8D4");

                var materialized = rows.ToList();
                if (materialized.Count == 0)
                {
                    column.Item().Text("Sin datos disponibles").FontSize(7).FontColor("#7A7A7A");
                    return;
                }

                foreach (var row in materialized)
                {
                    column.Item().Row(itemRow =>
                    {
                        itemRow.RelativeItem().Text(ShortenForPdf(row.Name, 34)).FontSize(7).FontColor("#45474B");
                        itemRow.ConstantItem(58).AlignRight().Text($"{row.TotalArticles:N0} · {row.Percentage:0.##}%").FontSize(7).Bold().FontColor("#313647");
                    });
                }
            });
    }

    private static void PdfChartGroup(
        IContainer container,
        string section,
        IEnumerable<ReportChartImageDto> charts)
    {
        var materialized = charts
            .Select(x => new
            {
                Chart = x,
                Bytes = TryDecodePngImage(x.Base64Png)
            })
            .Where(x => x.Bytes is { Length: > 0 })
            .ToList();

        if (materialized.Count == 0)
        {
            container
                .Border(1)
                .BorderColor("#E7EAEC")
                .Background("#FFFFFF")
                .Padding(8)
                .Text($"No se pudieron decodificar imágenes para la sección {section}.")
                .FontSize(8)
                .FontColor("#45474B");
            return;
        }

        container
            .Border(1)
            .BorderColor("#E7EAEC")
            .Background("#FFFFFF")
            .Padding(8)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item()
                    .Background("#055052")
                    .PaddingVertical(4)
                    .PaddingHorizontal(6)
                    .Text(section)
                    .FontSize(8)
                    .SemiBold()
                    .FontColor("#FFFFFF");

                foreach (var item in materialized)
                {
                    column.Item().Text(ShortenForPdf(item.Chart.Title, 86)).FontSize(8).Bold().FontColor("#313647");
                    column.Item()
                        .Border(1)
                        .BorderColor("#EEF1F2")
                        .Padding(5)
                        .MaxHeight(230)
                        .Image(item.Bytes!)
                        .FitArea();
                }
            });
    }

    private static byte[]? TryDecodePngImage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var base64 = value.Trim();
        var commaIndex = base64.IndexOf(',');
        if (commaIndex >= 0)
        {
            base64 = base64[(commaIndex + 1)..];
        }

        try
        {
            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length < 16)
            {
                return null;
            }

            var isPng = bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
            var isJpeg = bytes[0] == 0xFF && bytes[1] == 0xD8;
            return isPng || isJpeg ? bytes : null;
        }
        catch
        {
            return null;
        }
    }

    private static void PdfHeaderCell(TableDescriptor table, string text)
    {
        table.Cell()
            .Background("#45474B")
            .Border(1)
            .BorderColor("#45474B")
            .PaddingVertical(5)
            .PaddingHorizontal(6)
            .Text(text)
            .FontSize(7)
            .Bold()
            .FontColor("#FFF8D4");
    }

    private static void PdfBodyCell(TableDescriptor table, string text, bool alignRight = false)
    {
        var cell = table.Cell()
            .BorderBottom(1)
            .BorderColor("#EEF1F2")
            .PaddingVertical(4)
            .PaddingHorizontal(6);

        if (alignRight)
        {
            cell.AlignRight().Text(text).FontSize(7).FontColor("#45474B");
            return;
        }

        cell.Text(text).FontSize(7).FontColor("#45474B");
    }

    private static string ShortenForPdf(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Sin dato";
        }

        var text = value.Trim();
        return text.Length <= maxLength ? text : text[..Math.Max(0, maxLength - 1)] + "…";
    }

    private static string GetProjectNameLabel(ReportingArticleIndexingDetailDto article)
        => article.IsProjectResult
            ? string.IsNullOrWhiteSpace(article.ProjectName) ? "Sin registrar" : article.ProjectName
            : "-";

    private static List<(string Label, string Value)> BuildPdfFilterChips(InstitutionalReportingFilterDto? filter)
    {
        var chips = new List<(string Label, string Value)>();
        if (filter is null)
        {
            return chips;
        }

        AddPdfDate(chips, "Creación desde", filter.CreatedFrom);
        AddPdfDate(chips, "Creación hasta", filter.CreatedTo);
        AddPdfDate(chips, "Publicación desde", filter.PublishedFrom);
        AddPdfDate(chips, "Publicación hasta", filter.PublishedTo);
        AddPdfChip(chips, "Título", filter.ArticleTitle);
        AddPdfChip(chips, "DOI", filter.ArticleDoi);
        AddPdfChip(chips, "Proyecto", filter.ProjectName);
        AddPdfChip(chips, "Periodo académico", filter.AcademicTerm);
        AddPdfChip(chips, "Estado", filter.PublicationStatus);
        AddPdfChip(chips, "Línea de investigación", filter.ResearchLine);
        AddPdfChip(chips, "Facultad", filter.Faculty);
        AddPdfChip(chips, "Base de datos", filter.IndexingSource);
        AddPdfChip(chips, "Campo amplio", filter.BroadField);
        AddPdfChip(chips, "Campo específico", filter.SpecificField);
        AddPdfChip(chips, "Campo detallado", filter.DetailedField);
        AddPdfChip(chips, "Revista", filter.VenueName);
        AddPdfChip(chips, "Tipo de publicación", filter.VenueType);
        AddPdfChip(chips, "Cuartil", filter.Quartile);
        AddPdfChip(chips, "Autor", filter.AuthorName);
        AddPdfChip(chips, "Coautor", filter.CoauthorName);
        AddPdfChip(chips, "Filiación autor", filter.AuthorAffiliation);
        AddPdfChip(chips, "Tipo participante", filter.ParticipantType);
        if (filter.HasOrcid.HasValue)
        {
            chips.Add(("ORCID", filter.HasOrcid.Value ? "Con ORCID" : "Sin ORCID"));
        }

        if (filter.ArticleYear.HasValue)
        {
            chips.Add(("Año del artículo", filter.ArticleYear.Value.ToString()));
        }

        AddPdfChip(chips, "Mes", filter.ArticleMonth);

        if (filter.IsOpenAccess.HasValue)
        {
            chips.Add(("Acceso abierto", filter.IsOpenAccess.Value ? "Sí" : "No"));
        }

        if (filter.IsProjectResult.HasValue)
        {
            chips.Add(("Resultado de proyecto", filter.IsProjectResult.Value ? "Sí" : "No"));
        }

        if (filter.HasInterculturalComponent.HasValue)
        {
            chips.Add(("Interculturalidad", filter.HasInterculturalComponent.Value ? "Sí" : "No"));
        }

        if (filter.OnlyPrimaryAuthors == true)
        {
            chips.Add(("Autoría", "Solo autor principal"));
        }

        chips.Add(("Agrupación temporal", string.Equals(filter.PeriodDateType, "published", StringComparison.OrdinalIgnoreCase)
            ? "Fecha de publicación"
            : "Fecha de registro"));

        return chips;
    }

    private static void AddPdfChip(List<(string Label, string Value)> chips, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var normalizedValue = label.Equals("Autor", StringComparison.OrdinalIgnoreCase)
                ? string.Join(", ", SplitAuthorNameFilter(value))
                : value.Trim();
            chips.Add((label, normalizedValue));
        }
    }

    private static void AddPdfDate(List<(string Label, string Value)> chips, string label, DateTime? value)
    {
        if (value.HasValue)
        {
            chips.Add((label, value.Value.ToString("dd/MM/yyyy")));
        }
    }

    private async Task<List<ReportingSummaryItemDto>> BuildAuthorSummaryAsync(List<int> articleKeys, CancellationToken ct)
    {
        if (articleKeys.Count == 0)
        {
            return new List<ReportingSummaryItemDto>();
        }

        return await _db.Set<ReportingArticleAuthorSummaryRow>()
            .FromSqlInterpolated($"""
                SELECT TOP 12
                    ISNULL(NULLIF(LTRIM(RTRIM(da.Nombre)), N''), N'Sin autor') AS Name,
                    SUM(faa.AuthorCount) AS TotalArticles
                FROM dw.FactArticleAuthor faa
                INNER JOIN dw.DimAuthor da ON da.AuthorKey = faa.AuthorKey
                WHERE faa.ArticleKey IN (SELECT CAST([value] AS int) FROM STRING_SPLIT({string.Join(",", articleKeys)}, ','))
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(da.Nombre)), N''), N'Sin autor')
                ORDER BY TotalArticles DESC
                """)
            .AsNoTracking()
            .Select(x => new ReportingSummaryItemDto { Name = x.Name, TotalArticles = x.TotalArticles })
            .ToListAsync(ct);
    }

    private static List<ReportingSummaryItemDto> BuildAuthorSummary(IEnumerable<ReportingAuthorPublicationRow> authorRows)
    {
        return authorRows
            .GroupBy(x => new
            {
                Identity = x.AuthorIdentity,
                Name = Normalize(x.AuthorName, "Sin autor")
            })
            .Select(g => new ReportingSummaryItemDto
            {
                Name = g.Key.Name,
                TotalArticles = g.Select(x => x.ArticleKey).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalArticles)
            .ThenBy(x => x.Name)
            .Take(12)
            .ToList();
    }

    private async Task<List<ReportingSummaryItemDto>> BuildFacultySummaryAsync(List<int> articleKeys, CancellationToken ct)
    {
        if (articleKeys.Count == 0)
        {
            return new List<ReportingSummaryItemDto>();
        }

        return await _db.Set<ReportingArticleAuthorSummaryRow>()
            .FromSqlInterpolated($"""
                SELECT TOP 12
                    ISNULL(NULLIF(LTRIM(RTRIM(da.Affiliation)), N''), N'Sin facultad/afiliación') AS Name,
                    SUM(faa.AuthorCount) AS TotalArticles
                FROM dw.FactArticleAuthor faa
                INNER JOIN dw.DimAuthor da ON da.AuthorKey = faa.AuthorKey
                WHERE faa.ArticleKey IN (SELECT CAST([value] AS int) FROM STRING_SPLIT({string.Join(",", articleKeys)}, ','))
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(da.Affiliation)), N''), N'Sin facultad/afiliación')
                ORDER BY TotalArticles DESC
                """)
            .AsNoTracking()
            .Select(x => new ReportingSummaryItemDto { Name = x.Name, TotalArticles = x.TotalArticles })
            .ToListAsync(ct);
    }

    private static Func<ReportingArticleDetailRow, DateTime?> BuildPeriodDateSelector(InstitutionalReportingFilterDto? filter)
    {
        return string.Equals(filter?.PeriodDateType, "published", StringComparison.OrdinalIgnoreCase)
            ? x => x.PublishedDate
            : x => x.CreatedDate;
    }

    private static InstitutionalReportingFilterOptionsDto BuildFilterOptions(
        List<ReportingArticleDetailRow> details,
        List<ReportingArticleIndexingDetailRow> indexingDetails,
        List<ReportingAuthorPublicationRow> authorRows,
        InstitutionalReportingFilterDto? filter)
    {
        var periodDateSelector = BuildPeriodDateSelector(filter);

        return new InstitutionalReportingFilterOptionsDto
        {
            AcademicTerms = Distinct(details.Select(x => x.AcademicTerm)),
            PublicationStatuses = Distinct(details.Select(x => x.PublicationStatus)),
            ResearchLines = Distinct(details.Select(x => x.ResearchLine)),
            Faculties = Distinct(details.Select(x => x.FacultyName)),
            IndexingSources = Distinct(indexingDetails.Select(x => x.IndexingSourceName)),
            BroadFields = Distinct(details.Select(x => x.BroadFieldName)),
            SpecificFields = Distinct(details.Select(x => x.SpecificFieldName)),
            DetailedFields = Distinct(details.Select(x => x.DetailedFieldName)),
            Venues = Distinct(details.Select(x => x.VenueName)),
            VenueTypes = Distinct(details.Select(x => x.VenueType)),
            ArticleYears = details
                .Where(x => x.ArticleYear.HasValue)
                .Select(x => (int)x.ArticleYear!.Value)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList(),
            ArticleMonths = details
                .Select(periodDateSelector)
                .Where(x => x.HasValue)
                .Select(x => x!.Value.ToString("yyyy-MM"))
                .Distinct()
                .OrderByDescending(x => x)
                .ToList(),
            Quartiles = new List<string> { "Q1", "Q2", "Q3", "Q4", "Sin cuartil" },
            Authors = Distinct(authorRows.Select(x => x.AuthorName)),
            Affiliations = Distinct(authorRows.Select(x => x.Affiliation)),
            ParticipantTypes = Distinct(authorRows.Select(x => x.ParticipantType))
        };
    }

    private static List<ReportingSummaryItemDto> GroupByName(
        List<ReportingArticleDetailRow> details,
        Func<ReportingArticleDetailRow, string?> selector,
        string fallback)
    {
        return details
            .GroupBy(x => Normalize(selector(x), fallback))
            .Select(g => new ReportingSummaryItemDto
            {
                Name = g.Key,
                TotalArticles = g.Sum(x => x.ArticleCount)
            })
            .OrderByDescending(x => x.TotalArticles)
            .Take(12)
            .ToList();
    }

    private static List<ReportingSummaryItemDto> BuildArticlesByQuartile(
        List<ReportingArticleDetailRow> details,
        List<ReportingVenueMetricRow> venueMetrics)
    {
        var metricLookup = venueMetrics
            .Where(x => !string.IsNullOrWhiteSpace(x.VenueName))
            .GroupBy(x => Normalize(x.VenueName, "Sin venue"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(x => x.YearNumber)
                    .Select(x => Normalize(x.Quartile, "Sin cuartil"))
                    .FirstOrDefault() ?? "Sin cuartil",
                StringComparer.OrdinalIgnoreCase);

        return details
            .GroupBy(x =>
            {
                var venue = Normalize(x.VenueName, "Sin venue");
                return metricLookup.TryGetValue(venue, out var quartile) ? quartile : "Sin cuartil";
            })
            .Select(g => new ReportingSummaryItemDto
            {
                Name = g.Key,
                TotalArticles = g.Sum(x => x.ArticleCount)
            })
            .OrderBy(x => x.Name)
            .ToList();
    }

    private async Task<List<ReportingArticleIndexingDetailRow>> BuildArticleIndexingDetailsAsync(
        List<ReportingArticleDetailRow> details,
        InstitutionalReportingFilterDto? filter,
        List<ReportingAuthorPublicationRow>? authorRows,
        List<ReportingVenueMetricRow>? venueMetrics,
        CancellationToken ct)
    {
        if (details.Count == 0)
        {
            return new List<ReportingArticleIndexingDetailRow>();
        }

        var articleKeys = details.Select(x => x.ArticleKey).Distinct().ToHashSet();
        var rows = await SafeListAsync(
            _db.ArticleIndexingDetails
                .FromSqlRaw("""
                    SELECT
                        d.ArticleKey,
                        d.ArticleId_OLTP AS ArticleId,
                        d.Title,
                        d.Doi,
                        dis.Name AS IndexingSourceName,
                        d.VenueName,
                        dv.IssnCode AS Issn,
                        dv.JournalUrl,
                        d.PublicationUrl,
                        d.PublishedDate,
                        d.ArticleYear,
                        d.IsProjectResult,
                        d.HasInterculturalComponent,
                        d.FacultyName,
                        d.ResearchLine,
                        d.BroadFieldName,
                        d.SpecificFieldName,
                        d.DetailedFieldName,
                        CAST(NULL AS nvarchar(100)) AS AuthorIdentification,
                        CAST(NULL AS nvarchar(300)) AS AuthorName,
                        CAST(NULL AS nvarchar(150)) AS ParticipantType,
                        CAST(N'Sin cuartil' AS nvarchar(50)) AS Quartile
                    FROM dw.vw_Articles_Detail d
                    INNER JOIN dw.FactArticleIndexing fai
                        ON fai.ArticleKey = d.ArticleKey
                    INNER JOIN dw.DimIndexingSource dis
                        ON dis.IndexingSourceKey = fai.IndexingSourceKey
                    LEFT JOIN dw.FactArticlePublication fap
                        ON fap.FactArticlePublicationId = d.FactArticlePublicationId
                    LEFT JOIN dw.DimVenue dv
                        ON dv.VenueKey = fap.VenueKey
                    """)
                .AsNoTracking(),
            "detalle de artículos por base de datos",
            ct);

        EnrichIndexingDetails(rows, authorRows, venueMetrics);

        var filtered = rows
            .Where(x => articleKeys.Contains(x.ArticleKey));

        if (!string.IsNullOrWhiteSpace(filter?.IndexingSource))
        {
            var selected = filter.IndexingSource.Trim();
            filtered = filtered.Where(x => string.Equals(x.IndexingSourceName, selected, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    private static void EnrichIndexingDetails(
        List<ReportingArticleIndexingDetailRow> rows,
        List<ReportingAuthorPublicationRow>? authorRows,
        List<ReportingVenueMetricRow>? venueMetrics)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var authorLookup = authorRows?
            .GroupBy(x => x.ArticleKey)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(x => x.IsPrimaryAuthor)
                    .ThenBy(x => x.AuthorName)
                    .First());

        var metricLookup = venueMetrics?
            .Where(x => !string.IsNullOrWhiteSpace(x.VenueName))
            .GroupBy(x => Normalize(x.VenueName, "Sin venue"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.YearNumber).ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (authorLookup is not null
                && authorLookup.TryGetValue(row.ArticleKey, out var author))
            {
                row.AuthorIdentification = author.Identification;
                row.AuthorName = author.AuthorName;
                row.ParticipantType = author.ParticipantType;
            }

            if (metricLookup is null)
            {
                continue;
            }

            var venue = Normalize(row.VenueName, "Sin venue");
            if (!metricLookup.TryGetValue(venue, out var metrics))
            {
                continue;
            }

            var metric = metrics.FirstOrDefault(x => !row.ArticleYear.HasValue || x.YearNumber <= row.ArticleYear.Value)
                ?? metrics.FirstOrDefault();
            row.Quartile = Normalize(metric?.Quartile, "Sin cuartil");
        }
    }

    private static List<ReportingArticleIndexingDetailRow> BuildReportableArticleRows(
        List<ReportingArticleDetailRow> details,
        List<ReportingArticleIndexingDetailRow> indexingRows,
        List<ReportingAuthorPublicationRow>? authorRows)
    {
        if (details.Count == 0)
        {
            return new List<ReportingArticleIndexingDetailRow>();
        }

        var rows = indexingRows.ToList();
        var indexedArticleKeys = rows
            .Select(x => x.ArticleKey)
            .ToHashSet();
        var authorLookup = authorRows?
            .GroupBy(x => x.ArticleKey)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(x => x.IsPrimaryAuthor)
                    .ThenBy(x => x.AuthorName)
                    .First());

        foreach (var article in details.Where(x => !indexedArticleKeys.Contains(x.ArticleKey)))
        {
            ReportingAuthorPublicationRow? author = null;
            authorLookup?.TryGetValue(article.ArticleKey, out author);

            rows.Add(new ReportingArticleIndexingDetailRow
            {
                ArticleKey = article.ArticleKey,
                ArticleId = article.ArticleId_OLTP,
                Title = article.Title,
                Doi = article.Doi,
                IndexingSourceName = "Sin indexación",
                VenueName = Normalize(article.VenueName, "Sin revista"),
                Issn = null,
                JournalUrl = null,
                PublicationUrl = article.PublicationUrl,
                PublishedDate = article.PublishedDate,
                ArticleYear = article.ArticleYear,
                IsProjectResult = article.IsProjectResult,
                HasInterculturalComponent = article.HasInterculturalComponent,
                FacultyName = Normalize(article.FacultyName, "Sin facultad"),
                ResearchLine = Normalize(article.ResearchLine, "Sin línea"),
                BroadFieldName = Normalize(article.BroadFieldName, "Sin campo amplio"),
                SpecificFieldName = Normalize(article.SpecificFieldName, "Sin campo específico"),
                DetailedFieldName = Normalize(article.DetailedFieldName, "Sin campo detallado"),
                AuthorIdentification = author?.Identification,
                AuthorName = Normalize(author?.AuthorName, "Sin autor"),
                ParticipantType = Normalize(author?.ParticipantType, "Sin participación"),
                Quartile = "Sin cuartil"
            });
        }

        return rows;
    }

    private async Task<AuthorReportingDashboardDto> BuildAuthorDashboardAsync(
        InstitutionalReportingFilterDto? filter,
        CancellationToken ct)
    {
        var totalWatch = Stopwatch.StartNew();
        var stepWatch = Stopwatch.StartNew();
        var venueMetrics = await GetCachedListAsync(
            "venue-metrics-by-year",
            LoadVenueMetricsAsync,
            ct);
        LogDashboardStep("autores métricas revistas", stepWatch);
        var details = await SafeListAsync(
            ReportingFilterApplicator.ApplyArticleDetailFilters(
                _db.ArticleDetails.AsNoTracking(),
                filter,
                _db.VenueMetricsByYear.AsNoTracking()),
            "dw.vw_Articles_Detail",
            ct);
        LogDashboardStep("autores detalle artículos", stepWatch);

        var articleIndexingDetails = await BuildArticleIndexingDetailsAsync(details, filter, null, venueMetrics, ct);
        LogDashboardStep("autores detalle indexación", stepWatch);
        if (!string.IsNullOrWhiteSpace(filter?.IndexingSource))
        {
            var indexedArticleKeys = articleIndexingDetails
                .Select(x => x.ArticleKey)
                .Distinct()
                .ToHashSet();

            details = details
                .Where(x => indexedArticleKeys.Contains(x.ArticleKey))
                .ToList();
        }

        var scopedArticleKeys = details
            .Select(x => x.ArticleKey)
            .Distinct()
            .ToHashSet();

        var rows = (await GetCachedListAsync(
                "author-publication-rows",
                LoadAuthorPublicationRowsAsync,
                ct))
            .Where(x => scopedArticleKeys.Contains(x.ArticleKey))
            .Select(CloneAuthorPublicationRow)
            .ToList();
        LogDashboardStep("autores vínculos ligeros", stepWatch);
        EnrichAuthorPublicationRows(rows, details, articleIndexingDetails);
        LogDashboardStep("autores enriquecimiento en memoria", stepWatch);

        var filtered = ApplyAuthorFilters(rows, filter).ToList();
        var authorArticlePairs = filtered
            .GroupBy(x => new { AuthorIdentity = GetCanonicalAuthorIdentity(x), x.ArticleKey })
            .Select(g => g.First())
            .ToList();

        var articlesWithAuthorTrace = rows
            .Select(x => x.ArticleKey)
            .Distinct()
            .Count();

        var hasAuthorScopedFilters = ReportingFilterApplicator.HasAuthorScopedFilters(filter);

        var totalArticles = hasAuthorScopedFilters
            ? filtered.Select(x => x.ArticleKey).Distinct().Count()
            : scopedArticleKeys.Count;

        var effectiveArticlesWithAuthorTrace = hasAuthorScopedFilters
            ? filtered.Select(x => x.ArticleKey).Distinct().Count()
            : articlesWithAuthorTrace;

        var totalAuthors = filtered
            .Select(GetCanonicalAuthorIdentity)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var primaryLinks = authorArticlePairs.Count(x => x.IsPrimaryAuthor);

        var dashboard = new AuthorReportingDashboardDto
        {
            Kpis = new AuthorReportingKpiDto
            {
                TotalAuthors = totalAuthors,
                TotalArticles = totalArticles,
                ArticlesWithAuthorTrace = effectiveArticlesWithAuthorTrace,
                ArticlesWithoutAuthorTrace = Math.Max(0, totalArticles - effectiveArticlesWithAuthorTrace),
                TotalAuthorArticleLinks = authorArticlePairs.Count,
                PrimaryAuthorLinks = primaryLinks,
                CoauthorLinks = Math.Max(0, authorArticlePairs.Count - primaryLinks),
                AuthorsWithOrcid = filtered
                    .Where(x => !string.IsNullOrWhiteSpace(x.Orcid))
                    .Select(GetCanonicalAuthorIdentity)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                AuthorsWithAffiliation = filtered
                    .Where(x => !string.IsNullOrWhiteSpace(x.Affiliation))
                    .Select(GetCanonicalAuthorIdentity)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                AverageAuthorsPerArticle = effectiveArticlesWithAuthorTrace == 0
                    ? 0
                    : Math.Round((decimal)authorArticlePairs.Count / effectiveArticlesWithAuthorTrace, 2)
            },
            FilterOptions = new AuthorReportingFilterOptionsDto
            {
                Authors = Distinct(rows.Select(x => x.AuthorName)),
                Affiliations = Distinct(rows.Select(x => x.Affiliation)),
                ParticipantTypes = Distinct(rows.Select(x => x.ParticipantType))
            },
            Authors = BuildAuthorSummaries(authorArticlePairs),
            Publications = BuildAuthorPublications(filtered),
            Coauthors = BuildCoauthorSummaries(authorArticlePairs, filter),
            ArticlesByAffiliation = GroupAuthorRows(authorArticlePairs, x => x.Affiliation, "Sin filiación"),
            ArticlesByFaculty = GroupAuthorRows(authorArticlePairs, x => x.FacultyName, "Sin facultad"),
            ArticlesByIndexingSource = GroupAuthorRows(filtered, x => x.IndexingSourceName, "Sin base de datos"),
            ArticlesByQuartile = GroupAuthorRows(filtered, x => x.Quartile, "Sin cuartil"),
            ArticlesByMonth = authorArticlePairs
                .Select(x => new { Date = BuildPeriodDateSelector(filter)(ToArticleDetailRow(x)), x.ArticleKey })
                .Where(x => x.Date.HasValue)
                .GroupBy(x => x.Date!.Value.ToString("yyyy-MM"))
                .Select(g => new ReportingSummaryItemDto { Name = g.Key, TotalArticles = g.Select(x => x.ArticleKey).Distinct().Count() })
                .OrderBy(x => x.Name)
                .ToList()
        };

        totalWatch.Stop();
        _logger.LogInformation(
            "Analítica de autores construida en {ElapsedMs} ms. Autores={TotalAuthors}, publicaciones={Publications}, coautorías={Coauthors}.",
            totalWatch.ElapsedMilliseconds,
            dashboard.Kpis.TotalAuthors,
            dashboard.Publications.Count,
            dashboard.Coauthors.Count);

        return dashboard;
    }

    private async Task<List<T>> GetCachedListAsync<T>(
        string key,
        Func<CancellationToken, Task<List<T>>> factory,
        CancellationToken ct)
    {
        var cacheKey = $"reporting:lookup:{Volatile.Read(ref _cacheVersion)}:{key}";
        if (_cache.TryGetValue(cacheKey, out List<T>? cached) && cached is not null)
        {
            return cached;
        }

        var rows = await factory(ct);
        _cache.Set(cacheKey, rows, TimeSpan.FromMinutes(5));
        return rows;
    }

    private Task<List<ReportingVenueMetricRow>> LoadVenueMetricsAsync(CancellationToken ct)
    {
        return SafeListAsync(
            _db.VenueMetricsByYear.AsNoTracking().OrderByDescending(x => x.YearNumber).ThenBy(x => x.VenueName),
            "dw.vw_VenueMetrics_ByYear",
            ct);
    }

    private Task<List<ReportingAuthorPublicationRow>> LoadAuthorDashboardRowsAsync(CancellationToken ct)
    {
        return SafeListAsync(
            _db.AuthorPublications
                .FromSqlRaw("""
                    SELECT
                        da.AuthorKey,
                        COALESCE(
                            NULLIF(LTRIM(RTRIM(da.ExternalAuthorId)), N''),
                            NULLIF(LTRIM(RTRIM(da.Orcid)), N''),
                            NULLIF(LTRIM(RTRIM(da.Identificacion)), N''),
                            NULLIF(LTRIM(RTRIM(da.Email)), N''),
                            UPPER(LTRIM(RTRIM(da.Nombre)))
                        ) AS AuthorIdentity,
                        da.Nombre AS AuthorName,
                        da.Identificacion AS Identification,
                        da.Affiliation,
                        da.ParticipantType,
                        da.Email,
                        da.Orcid,
                        CAST(faa.IsPrimaryAuthorFlag AS bit) AS IsPrimaryAuthor,
                        faa.ArticleKey,
                        CAST(0 AS int) AS ArticleId,
                        CAST(NULL AS nvarchar(500)) AS Title,
                        CAST(NULL AS nvarchar(200)) AS Doi,
                        CAST(NULL AS nvarchar(100)) AS Issn,
                        CAST(NULL AS nvarchar(500)) AS JournalUrl,
                        CAST(NULL AS nvarchar(500)) AS PublicationUrl,
                        CAST(NULL AS datetime2) AS PublishedDate,
                        CAST(NULL AS datetime2) AS CreatedDate,
                        CAST(NULL AS smallint) AS ArticleYear,
                        CAST(NULL AS nvarchar(300)) AS VenueName,
                        CAST(NULL AS nvarchar(100)) AS VenueType,
                        CAST(NULL AS nvarchar(100)) AS PublicationStatus,
                        CAST(NULL AS nvarchar(100)) AS AcademicTerm,
                        CAST(0 AS bit) AS IsOpenAccess,
                        CAST(0 AS bit) AS IsProjectResult,
                        CAST(0 AS bit) AS HasInterculturalComponent,
                        CAST(NULL AS nvarchar(250)) AS IndexingSourceName,
                        CAST(N'Sin cuartil' AS nvarchar(50)) AS Quartile,
                        CAST(NULL AS nvarchar(250)) AS FacultyName,
                        CAST(NULL AS nvarchar(250)) AS ResearchLine,
                        CAST(NULL AS nvarchar(250)) AS BroadFieldName,
                        CAST(NULL AS nvarchar(250)) AS SpecificFieldName,
                        CAST(NULL AS nvarchar(250)) AS DetailedFieldName
                    FROM dw.FactArticleAuthor faa
                    INNER JOIN dw.DimAuthor da
                        ON da.AuthorKey = faa.AuthorKey
                    """)
                .AsNoTracking(),
            "autores ligeros para dashboard institucional",
            ct);
    }

    private Task<List<ReportingAuthorPublicationRow>> LoadAuthorPublicationRowsAsync(CancellationToken ct)
    {
        return SafeListAsync(
            _db.AuthorPublications
                .FromSqlRaw("""
                    SELECT
                        da.AuthorKey,
                        COALESCE(
                            NULLIF(LTRIM(RTRIM(da.ExternalAuthorId)), N''),
                            NULLIF(LTRIM(RTRIM(da.Orcid)), N''),
                            NULLIF(LTRIM(RTRIM(da.Identificacion)), N''),
                            NULLIF(LTRIM(RTRIM(da.Email)), N''),
                            UPPER(LTRIM(RTRIM(da.Nombre)))
                        ) AS AuthorIdentity,
                        da.Nombre AS AuthorName,
                        da.Identificacion AS Identification,
                        da.Affiliation,
                        da.ParticipantType,
                        da.Email,
                        da.Orcid,
                        CAST(faa.IsPrimaryAuthorFlag AS bit) AS IsPrimaryAuthor,
                        faa.ArticleKey,
                        CAST(0 AS int) AS ArticleId,
                        CAST(NULL AS nvarchar(500)) AS Title,
                        CAST(NULL AS nvarchar(200)) AS Doi,
                        CAST(NULL AS nvarchar(100)) AS Issn,
                        CAST(NULL AS nvarchar(500)) AS JournalUrl,
                        CAST(NULL AS nvarchar(500)) AS PublicationUrl,
                        CAST(NULL AS datetime2) AS PublishedDate,
                        CAST(NULL AS datetime2) AS CreatedDate,
                        CAST(NULL AS smallint) AS ArticleYear,
                        CAST(NULL AS nvarchar(300)) AS VenueName,
                        CAST(NULL AS nvarchar(100)) AS VenueType,
                        CAST(NULL AS nvarchar(100)) AS PublicationStatus,
                        CAST(NULL AS nvarchar(100)) AS AcademicTerm,
                        CAST(0 AS bit) AS IsOpenAccess,
                        CAST(0 AS bit) AS IsProjectResult,
                        CAST(0 AS bit) AS HasInterculturalComponent,
                        dis.Name AS IndexingSourceName,
                        CAST(N'Sin cuartil' AS nvarchar(50)) AS Quartile,
                        CAST(NULL AS nvarchar(250)) AS FacultyName,
                        CAST(NULL AS nvarchar(250)) AS ResearchLine,
                        CAST(NULL AS nvarchar(250)) AS BroadFieldName,
                        CAST(NULL AS nvarchar(250)) AS SpecificFieldName,
                        CAST(NULL AS nvarchar(250)) AS DetailedFieldName
                    FROM dw.FactArticleAuthor faa
                    INNER JOIN dw.DimAuthor da
                        ON da.AuthorKey = faa.AuthorKey
                    LEFT JOIN dw.FactArticleIndexing fai
                        ON fai.ArticleKey = faa.ArticleKey
                    LEFT JOIN dw.DimIndexingSource dis
                        ON dis.IndexingSourceKey = fai.IndexingSourceKey
                    """)
                .AsNoTracking(),
            "detalle analítico de autores",
            ct);
    }

    private static ReportingAuthorPublicationRow CloneAuthorPublicationRow(ReportingAuthorPublicationRow row)
    {
        return new ReportingAuthorPublicationRow
        {
            AuthorKey = row.AuthorKey,
            AuthorIdentity = row.AuthorIdentity,
            AuthorName = row.AuthorName,
            Identification = row.Identification,
            Affiliation = row.Affiliation,
            ParticipantType = row.ParticipantType,
            Email = row.Email,
            Orcid = row.Orcid,
            IsPrimaryAuthor = row.IsPrimaryAuthor,
            ArticleKey = row.ArticleKey,
            ArticleId = row.ArticleId,
            Title = row.Title,
            Doi = row.Doi,
            Issn = row.Issn,
            JournalUrl = row.JournalUrl,
            PublicationUrl = row.PublicationUrl,
            PublishedDate = row.PublishedDate,
            CreatedDate = row.CreatedDate,
            ArticleYear = row.ArticleYear,
            VenueName = row.VenueName,
            VenueType = row.VenueType,
            PublicationStatus = row.PublicationStatus,
            AcademicTerm = row.AcademicTerm,
            IsOpenAccess = row.IsOpenAccess,
            IsProjectResult = row.IsProjectResult,
            HasInterculturalComponent = row.HasInterculturalComponent,
            IndexingSourceName = row.IndexingSourceName,
            Quartile = row.Quartile,
            FacultyName = row.FacultyName,
            ResearchLine = row.ResearchLine,
            BroadFieldName = row.BroadFieldName,
            SpecificFieldName = row.SpecificFieldName,
            DetailedFieldName = row.DetailedFieldName
        };
    }

    private static void EnrichAuthorPublicationRows(
        List<ReportingAuthorPublicationRow> rows,
        List<ReportingArticleDetailRow> details,
        List<ReportingArticleIndexingDetailRow> indexingDetails)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var detailLookup = details
            .GroupBy(x => x.ArticleKey)
            .ToDictionary(g => g.Key, g => g.First());

        var indexingLookup = indexingDetails
            .GroupBy(x => new { x.ArticleKey, Source = Normalize(x.IndexingSourceName, "Sin base de datos") })
            .ToDictionary(g => (g.Key.ArticleKey, g.Key.Source), g => g.First());
        var firstIndexingByArticle = indexingDetails
            .GroupBy(x => x.ArticleKey)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var row in rows)
        {
            if (detailLookup.TryGetValue(row.ArticleKey, out var detail))
            {
                row.ArticleId = detail.ArticleId_OLTP;
                row.Title = detail.Title;
                row.Doi = detail.Doi;
                row.PublicationUrl = detail.PublicationUrl;
                row.PublishedDate = detail.PublishedDate;
                row.CreatedDate = detail.CreatedDate;
                row.ArticleYear = detail.ArticleYear;
                row.VenueName = detail.VenueName;
                row.VenueType = detail.VenueType;
                row.PublicationStatus = detail.PublicationStatus;
                row.AcademicTerm = detail.AcademicTerm;
                row.IsOpenAccess = detail.IsOpenAccess;
                row.IsProjectResult = detail.IsProjectResult;
                row.HasInterculturalComponent = detail.HasInterculturalComponent;
                row.FacultyName = detail.FacultyName;
                row.ResearchLine = detail.ResearchLine;
                row.BroadFieldName = detail.BroadFieldName;
                row.SpecificFieldName = detail.SpecificFieldName;
                row.DetailedFieldName = detail.DetailedFieldName;
            }

            var indexingKey = (row.ArticleKey, Normalize(row.IndexingSourceName, "Sin base de datos"));
            if (!indexingLookup.TryGetValue(indexingKey, out var indexing)
                && !indexingLookup.TryGetValue((row.ArticleKey, "Sin base de datos"), out indexing))
            {
                firstIndexingByArticle.TryGetValue(row.ArticleKey, out indexing);
            }

            if (indexing is null)
            {
                continue;
            }

            row.Issn = indexing.Issn;
            row.JournalUrl = indexing.JournalUrl;
            row.Quartile = indexing.Quartile;
        }
    }

    private static IEnumerable<ReportingAuthorPublicationRow> ApplyAuthorFilters(
        IEnumerable<ReportingAuthorPublicationRow> rows,
        InstitutionalReportingFilterDto? filter)
    {
        var query = rows;

        if (filter is null)
        {
            return query;
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(x => x.CreatedDate.HasValue && x.CreatedDate.Value.Date >= filter.CreatedFrom.Value.Date);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(x => x.CreatedDate.HasValue && x.CreatedDate.Value.Date <= filter.CreatedTo.Value.Date);
        }

        if (filter.PublishedFrom.HasValue)
        {
            query = query.Where(x => x.PublishedDate.HasValue && x.PublishedDate.Value.Date >= filter.PublishedFrom.Value.Date);
        }

        if (filter.PublishedTo.HasValue)
        {
            query = query.Where(x => x.PublishedDate.HasValue && x.PublishedDate.Value.Date <= filter.PublishedTo.Value.Date);
        }

        if (filter.ArticleYear.HasValue)
        {
            query = query.Where(x => x.ArticleYear == filter.ArticleYear.Value);
        }

        query = FilterByContainsText(query, filter.ArticleTitle, x => x.Title);
        query = FilterByContainsText(query, filter.ArticleDoi, x => x.Doi);
        if (!string.IsNullOrWhiteSpace(filter.ArticleMonth)
            && DateTime.TryParse($"{filter.ArticleMonth.Trim()}-01", out var monthStart))
        {
            var monthEnd = monthStart.AddMonths(1);
            query = string.Equals(filter.PeriodDateType, "published", StringComparison.OrdinalIgnoreCase)
                ? query.Where(x => x.PublishedDate >= monthStart && x.PublishedDate < monthEnd)
                : query.Where(x => x.CreatedDate >= monthStart && x.CreatedDate < monthEnd);
        }

        query = FilterByText(query, filter.AcademicTerm, x => x.AcademicTerm);
        query = FilterByText(query, filter.PublicationStatus, x => x.PublicationStatus);
        query = FilterByText(query, filter.ResearchLine, x => x.ResearchLine);
        query = FilterByText(query, filter.Faculty, x => x.FacultyName);
        query = FilterByText(query, filter.IndexingSource, x => x.IndexingSourceName);
        query = FilterByText(query, filter.BroadField, x => x.BroadFieldName);
        query = FilterByText(query, filter.SpecificField, x => x.SpecificFieldName);
        query = FilterByText(query, filter.DetailedField, x => x.DetailedFieldName);
        query = FilterByText(query, filter.VenueName, x => x.VenueName);
        query = FilterByText(query, filter.VenueType, x => x.VenueType);
        query = FilterByText(query, filter.Quartile, x => x.Quartile);
        query = FilterByText(query, filter.AuthorAffiliation, x => x.Affiliation);
        query = FilterByText(query, filter.ParticipantType, x => x.ParticipantType);

        if (filter.HasOrcid.HasValue)
        {
            query = filter.HasOrcid.Value
                ? query.Where(x => !string.IsNullOrWhiteSpace(x.Orcid))
                : query.Where(x => string.IsNullOrWhiteSpace(x.Orcid));
        }

        if (filter.IsOpenAccess.HasValue)
        {
            query = query.Where(x => x.IsOpenAccess == filter.IsOpenAccess.Value);
        }

        if (filter.IsProjectResult.HasValue)
        {
            query = query.Where(x => x.IsProjectResult == filter.IsProjectResult.Value);
        }

        if (filter.HasInterculturalComponent.HasValue)
        {
            query = query.Where(x => x.HasInterculturalComponent == filter.HasInterculturalComponent.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CoauthorName))
        {
            var selectedCoauthor = filter.CoauthorName.Trim();
            var coauthoredArticleKeys = query
                .Where(x => ContainsText(x.AuthorName, selectedCoauthor))
                .Select(x => x.ArticleKey)
                .Distinct()
                .ToHashSet();

            query = query.Where(x => coauthoredArticleKeys.Contains(x.ArticleKey));
        }

        if (!string.IsNullOrWhiteSpace(filter.AuthorName))
        {
            var selectedAuthors = SplitAuthorNameFilter(filter.AuthorName);
            query = selectedAuthors.Count == 0
                ? query
                : query.Where(x => selectedAuthors.Any(author => ContainsText(x.AuthorName, author)));
        }

        if (filter.OnlyPrimaryAuthors == true)
        {
            query = query.Where(x => x.IsPrimaryAuthor);
        }

        return query;
    }

    private static IReadOnlyList<string> SplitAuthorNameFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<ReportingAuthorPublicationRow> FilterByContainsText(
        IEnumerable<ReportingAuthorPublicationRow> rows,
        string? selected,
        Func<ReportingAuthorPublicationRow, string?> selector)
    {
        if (string.IsNullOrWhiteSpace(selected))
        {
            return rows;
        }

        var value = selected.Trim();
        return rows.Where(x => ContainsText(selector(x), value));
    }

    private static IEnumerable<ReportingAuthorPublicationRow> ApplyAuthorOptionsScope(
        IEnumerable<ReportingAuthorPublicationRow> rows,
        InstitutionalReportingFilterDto? filter)
    {
        if (filter is null)
        {
            return rows;
        }

        var scopeFilter = new InstitutionalReportingFilterDto
        {
            CreatedFrom = filter.CreatedFrom,
            CreatedTo = filter.CreatedTo,
            PublishedFrom = filter.PublishedFrom,
            PublishedTo = filter.PublishedTo,
            ArticleTitle = filter.ArticleTitle,
            ArticleDoi = filter.ArticleDoi,
            AcademicTerm = filter.AcademicTerm,
            PublicationStatus = filter.PublicationStatus,
            ResearchLine = filter.ResearchLine,
            Faculty = filter.Faculty,
            IndexingSource = filter.IndexingSource,
            BroadField = filter.BroadField,
            SpecificField = filter.SpecificField,
            DetailedField = filter.DetailedField,
            VenueName = filter.VenueName,
            VenueType = filter.VenueType,
            ArticleYear = filter.ArticleYear,
            Quartile = filter.Quartile,
            IsOpenAccess = filter.IsOpenAccess,
            IsProjectResult = filter.IsProjectResult,
            HasInterculturalComponent = filter.HasInterculturalComponent,
            PeriodDateType = filter.PeriodDateType
            ,
            HasOrcid = filter.HasOrcid
        };

        return ApplyAuthorFilters(rows, scopeFilter);
    }

    private static IEnumerable<ReportingAuthorPublicationRow> FilterByText(
        IEnumerable<ReportingAuthorPublicationRow> rows,
        string? selected,
        Func<ReportingAuthorPublicationRow, string?> selector)
    {
        if (string.IsNullOrWhiteSpace(selected))
        {
            return rows;
        }

        var value = selected.Trim();
        return rows.Where(x => string.Equals(selector(x)?.Trim(), value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsText(string? source, string value)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static List<AuthorReportingSummaryDto> BuildAuthorSummaries(List<ReportingAuthorPublicationRow> authorArticlePairs)
    {
        return authorArticlePairs
            .GroupBy(GetCanonicalAuthorIdentity, StringComparer.OrdinalIgnoreCase)
            .Select(g => new AuthorReportingSummaryDto
            {
                AuthorKey = g.Min(x => x.AuthorKey),
                AuthorIdentity = g.Key,
                AuthorName = GetMostUsefulValue(g.Select(x => x.AuthorName), "Sin autor"),
                Identification = g.Select(x => x.Identification).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                Affiliation = GetMostUsefulValue(g.Select(x => x.Affiliation), "Sin filiación"),
                ParticipantType = GetMostUsefulValue(g.Select(x => x.ParticipantType), "Sin tipo"),
                Email = g.Select(x => x.Email).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                Orcid = g.Select(x => x.Orcid).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                TotalArticles = g.Select(x => x.ArticleKey).Distinct().Count(),
                PrimaryAuthorArticles = g.Where(x => x.IsPrimaryAuthor).Select(x => x.ArticleKey).Distinct().Count(),
                CoauthorArticles = g.Where(x => !x.IsPrimaryAuthor).Select(x => x.ArticleKey).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalArticles)
            .ThenBy(x => x.AuthorName)
            .ToList();
    }

    private static List<AuthorPublicationDto> BuildAuthorPublications(List<ReportingAuthorPublicationRow> rows)
    {
        return rows
            .GroupBy(x => new { AuthorIdentity = GetCanonicalAuthorIdentity(x), x.ArticleKey })
            .Select(g =>
            {
                var row = g.First();
                return new AuthorPublicationDto
                {
                    AuthorKey = row.AuthorKey,
                    AuthorIdentity = g.Key.AuthorIdentity,
                    AuthorName = Normalize(row.AuthorName, "Sin autor"),
                    Identification = row.Identification,
                    Affiliation = Normalize(row.Affiliation, "Sin filiación"),
                    ParticipantType = Normalize(row.ParticipantType, "Sin tipo"),
                    IsPrimaryAuthor = row.IsPrimaryAuthor,
                    ArticleKey = row.ArticleKey,
                    ArticleId = row.ArticleId,
                    Title = Normalize(row.Title, "Sin título"),
                    Doi = row.Doi,
                    Issn = row.Issn,
                    JournalUrl = row.JournalUrl,
                    PublicationUrl = row.PublicationUrl,
                    PublishedDate = row.PublishedDate,
                    CreatedDate = row.CreatedDate,
                    Year = row.ArticleYear,
                    VenueName = Normalize(row.VenueName, "Sin revista"),
                    IndexingSourceName = JoinDistinct(g.Select(x => x.IndexingSourceName), "Sin base de datos"),
                    Quartile = JoinDistinct(g.Select(x => x.Quartile), "Sin cuartil"),
                    Faculty = Normalize(row.FacultyName, "Sin facultad"),
                    ResearchLine = Normalize(row.ResearchLine, "Sin línea"),
                    BroadField = Normalize(row.BroadFieldName, "Sin campo amplio"),
                    SpecificField = Normalize(row.SpecificFieldName, "Sin campo específico"),
                    DetailedField = Normalize(row.DetailedFieldName, "Sin campo detallado")
                };
            })
            .OrderByDescending(x => x.PublishedDate ?? x.CreatedDate)
            .ThenBy(x => x.AuthorName)
            .ToList();
    }

    private static List<AuthorCoauthorDto> BuildCoauthorSummaries(
        List<ReportingAuthorPublicationRow> authorArticlePairs,
        InstitutionalReportingFilterDto? filter)
    {
        var articleAuthors = authorArticlePairs
            .GroupBy(x => x.ArticleKey)
            .ToList();
        var selectedAuthor = filter?.AuthorName?.Trim();
        var selectedCoauthor = filter?.CoauthorName?.Trim();
        var pairs = new List<AuthorCoauthorDto>();

        foreach (var article in articleAuthors)
        {
            var authors = article
                .GroupBy(GetCanonicalAuthorIdentity, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            foreach (var author in authors)
            {
                foreach (var coauthor in authors.Where(x => !string.Equals(x.AuthorIdentity, author.AuthorIdentity, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!string.IsNullOrWhiteSpace(selectedAuthor) && !ContainsText(author.AuthorName, selectedAuthor))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(selectedCoauthor) && !ContainsText(coauthor.AuthorName, selectedCoauthor))
                    {
                        continue;
                    }

                    pairs.Add(new AuthorCoauthorDto
                    {
                        AuthorKey = author.AuthorKey,
                        AuthorName = Normalize(author.AuthorName, "Sin autor"),
                        CoauthorKey = coauthor.AuthorKey,
                        CoauthorName = Normalize(coauthor.AuthorName, "Sin coautor"),
                        CoauthorAffiliation = Normalize(coauthor.Affiliation, "Sin filiación"),
                        SharedArticles = 1
                    });
                }
            }
        }

        return pairs
            .GroupBy(x => new { x.AuthorName, x.CoauthorName, x.CoauthorAffiliation })
            .Select(g => new AuthorCoauthorDto
            {
                AuthorKey = g.Min(x => x.AuthorKey),
                AuthorName = g.Key.AuthorName,
                CoauthorKey = g.Min(x => x.CoauthorKey),
                CoauthorName = g.Key.CoauthorName,
                CoauthorAffiliation = g.Key.CoauthorAffiliation,
                SharedArticles = g.Sum(x => x.SharedArticles)
            })
            .OrderByDescending(x => x.SharedArticles)
            .ThenBy(x => x.AuthorName)
            .ToList();
    }

    private static List<ReportingSummaryItemDto> GroupAuthorRows(
        IEnumerable<ReportingAuthorPublicationRow> rows,
        Func<ReportingAuthorPublicationRow, string?> selector,
        string fallback)
    {
        return rows
            .GroupBy(x => Normalize(selector(x), fallback))
            .Select(g => new ReportingSummaryItemDto
            {
                Name = g.Key,
                TotalArticles = g.Select(x => x.ArticleKey).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalArticles)
            .ThenBy(x => x.Name)
            .Take(20)
            .ToList();
    }

    private static ReportingArticleDetailRow ToArticleDetailRow(ReportingAuthorPublicationRow row)
    {
        return new ReportingArticleDetailRow
        {
            ArticleKey = row.ArticleKey,
            CreatedDate = row.CreatedDate,
            PublishedDate = row.PublishedDate
        };
    }

    private static ReportingArticleIndexingDetailDto ToArticleIndexingDetailDto(ReportingArticleIndexingDetailRow row)
    {
        return new ReportingArticleIndexingDetailDto
        {
            ArticleId = row.ArticleId,
            Title = Normalize(row.Title, "Sin título"),
            Doi = row.Doi,
            IndexingSourceName = Normalize(row.IndexingSourceName, "Sin base de datos"),
            VenueName = Normalize(row.VenueName, "Sin revista"),
            Issn = row.Issn,
            JournalUrl = row.JournalUrl,
            PublicationUrl = row.PublicationUrl,
            PublishedDate = row.PublishedDate,
            PublicationMonth = row.PublishedDate?.ToString("yyyy-MM") ?? "Sin mes",
            AuthorIdentification = row.AuthorIdentification,
            AuthorName = Normalize(row.AuthorName, "Sin autor"),
            ParticipantType = Normalize(row.ParticipantType, "Sin participación"),
            Career = Normalize(row.ResearchLine, "Sin línea"),
            IsProjectResult = row.IsProjectResult,
            ProjectName = null,
            HasInterculturalComponent = row.HasInterculturalComponent,
            Quartile = Normalize(row.Quartile, "Sin cuartil"),
            Faculty = Normalize(row.FacultyName, "Sin facultad"),
            BroadField = Normalize(row.BroadFieldName, "Sin campo amplio"),
            SpecificField = Normalize(row.SpecificFieldName, "Sin campo específico"),
            DetailedField = Normalize(row.DetailedFieldName, "Sin campo detallado")
        };
    }

    private static ReportingParticipationSummaryDto BuildParticipationSummary(
        List<ReportingArticleDetailRow> details,
        List<ReportingArticleIndexingDetailRow> indexingDetails)
    {
        var totalArticles = Math.Max(0, details.Select(x => x.ArticleKey).Distinct().Count());
        var totalIndexingLinks = indexingDetails.Count;

        return new ReportingParticipationSummaryDto
        {
            TotalArticles = totalArticles,
            TotalIndexingLinks = totalIndexingLinks,
            ByFaculty = details
                .GroupBy(x => Normalize(x.FacultyName, "Sin facultad"))
                .Select(g => new ReportingParticipationItemDto
                {
                    Name = g.Key,
                    TotalArticles = g.Select(x => x.ArticleKey).Distinct().Count(),
                    Percentage = CalculatePercentage(g.Select(x => x.ArticleKey).Distinct().Count(), totalArticles)
                })
                .OrderByDescending(x => x.TotalArticles)
                .ToList(),
            ByIndexingSource = indexingDetails
                .GroupBy(x => Normalize(x.IndexingSourceName, "Sin base de datos"))
                .Select(g => new ReportingParticipationItemDto
                {
                    Name = g.Key,
                    TotalArticles = g.Select(x => x.ArticleKey).Distinct().Count(),
                    Percentage = CalculatePercentage(g.Count(), totalIndexingLinks)
                })
                .OrderByDescending(x => x.TotalArticles)
                .ToList(),
            ByQuartile = indexingDetails
                .GroupBy(x => x.ArticleKey)
                .Select(g => Normalize(g.Select(x => x.Quartile).FirstOrDefault(), "Sin cuartil"))
                .GroupBy(x => x)
                .Select(g => new ReportingParticipationItemDto
                {
                    Name = g.Key,
                    TotalArticles = g.Count(),
                    Percentage = CalculatePercentage(g.Count(), totalArticles)
                })
                .OrderBy(x => x.Name)
                .ToList(),
            IndexingByFaculty = indexingDetails
                .GroupBy(x => new
                {
                    Faculty = Normalize(x.FacultyName, "Sin facultad"),
                    IndexingSource = Normalize(x.IndexingSourceName, "Sin base de datos")
                })
                .Select(g => new ReportingFacultyIndexingBreakdownDto
                {
                    Faculty = g.Key.Faculty,
                    IndexingSourceName = g.Key.IndexingSource,
                    TotalArticles = g.Select(x => x.ArticleKey).Distinct().Count()
                })
                .OrderBy(x => x.Faculty)
                .ThenByDescending(x => x.TotalArticles)
                .ToList()
        };
    }

    private static decimal CalculatePercentage(int value, int total)
    {
        return total <= 0 ? 0 : Math.Round((decimal)value * 100 / total, 2);
    }

    private static List<string> Distinct(IEnumerable<string?> values)
    {
        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static string JoinDistinct(IEnumerable<string?> values, string fallback)
    {
        var items = values
            .Select(x => Normalize(x, fallback))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return items.Count == 0 ? fallback : string.Join(", ", items);
    }

    private static string GetCanonicalAuthorIdentity(ReportingAuthorPublicationRow row)
    {
        var identification = NormalizeAuthorKeyValue(row.Identification);
        if (!string.IsNullOrWhiteSpace(identification))
        {
            return $"id:{identification}";
        }

        var orcid = NormalizeAuthorKeyValue(row.Orcid);
        if (!string.IsNullOrWhiteSpace(orcid))
        {
            return $"orcid:{orcid}";
        }

        var email = NormalizeAuthorKeyValue(row.Email);
        if (!string.IsNullOrWhiteSpace(email))
        {
            return $"email:{email}";
        }

        var name = NormalizeAuthorName(row.AuthorName);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return $"name:{name}";
        }

        return string.IsNullOrWhiteSpace(row.AuthorIdentity)
            ? $"key:{row.AuthorKey}"
            : $"raw:{NormalizeAuthorKeyValue(row.AuthorIdentity)}";
    }

    private static string GetMostUsefulValue(IEnumerable<string?> values, string fallback)
    {
        return values
            .Select(x => Normalize(x, fallback))
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals(fallback, StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key.Length)
            .Select(g => g.Key)
            .FirstOrDefault() ?? fallback;
    }

    private static string NormalizeAuthorKeyValue(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Concat(value.Trim().Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

    private static string NormalizeAuthorName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(' ', parts).ToUpperInvariant();
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
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
