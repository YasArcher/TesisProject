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
        var indexing = await SafeListAsync(
            _db.ArticlesByIndexingSource.AsNoTracking().OrderByDescending(x => x.TotalArticles),
            "dw.vw_Articles_ByIndexingSource",
            ct);
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

        return new InstitutionalReportingDashboardDto
        {
            Health = health,
            FilterOptions = BuildFilterOptions(details),
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
            ArticlesByIndexingSource = indexing
                .Select(x => new IndexingSourceSummaryDto
                {
                    IndexingSourceName = x.IndexingSourceName,
                    TotalArticles = x.TotalArticles
                })
                .ToList(),
            ArticlesByPublicationStatus = GroupByName(details, x => x.PublicationStatus, "Sin estado"),
            ArticlesByAcademicTerm = GroupByName(details, x => x.AcademicTerm, "Sin periodo"),
            ArticlesByResearchLine = GroupByName(details, x => x.ResearchLine, "Sin línea"),
            ArticlesByAuthor = await BuildAuthorSummaryAsync(details.Select(x => x.ArticleKey).Distinct().ToList(), ct),
            ArticlesByFaculty = await BuildFacultySummaryAsync(details.Select(x => x.ArticleKey).Distinct().ToList(), ct),
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
                        column.Item().Element(section => PdfSectionHeader(section, "Líneas, autores y afiliación", "Participación académica y concentración temática."));
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
                            PdfCompactList(table, "Afiliación", dashboard.ArticlesByFaculty.Select(x => (x.Name, x.TotalArticles)).Take(10));
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
            filter.BroadField,
            filter.SpecificField,
            filter.DetailedField,
            filter.VenueName,
            filter.VenueType,
            filter.ArticleYear,
            filter.Quartile,
            filter.IsOpenAccess,
            filter.PeriodDateType);
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

    private static InstitutionalReportingFilterOptionsDto BuildFilterOptions(List<ReportingArticleDetailRow> details)
    {
        return new InstitutionalReportingFilterOptionsDto
        {
            AcademicTerms = Distinct(details.Select(x => x.AcademicTerm)),
            PublicationStatuses = Distinct(details.Select(x => x.PublicationStatus)),
            ResearchLines = Distinct(details.Select(x => x.ResearchLine)),
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
