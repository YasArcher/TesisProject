using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
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
        var cacheKey = BuildCacheKey(filter);
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
        var cacheKey = BuildAuthorCacheKey(filter);
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
        var health = await GetHealthAsync(ct);
        var detailsQuery = ApplyFilters(_db.ArticleDetails.AsNoTracking(), filter);
        var details = await SafeListAsync(detailsQuery, "dw.vw_Articles_Detail", ct);
        var periodDateSelector = BuildPeriodDateSelector(filter);
        var loadQuality = await SafeFirstOrDefaultAsync(_db.LoadQualityKpis.AsNoTracking(), "dw.vw_KPI_CalidadCarga", ct);
        var workflow = await SafeFirstOrDefaultAsync(_db.WorkflowKpis.AsNoTracking(), "dw.vw_KPI_Workflow", ct);
        var quartiles = await SafeListAsync(
            _db.QuartileDistribution.AsNoTracking().OrderBy(x => x.Quartile),
            "dw.vw_QuartileDistribution",
            ct);
        var venueMetrics = await SafeListAsync(
            _db.VenueMetricsByYear.AsNoTracking().OrderByDescending(x => x.YearNumber).ThenBy(x => x.VenueName),
            "dw.vw_VenueMetrics_ByYear",
            ct);
        var workflowStages = await SafeListAsync(
            _db.WorkflowCurrentStages.AsNoTracking().OrderByDescending(x => x.BatchId_OLTP).Take(25),
            "dw.vw_Workflow_Batches_ByCurrentStage",
            ct);
        var articleIndexingDetails = await BuildArticleIndexingDetailsAsync(details, filter, ct);

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

        var participationSummary = BuildParticipationSummary(details, articleIndexingDetails);

        return new InstitutionalReportingDashboardDto
        {
            Health = health,
            FilterOptions = BuildFilterOptions(details, articleIndexingDetails),
            ScientificProduction = new ScientificProductionKpiDto
            {
                TotalArticles = details.Sum(x => x.ArticleCount),
                OpenAccessArticles = details.Where(x => x.IsOpenAccess).Sum(x => x.ArticleCount),
                ProjectResultArticles = details.Where(x => x.IsProjectResult).Sum(x => x.ArticleCount),
                InterculturalArticles = details.Where(x => x.HasInterculturalComponent).Sum(x => x.ArticleCount)
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
            PediIiitArticles = articleIndexingDetails
                .OrderByDescending(x => x.PublishedDate)
                .ThenBy(x => x.Title)
                .Take(80)
                .Select(ToArticleIndexingDetailDto)
                .ToList(),
            TddTotalArticles = articleIndexingDetails
                .OrderBy(x => x.FacultyName)
                .ThenBy(x => x.IndexingSourceName)
                .ThenByDescending(x => x.PublishedDate)
                .Take(100)
                .Select(ToArticleIndexingDetailDto)
                .ToList(),
            ParticipationSummary = participationSummary,
            ArticlesByPublicationStatus = GroupByName(details, x => x.PublicationStatus, "Sin estado"),
            ArticlesByAcademicTerm = GroupByName(details, x => x.AcademicTerm, "Sin periodo"),
            ArticlesByResearchLine = GroupByName(details, x => x.ResearchLine, "Sin línea"),
            ArticlesByAuthor = await BuildAuthorSummaryAsync(details.Select(x => x.ArticleKey).Distinct().ToList(), ct),
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
            QuartileDistribution = quartiles
                .Select(x => new ReportingQuartileSummaryDto
                {
                    Quartile = Normalize(x.Quartile, "Sin cuartil"),
                    TotalVenues = x.TotalVenues
                })
                .ToList(),
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
            VenueMetricsByYear = venueMetrics
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

    public async Task<byte[]> GenerateDashboardPdfAsync(InstitutionalReportingFilterDto? filter = null, CancellationToken ct = default)
    {
        var dashboard = await GetDashboardAsync(filter, ct);
        var filterChips = BuildPdfFilterChips(filter);
        var generatedAt = DateTime.Now;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
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

                    column.Item().Element(section => PdfSectionHeader(section, "Resumen ejecutivo", "Indicadores principales del reporte filtrado."));
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        PdfKpiCell(table, "Total de artículos", dashboard.ScientificProduction.TotalArticles.ToString("N0"), "Producción académica registrada");
                        PdfKpiCell(table, "Open Access", dashboard.ScientificProduction.OpenAccessArticles.ToString("N0"), "Artículos de acceso abierto");
                        PdfKpiCell(table, "Resultado de proyecto", dashboard.ScientificProduction.ProjectResultArticles.ToString("N0"), "Producción asociada a proyectos");
                        PdfKpiCell(table, "Interculturalidad", dashboard.ScientificProduction.InterculturalArticles.ToString("N0"), "Artículos con componente intercultural");
                    });

                    column.Item().Element(section => PdfSectionHeader(section, "Filtros aplicados", "Criterios usados para generar este reporte."));
                    column.Item().Element(element => PdfFilterSummary(element, filterChips));

                    if (dashboard.ArticlesByYear.Count > 0 || dashboard.ArticlesByMonth.Count > 0 || dashboard.ArticlesByDay.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Producción por periodo", "Conteos agrupados por año, mes y día según el periodo seleccionado."));
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            PdfCompactList(table, "Por año", dashboard.ArticlesByYear.Select(x => ($"{x.Year}", x.Count)).Take(10));
                            PdfCompactList(table, "Por mes", dashboard.ArticlesByMonth.Select(x => (x.Name, x.TotalArticles)).Take(10));
                            PdfCompactList(table, "Por día", dashboard.ArticlesByDay.Select(x => (x.Name, x.TotalArticles)).Take(10));
                        });
                    }

                    if (dashboard.ArticlesByField.Count > 0)
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

                    if (dashboard.ArticlesByVenue.Count > 0 || dashboard.ArticlesByQuartile.Count > 0 || dashboard.ArticlesByIndexingSource.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Revistas, cuartiles e indexación", "Concentración académica por revista, clasificación e indexación."));
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            PdfCompactList(table, "Revistas", dashboard.ArticlesByVenue.Select(x => (x.VenueName, x.TotalArticles)).Take(10));
                            PdfCompactList(table, "Cuartiles", dashboard.ArticlesByQuartile.Select(x => (x.Name, x.TotalArticles)).Take(10));
                            PdfCompactList(table, "Indexación", dashboard.ArticlesByIndexingSource.Select(x => (x.IndexingSourceName, x.TotalArticles)).Take(10));
                        });
                    }

                    if (dashboard.ArticlesByResearchLine.Count > 0 || dashboard.ArticlesByAuthor.Count > 0 || dashboard.ArticlesByFaculty.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Líneas, autores y facultades", "Participación académica y concentración temática."));
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            PdfCompactList(table, "Líneas de investigación", dashboard.ArticlesByResearchLine.Select(x => (x.Name, x.TotalArticles)).Take(10));
                            PdfCompactList(table, "Autores", dashboard.ArticlesByAuthor.Select(x => (x.Name, x.TotalArticles)).Take(10));
                            PdfCompactList(table, "Facultades", dashboard.ArticlesByFaculty.Select(x => (x.Name, x.TotalArticles)).Take(10));
                        });
                    }

                    if (dashboard.PediIiitArticles.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "PEDI IIIT", "Detalle por título de artículo, base de datos, enlace, mes de publicación, proyecto y cuartil."));
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.1f);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.ConstantColumn(54);
                                columns.ConstantColumn(42);
                                columns.ConstantColumn(42);
                            });

                            PdfHeaderCell(table, "Título de artículo");
                            PdfHeaderCell(table, "Base de datos");
                            PdfHeaderCell(table, "Enlace");
                            PdfHeaderCell(table, "Mes");
                            PdfHeaderCell(table, "Proyecto");
                            PdfHeaderCell(table, "Cuartil");

                            foreach (var article in dashboard.PediIiitArticles.Take(16))
                            {
                                PdfBodyCell(table, ShortenForPdf(article.Title, 78));
                                PdfBodyCell(table, ShortenForPdf(article.IndexingSourceName, 28));
                                PdfBodyCell(table, ShortenForPdf(article.PublicationUrl ?? "Sin enlace", 34));
                                PdfBodyCell(table, article.PublicationMonth);
                                PdfBodyCell(table, article.IsProjectResult ? "Sí" : "No", alignRight: true);
                                PdfBodyCell(table, article.Quartile, alignRight: true);
                            }
                        });
                    }

                    if (dashboard.TddTotalArticles.Count > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "TDD Total", "Detalle por publicación, base de datos, cuartil, facultad y mes."));
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2f);
                                columns.RelativeColumn();
                                columns.ConstantColumn(48);
                                columns.RelativeColumn(1.35f);
                                columns.ConstantColumn(56);
                            });

                            PdfHeaderCell(table, "Título de publicación");
                            PdfHeaderCell(table, "Base de datos");
                            PdfHeaderCell(table, "Cuartil");
                            PdfHeaderCell(table, "Facultad");
                            PdfHeaderCell(table, "Mes");

                            foreach (var article in dashboard.TddTotalArticles.Take(18))
                            {
                                PdfBodyCell(table, ShortenForPdf(article.Title, 82));
                                PdfBodyCell(table, ShortenForPdf(article.IndexingSourceName, 30));
                                PdfBodyCell(table, article.Quartile, alignRight: true);
                                PdfBodyCell(table, ShortenForPdf(article.Faculty, 42));
                                PdfBodyCell(table, article.PublicationMonth);
                            }
                        });
                    }

                    if (dashboard.ParticipationSummary.TotalArticles > 0 || dashboard.ParticipationSummary.TotalIndexingLinks > 0)
                    {
                        column.Item().Element(section => PdfSectionHeader(section, "Porcentaje de participación", "Resumen proporcional por facultad, base de datos, cuartil y cruce facultad/base."));
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            PdfParticipationList(table, "Facultades", dashboard.ParticipationSummary.ByFaculty.Take(10), dashboard.ParticipationSummary.TotalArticles);
                            PdfParticipationList(table, "Bases de datos", dashboard.ParticipationSummary.ByIndexingSource.Take(10), dashboard.ParticipationSummary.TotalIndexingLinks);
                            PdfParticipationList(table, "Cuartiles", dashboard.ParticipationSummary.ByQuartile.Take(10), dashboard.ParticipationSummary.TotalArticles);
                        });

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

                    if (dashboard.RecentArticles.Count > 0)
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

    private static void PdfKpiCell(TableDescriptor table, string label, string value, string helper)
    {
        table.Cell()
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

    private static void PdfCompactList(TableDescriptor table, string title, IEnumerable<(string Name, int Count)> rows)
    {
        table.Cell()
            .Border(1)
            .BorderColor("#E7EAEC")
            .Padding(7)
            .Column(column =>
            {
                column.Spacing(4);
                column.Item()
                    .Background("#435663")
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

    private static void PdfParticipationList(
        TableDescriptor table,
        string title,
        IEnumerable<ReportingParticipationItemDto> rows,
        int total)
    {
        table.Cell()
            .Border(1)
            .BorderColor("#E7EAEC")
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

        if (filter.ArticleYear.HasValue)
        {
            chips.Add(("Año del artículo", filter.ArticleYear.Value.ToString()));
        }

        if (filter.IsOpenAccess.HasValue)
        {
            chips.Add(("Acceso abierto", filter.IsOpenAccess.Value ? "Sí" : "No"));
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
            chips.Add((label, value.Trim()));
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

    private static string BuildCacheKey(InstitutionalReportingFilterDto? filter)
    {
        if (filter is null)
        {
            return $"reporting:dashboard:{Volatile.Read(ref _cacheVersion)}:empty";
        }

        return string.Join('|',
            "reporting:dashboard",
            Volatile.Read(ref _cacheVersion),
            filter.CreatedFrom?.ToString("yyyyMMdd"),
            filter.CreatedTo?.ToString("yyyyMMdd"),
            filter.PublishedFrom?.ToString("yyyyMMdd"),
            filter.PublishedTo?.ToString("yyyyMMdd"),
            filter.AcademicTerm,
            filter.PublicationStatus,
            filter.ResearchLine,
            filter.Faculty,
            filter.IndexingSource,
            filter.BroadField,
            filter.SpecificField,
            filter.DetailedField,
            filter.VenueName,
            filter.VenueType,
            filter.ArticleYear,
            filter.Quartile,
            filter.IsOpenAccess,
            filter.PeriodDateType,
            filter.AuthorName,
            filter.AuthorAffiliation,
            filter.ParticipantType,
            filter.OnlyPrimaryAuthors,
            filter.CoauthorName);
    }

    private static string BuildAuthorCacheKey(InstitutionalReportingFilterDto? filter)
    {
        return $"reporting:authors:{BuildCacheKey(filter)}";
    }

    private static Func<ReportingArticleDetailRow, DateTime?> BuildPeriodDateSelector(InstitutionalReportingFilterDto? filter)
    {
        return string.Equals(filter?.PeriodDateType, "published", StringComparison.OrdinalIgnoreCase)
            ? x => x.PublishedDate
            : x => x.CreatedDate;
    }

    private IQueryable<ReportingArticleDetailRow> ApplyFilters(
        IQueryable<ReportingArticleDetailRow> query,
        InstitutionalReportingFilterDto? filter)
    {
        if (filter is null)
        {
            return query;
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(x => x.CreatedDate >= filter.CreatedFrom.Value.Date);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(x => x.CreatedDate < filter.CreatedTo.Value.Date.AddDays(1));
        }

        if (filter.PublishedFrom.HasValue)
        {
            query = query.Where(x => x.PublishedDate >= filter.PublishedFrom.Value.Date);
        }

        if (filter.PublishedTo.HasValue)
        {
            query = query.Where(x => x.PublishedDate < filter.PublishedTo.Value.Date.AddDays(1));
        }

        query = ApplyStringFilter(query, filter.AcademicTerm, x => x.AcademicTerm);
        query = ApplyStringFilter(query, filter.PublicationStatus, x => x.PublicationStatus);
        query = ApplyStringFilter(query, filter.ResearchLine, x => x.ResearchLine);
        query = ApplyStringFilter(query, filter.Faculty, x => x.FacultyName);
        query = ApplyStringFilter(query, filter.BroadField, x => x.BroadFieldName);
        query = ApplyStringFilter(query, filter.SpecificField, x => x.SpecificFieldName);
        query = ApplyStringFilter(query, filter.DetailedField, x => x.DetailedFieldName);
        query = ApplyStringFilter(query, filter.VenueName, x => x.VenueName);
        query = ApplyStringFilter(query, filter.VenueType, x => x.VenueType);

        if (filter.ArticleYear.HasValue)
        {
            query = query.Where(x => x.ArticleYear == filter.ArticleYear.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Quartile))
        {
            var quartile = filter.Quartile.Trim();
            query = query.Where(x =>
                _db.VenueMetricsByYear.Any(metric =>
                    metric.VenueName == x.VenueName
                    && metric.YearNumber == x.ArticleYear
                    && metric.Quartile == quartile));
        }

        if (filter.IsOpenAccess.HasValue)
        {
            query = query.Where(x => x.IsOpenAccess == filter.IsOpenAccess.Value);
        }

        return query;
    }

    private static IQueryable<ReportingArticleDetailRow> ApplyStringFilter(
        IQueryable<ReportingArticleDetailRow> query,
        string? value,
        System.Linq.Expressions.Expression<Func<ReportingArticleDetailRow, string?>> selector)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return query;
        }

        return query.Where(ExpressionEqual(selector, value.Trim()));
    }

    private static System.Linq.Expressions.Expression<Func<ReportingArticleDetailRow, bool>> ExpressionEqual(
        System.Linq.Expressions.Expression<Func<ReportingArticleDetailRow, string?>> selector,
        string value)
    {
        var body = System.Linq.Expressions.Expression.Equal(
            selector.Body,
            System.Linq.Expressions.Expression.Constant(value));

        return System.Linq.Expressions.Expression.Lambda<Func<ReportingArticleDetailRow, bool>>(body, selector.Parameters);
    }

    private static InstitutionalReportingFilterOptionsDto BuildFilterOptions(
        List<ReportingArticleDetailRow> details,
        List<ReportingArticleIndexingDetailRow> indexingDetails)
    {
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
            Quartiles = new List<string> { "Q1", "Q2", "Q3", "Q4", "Sin cuartil" }
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
                        dis.Name AS IndexingSourceName,
                        d.PublicationUrl,
                        d.PublishedDate,
                        d.IsProjectResult,
                        d.FacultyName,
                        ISNULL(NULLIF(metric.Quartile, N''), N'Sin cuartil') AS Quartile
                    FROM dw.vw_Articles_Detail d
                    INNER JOIN dw.FactArticleIndexing fai
                        ON fai.ArticleKey = d.ArticleKey
                    INNER JOIN dw.DimIndexingSource dis
                        ON dis.IndexingSourceKey = fai.IndexingSourceKey
                    OUTER APPLY (
                        SELECT TOP 1 vm.Quartile
                        FROM dw.vw_VenueMetrics_ByYear vm
                        WHERE vm.VenueName = d.VenueName
                          AND (d.ArticleYear IS NULL OR vm.YearNumber <= d.ArticleYear)
                        ORDER BY vm.YearNumber DESC
                    ) metric
                    """)
                .AsNoTracking(),
            "detalle de artículos por base de datos",
            ct);

        var filtered = rows
            .Where(x => articleKeys.Contains(x.ArticleKey));

        if (!string.IsNullOrWhiteSpace(filter?.IndexingSource))
        {
            var selected = filter.IndexingSource.Trim();
            filtered = filtered.Where(x => string.Equals(x.IndexingSourceName, selected, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    private async Task<AuthorReportingDashboardDto> BuildAuthorDashboardAsync(
        InstitutionalReportingFilterDto? filter,
        CancellationToken ct)
    {
        var rows = await SafeListAsync(
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
                        da.Affiliation,
                        da.ParticipantType,
                        da.Email,
                        da.Orcid,
                        CAST(faa.IsPrimaryAuthorFlag AS bit) AS IsPrimaryAuthor,
                        d.ArticleKey,
                        d.ArticleId_OLTP AS ArticleId,
                        d.Title,
                        d.PublicationUrl,
                        d.PublishedDate,
                        d.CreatedDate,
                        d.ArticleYear,
                        d.VenueName,
                        dis.Name AS IndexingSourceName,
                        ISNULL(NULLIF(metric.Quartile, N''), N'Sin cuartil') AS Quartile,
                        d.FacultyName,
                        d.ResearchLine,
                        d.BroadFieldName,
                        d.SpecificFieldName,
                        d.DetailedFieldName
                    FROM dw.FactArticleAuthor faa
                    INNER JOIN dw.DimAuthor da
                        ON da.AuthorKey = faa.AuthorKey
                    INNER JOIN dw.vw_Articles_Detail d
                        ON d.ArticleKey = faa.ArticleKey
                    LEFT JOIN dw.FactArticleIndexing fai
                        ON fai.ArticleKey = d.ArticleKey
                    LEFT JOIN dw.DimIndexingSource dis
                        ON dis.IndexingSourceKey = fai.IndexingSourceKey
                    OUTER APPLY (
                        SELECT TOP 1 vm.Quartile
                        FROM dw.vw_VenueMetrics_ByYear vm
                        WHERE vm.VenueName = d.VenueName
                          AND (d.ArticleYear IS NULL OR vm.YearNumber <= d.ArticleYear)
                        ORDER BY vm.YearNumber DESC
                    ) metric
                    """)
                .AsNoTracking(),
            "detalle analítico de autores",
            ct);

        var filtered = ApplyAuthorFilters(rows, filter).ToList();
        var authorArticlePairs = filtered
            .GroupBy(x => new { x.AuthorIdentity, x.ArticleKey })
            .Select(g => g.First())
            .ToList();

        var totalArticles = filtered.Select(x => x.ArticleKey).Distinct().Count();
        var totalAuthors = filtered.Select(x => x.AuthorIdentity).Distinct().Count();
        var primaryLinks = authorArticlePairs.Count(x => x.IsPrimaryAuthor);

        return new AuthorReportingDashboardDto
        {
            Kpis = new AuthorReportingKpiDto
            {
                TotalAuthors = totalAuthors,
                TotalArticles = totalArticles,
                TotalAuthorArticleLinks = authorArticlePairs.Count,
                PrimaryAuthorLinks = primaryLinks,
                CoauthorLinks = Math.Max(0, authorArticlePairs.Count - primaryLinks),
                AuthorsWithOrcid = filtered
                    .Where(x => !string.IsNullOrWhiteSpace(x.Orcid))
                    .Select(x => x.AuthorIdentity)
                    .Distinct()
                    .Count(),
                AuthorsWithAffiliation = filtered
                    .Where(x => !string.IsNullOrWhiteSpace(x.Affiliation))
                    .Select(x => x.AuthorIdentity)
                    .Distinct()
                    .Count(),
                AverageAuthorsPerArticle = totalArticles == 0
                    ? 0
                    : Math.Round((decimal)authorArticlePairs.Count / totalArticles, 2)
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

        query = FilterByText(query, filter.ResearchLine, x => x.ResearchLine);
        query = FilterByText(query, filter.Faculty, x => x.FacultyName);
        query = FilterByText(query, filter.IndexingSource, x => x.IndexingSourceName);
        query = FilterByText(query, filter.BroadField, x => x.BroadFieldName);
        query = FilterByText(query, filter.SpecificField, x => x.SpecificFieldName);
        query = FilterByText(query, filter.DetailedField, x => x.DetailedFieldName);
        query = FilterByText(query, filter.VenueName, x => x.VenueName);
        query = FilterByText(query, filter.Quartile, x => x.Quartile);
        query = FilterByText(query, filter.AuthorAffiliation, x => x.Affiliation);
        query = FilterByText(query, filter.ParticipantType, x => x.ParticipantType);

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
            var selected = filter.AuthorName.Trim();
            query = query.Where(x => ContainsText(x.AuthorName, selected));
        }

        if (filter.OnlyPrimaryAuthors == true)
        {
            query = query.Where(x => x.IsPrimaryAuthor);
        }

        return query;
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
            .GroupBy(x => new
            {
                x.AuthorIdentity,
                Name = Normalize(x.AuthorName, "Sin autor"),
                Affiliation = Normalize(x.Affiliation, "Sin filiación"),
                ParticipantType = Normalize(x.ParticipantType, "Sin tipo"),
                x.Email,
                x.Orcid
            })
            .Select(g => new AuthorReportingSummaryDto
            {
                AuthorKey = g.Min(x => x.AuthorKey),
                AuthorName = g.Key.Name,
                Affiliation = g.Key.Affiliation,
                ParticipantType = g.Key.ParticipantType,
                Email = g.Key.Email,
                Orcid = g.Key.Orcid,
                TotalArticles = g.Select(x => x.ArticleKey).Distinct().Count(),
                PrimaryAuthorArticles = g.Where(x => x.IsPrimaryAuthor).Select(x => x.ArticleKey).Distinct().Count(),
                CoauthorArticles = g.Where(x => !x.IsPrimaryAuthor).Select(x => x.ArticleKey).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalArticles)
            .ThenBy(x => x.AuthorName)
            .Take(50)
            .ToList();
    }

    private static List<AuthorPublicationDto> BuildAuthorPublications(List<ReportingAuthorPublicationRow> rows)
    {
        return rows
            .GroupBy(x => new { x.AuthorIdentity, x.ArticleKey, Indexing = Normalize(x.IndexingSourceName, "Sin base de datos") })
            .Select(g =>
            {
                var row = g.First();
                return new AuthorPublicationDto
                {
                    AuthorKey = row.AuthorKey,
                    AuthorName = Normalize(row.AuthorName, "Sin autor"),
                    Affiliation = Normalize(row.Affiliation, "Sin filiación"),
                    ParticipantType = Normalize(row.ParticipantType, "Sin tipo"),
                    IsPrimaryAuthor = row.IsPrimaryAuthor,
                    ArticleId = row.ArticleId,
                    Title = Normalize(row.Title, "Sin título"),
                    PublicationUrl = row.PublicationUrl,
                    PublishedDate = row.PublishedDate,
                    CreatedDate = row.CreatedDate,
                    Year = row.ArticleYear,
                    VenueName = Normalize(row.VenueName, "Sin revista"),
                    IndexingSourceName = Normalize(row.IndexingSourceName, "Sin base de datos"),
                    Quartile = Normalize(row.Quartile, "Sin cuartil"),
                    Faculty = Normalize(row.FacultyName, "Sin facultad"),
                    ResearchLine = Normalize(row.ResearchLine, "Sin línea"),
                    BroadField = Normalize(row.BroadFieldName, "Sin campo amplio"),
                    SpecificField = Normalize(row.SpecificFieldName, "Sin campo específico"),
                    DetailedField = Normalize(row.DetailedFieldName, "Sin campo detallado")
                };
            })
            .OrderByDescending(x => x.PublishedDate ?? x.CreatedDate)
            .ThenBy(x => x.AuthorName)
            .Take(120)
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
                .GroupBy(x => x.AuthorIdentity)
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
            .Take(80)
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
            IndexingSourceName = Normalize(row.IndexingSourceName, "Sin base de datos"),
            PublicationUrl = row.PublicationUrl,
            PublishedDate = row.PublishedDate,
            PublicationMonth = row.PublishedDate?.ToString("yyyy-MM") ?? "Sin mes",
            IsProjectResult = row.IsProjectResult,
            Quartile = Normalize(row.Quartile, "Sin cuartil"),
            Faculty = Normalize(row.FacultyName, "Sin facultad")
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
                    Percentage = CalculatePercentage(g.Select(x => x.ArticleKey).Distinct().Count(), totalArticles)
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
