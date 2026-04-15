using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using tesisproject.frontend.Features.Management.Components;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Reports;
using tesisproject.shared.DTOs.Catalogs;

namespace tesisproject.frontend.Features.Management.Pages
{
    public partial class StatisticalReports : ComponentBase
    {
        // =========================================================
        // INYECCIONES
        // =========================================================
        [Inject] private IInsightsService Insights { get; set; } = default!;
        [Inject] private IApiClient ApiClient { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private ICatalogsService Catalogs { get; set; } = default!;
        [Inject] private IInstitutionalReportingClient InstitutionalReporting { get; set; } = default!;

        // =========================================================
        // ESTADO BASE
        // =========================================================
        protected bool _isLoading = true;
        protected string? _errorMessage;

        protected bool _showAdvancedFilters = false;
        protected bool _showLegacyReporting = false;
        protected bool _showInstitutionalFilters = false;
        protected bool _isLoadingInstitutionalReporting = true;
        protected bool _isRefreshingInstitutionalReporting = false;
        protected bool _isRunningInstitutionalEtl = false;
        protected bool _isInstitutionalPdfGenerating = false;
        protected string? _institutionalReportingError;
        protected InstitutionalReportingDashboardDto? _institutionalDashboard;
        protected InstitutionalReportingFilterDto _institutionalFilter = new();
        protected DateTime? _institutionalLastLoadedAt;
        private CancellationTokenSource? _institutionalLoadCts;

        protected static readonly IReadOnlyList<ReportBadgeItem> ReportHeroBadges =
        [
            new("KPIs", "bi bi-speedometer2"),
            new("Filtros", "bi bi-funnel"),
            new("PDF", "bi bi-file-earmark-pdf"),
            new("Detalle", "bi bi-table")
        ];

        // Datos del dashboard
        protected ArticlesKpiSummaryDto? _kpis;
        protected List<ArticlesByYearDto> _allByYear = new();
        protected List<ArticlesByYearDto> _filteredByYear = new();
        protected List<ArticlesByFieldDto> _byField = new();
        protected List<ArticlesByResearchLineDto> _byResearchLine = new();
        protected ArticlesQuartileStatsDto? _quartileStats;
        protected ArticlesOpenAccessIndexingStatsDto? _accessIndexingStats;
        protected ArticlesTimeToPublicationStatsDto? _timeToPublicationStats;

        // Tendencias
        protected double? _trendGrowthPercent;
        protected int? _trendStartYear;
        protected int? _trendEndYear;

        // =========================================================
        // FILTROS BÁSICOS (cabecera)
        // =========================================================
        protected DateTime? _filterCreatedFrom;
        protected DateTime? _filterCreatedTo;
        protected DateTime? _filterPublicationFrom;
        protected DateTime? _filterPublicationTo;

        // Open Access y Cuartil
        protected string? _filterIsOpenAccess; // "", "true", "false"
        protected string? _selectedQuartile;   // "", "Q1", "Q2", "Q3", "Q4", "SIN_CUARTIL"

        // =========================================================
        // CATÁLOGOS PARA EL PANEL LATERAL
        // =========================================================
        protected List<CatalogItemDto> _projects = new();
        protected List<CatalogItemDto> _academicTerms = new();
        protected List<CatalogItemDto> _researchLines = new();
        protected List<CatalogItemDto> _broadFields = new();
        protected List<CatalogItemDto> _specificFields = new();
        protected List<CatalogItemDto> _detailedFields = new();
        protected List<CatalogItemDto> _publicationStatuses = new();
        protected List<CatalogItemDto> _indexingSources = new();
        protected List<VenueCatalogItemDto> _venues = new();

        // Selección (una por combo)
        protected int _selectedProjectId;
        protected int _selectedAcademicTermId;
        protected int _selectedResearchLineId;
        protected int _selectedBroadFieldId;
        protected int _selectedSpecificFieldId;
        protected int _selectedDetailedFieldId;
        protected int _selectedPublicationStatusId;
        protected int _selectedIndexingSourceId;
        protected int _selectedVenueId;

        // =========================================================
        // SECCIONES DEL PDF
        // =========================================================
        protected bool _includeKpis = true;
        protected bool _includeByYear = true;
        protected bool _includeByField = true;
        protected bool _includeQuartiles = true;
        protected bool _includeByResearchLine = true;

        // =========================================================
        // ESTADO PDF
        // =========================================================
        protected bool _showPdfPreview = false;
        protected bool _isPdfGenerating = false;
        protected string? _pdfPreviewDataUrl;

        // Agregados para tablas
        protected List<(string Name, int Count)> _topBroadFields = new();
        protected List<(string Name, int Count)> _topResearchLines = new();

        // =========================================================
        // TABS / VISTA DETALLADA
        // =========================================================
        protected string _activeTab = "dashboard";

        protected bool _isLoadingDetailed = false;
        protected string? _detailedErrorMessage;
        protected ArticlesDetailedResultDto? _detailedResult;

        // Datos base de la vista detallada
        protected List<ArticleReportRowDto> _detailedRows = new();

        // Lista filtrada que usa el .razor: _detailedFilteredRows
        protected List<ArticleReportRowDto> _detailedFilteredRows = new();

        // Años disponibles para el filtro local
        protected List<int> _detailAvailableYears = new();

        // Filtros locales (año / mes)
        protected int? _detailYearFilter;
        protected int? _detailMonthFilter;

        protected ReportDetailKpisModel _detailKpis = new();

        // =========================================================
        // CICLO DE VIDA
        // =========================================================
        protected override async Task OnInitializedAsync()
        {
            _isLoading = true;
            _errorMessage = null;
            StateHasChanged();

            try
            {
                if (_showLegacyReporting)
                {
                    await LoadCatalogsAsync();
                    await Task.WhenAll(
                        LoadInstitutionalReportingAsync(),
                        LoadDashboardAsync(buildFilterFromUi: false));
                }
                else
                {
                    await LoadInstitutionalReportingAsync();
                }
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }

        // =========================================================
        // CARGA DE CATÁLOGOS
        // =========================================================
        private async Task LoadCatalogsAsync()
        {
            try
            {
                var projectsTask = Catalogs.GetProjectsAsync();
                var termsTask = Catalogs.GetAcademicTermsAsync();
                var researchLinesTask = Catalogs.GetResearchLinesAsync();
                var broadFieldsTask = Catalogs.GetBroadFieldsAsync();
                var statusesTask = Catalogs.GetPublicationStatusesAsync();
                var indexingSourcesTask = Catalogs.GetIndexingSourcesAsync();
                var venuesTask = Catalogs.GetVenuesAsync();

                await Task.WhenAll(
                    projectsTask,
                    termsTask,
                    researchLinesTask,
                    broadFieldsTask,
                    statusesTask,
                    indexingSourcesTask,
                    venuesTask
                );

                _projects = projectsTask.Result ?? new List<CatalogItemDto>();
                _academicTerms = termsTask.Result ?? new List<CatalogItemDto>();
                _researchLines = researchLinesTask.Result ?? new List<CatalogItemDto>();
                _broadFields = broadFieldsTask.Result ?? new List<CatalogItemDto>();
                _publicationStatuses = statusesTask.Result ?? new List<CatalogItemDto>();
                _indexingSources = indexingSourcesTask.Result ?? new List<CatalogItemDto>();
                _venues = venuesTask.Result ?? new List<VenueCatalogItemDto>();

                _specificFields = new List<CatalogItemDto>();
                _detailedFields = new List<CatalogItemDto>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error cargando catálogos: {ex}");
            }
        }

        // =========================================================
        // MANEJO PANEL LATERAL / FILTROS
        // =========================================================

        protected void ToggleAdvancedFilters()
        {
            _showAdvancedFilters = !_showAdvancedFilters;
        }

        private async Task ClearFilters()
        {
            // Fechas
            _filterCreatedFrom = null;
            _filterCreatedTo = null;
            _filterPublicationFrom = null;
            _filterPublicationTo = null;

            // OA / Cuartil
            _filterIsOpenAccess = null;
            _selectedQuartile = null;

            // Dimensiones
            _selectedProjectId = 0;
            _selectedAcademicTermId = 0;
            _selectedResearchLineId = 0;
            _selectedBroadFieldId = 0;
            _selectedSpecificFieldId = 0;
            _selectedDetailedFieldId = 0;
            _selectedPublicationStatusId = 0;
            _selectedIndexingSourceId = 0;
            _selectedVenueId = 0;

            _specificFields.Clear();
            _detailedFields.Clear();

            // También limpiar filtros locales de la vista detallada
            ClearDetailFilters();

            await ApplyFilters();
        }

        // Cascada OCDE: al cambiar área amplia -> specific
        protected async Task OnBroadFieldChanged(ChangeEventArgs e)
        {
            _selectedSpecificFieldId = 0;
            _selectedDetailedFieldId = 0;
            _specificFields.Clear();
            _detailedFields.Clear();

            if (e?.Value == null)
            {
                _selectedBroadFieldId = 0;
                return;
            }

            if (int.TryParse(e.Value.ToString(), out var broadId))
            {
                _selectedBroadFieldId = broadId;

                if (broadId > 0)
                {
                    _specificFields = await Catalogs.GetSpecificFieldsAsync(broadId);
                }
            }
        }

        // Cascada OCDE: al cambiar área específica -> detailed
        protected async Task OnSpecificFieldChanged(ChangeEventArgs e)
        {
            _selectedDetailedFieldId = 0;
            _detailedFields.Clear();

            if (e?.Value == null)
            {
                _selectedSpecificFieldId = 0;
                return;
            }

            if (int.TryParse(e.Value.ToString(), out var specificId))
            {
                _selectedSpecificFieldId = specificId;

                if (specificId > 0)
                {
                    _detailedFields = await Catalogs.GetDetailedFieldsAsync(specificId);
                }
            }
        }

        protected void OnDetailedFieldChanged(ChangeEventArgs e)
        {
            if (e?.Value == null)
            {
                _selectedDetailedFieldId = 0;
                return;
            }

            if (int.TryParse(e.Value.ToString(), out var detailedId))
            {
                _selectedDetailedFieldId = detailedId;
            }
        }

        private ArticlesDashboardFilterDto BuildFilterFromUi()
            => ReportDashboardFilterBuilder.Build(
                _filterCreatedFrom,
                _filterCreatedTo,
                _filterPublicationFrom,
                _filterPublicationTo,
                _filterIsOpenAccess,
                _selectedQuartile,
                _selectedAcademicTermId,
                _selectedProjectId,
                _selectedResearchLineId,
                _selectedBroadFieldId,
                _selectedSpecificFieldId,
                _selectedDetailedFieldId,
                _selectedPublicationStatusId,
                _selectedVenueId,
                _selectedIndexingSourceId);

        protected async Task ApplyFilters()
        {
            await LoadDashboardAsync(buildFilterFromUi: true);

            // Invalida la vista detallada para recargarla con los filtros nuevos
            ResetDetailView();

            _showAdvancedFilters = false; // cerrar panel al aplicar
        }

        // =========================================================
        // CARGA DEL DASHBOARD
        // =========================================================
        private async Task LoadDashboardAsync(bool buildFilterFromUi)
        {
            _isLoading = true;
            _errorMessage = null;
            StateHasChanged();

            try
            {
                var filter = buildFilterFromUi
                    ? BuildFilterFromUi()
                    : new ArticlesDashboardFilterDto();

                var result = await Insights.GetArticlesDashboardAsync(filter);

                if (result == null)
                {
                    ResetDashboardData();
                    return;
                }

                _kpis = result.Kpis;
                _allByYear = result.ByYear ?? new List<ArticlesByYearDto>();
                _byField = result.ByField ?? new List<ArticlesByFieldDto>();
                _byResearchLine = result.ByResearchLine ?? new List<ArticlesByResearchLineDto>();
                _quartileStats = result.Quartiles;
                _accessIndexingStats = result.AccessIndexing;
                _timeToPublicationStats = result.TimeToPublication;

                if (!buildFilterFromUi && _allByYear.Count > 0)
                {
                    SetDefaultDateRange();
                }

                _filteredByYear = _allByYear
                    .OrderBy(x => x.Year)
                    .ToList();

                BuildAggregations();
                RebuildTrendSummary();
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error al cargar datos: {ex.Message}";
                Console.Error.WriteLine(ex);
                ResetDashboardData();
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }

        private void ResetDashboardData()
        {
            _kpis = null;
            _allByYear = new();
            _filteredByYear = new();
            _byField = new();
            _byResearchLine = new();
            _quartileStats = null;
            _accessIndexingStats = null;
            _timeToPublicationStats = null;
            _topBroadFields = new();
            _topResearchLines = new();
            _trendGrowthPercent = null;
            _trendStartYear = null;
            _trendEndYear = null;
        }

        private void SetDefaultDateRange()
        {
            if (_allByYear.Count > 0)
            {
                var minYear = _allByYear.Min(x => x.Year);
                var maxYear = _allByYear.Max(x => x.Year);

                _filterCreatedFrom = new DateTime(minYear, 1, 1);
                _filterCreatedTo = new DateTime(maxYear, 12, 31);
            }
        }

        private void BuildAggregations()
        {
            _topBroadFields = ReportDashboardMetricsBuilder.BuildTopBroadFields(_byField);
            _topResearchLines = ReportDashboardMetricsBuilder.BuildTopResearchLines(_byResearchLine);
        }

        private async Task LoadInstitutionalReportingAsync(bool keepCurrentDashboard = false)
        {
            _institutionalLoadCts?.Cancel();
            _institutionalLoadCts?.Dispose();
            _institutionalLoadCts = new CancellationTokenSource();
            var requestCts = _institutionalLoadCts;

            var canRefreshInPlace = keepCurrentDashboard && _institutionalDashboard != null;

            if (canRefreshInPlace)
            {
                _isRefreshingInstitutionalReporting = true;
            }
            else
            {
                _isLoadingInstitutionalReporting = true;
            }

            _institutionalReportingError = null;

            try
            {
                _institutionalDashboard = await InstitutionalReporting.GetDashboardAsync(_institutionalFilter, requestCts.Token);
                _institutionalLastLoadedAt = DateTime.Now;
            }
            catch (OperationCanceledException) when (requestCts.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                if (!canRefreshInPlace)
                {
                    _institutionalDashboard = null;
                }

                _institutionalReportingError = $"No se pudo cargar la reportería institucional: {ex.Message}";
                Console.Error.WriteLine(ex);
            }
            finally
            {
                if (ReferenceEquals(_institutionalLoadCts, requestCts))
                {
                    _isLoadingInstitutionalReporting = false;
                    _isRefreshingInstitutionalReporting = false;
                    _institutionalLoadCts.Dispose();
                    _institutionalLoadCts = null;
                }
            }
        }

        private void RebuildTrendSummary()
        {
            var trend = ReportDashboardMetricsBuilder.BuildTrendSummary(_filteredByYear);

            _trendGrowthPercent = trend.GrowthPercent;
            _trendStartYear = trend.StartYear;
            _trendEndYear = trend.EndYear;
        }

        private static string ShortenLabel(string? text, int maxLength = 18)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "Sin dato";

            return text.Length <= maxLength
                ? text
                : text.Substring(0, maxLength) + "…";
        }

        // =========================================================
        // TABLA DE CUARTILES
        // =========================================================
        protected IEnumerable<(string Label, int Count, double Percent)> QuartileRows
        {
            get
            {
                if (_quartileStats == null)
                    return Enumerable.Empty<(string, int, double)>();

                int total =
                    _quartileStats.Q1Count +
                    _quartileStats.Q2Count +
                    _quartileStats.Q3Count +
                    _quartileStats.Q4Count +
                    _quartileStats.NoQuartileCount;

                if (total <= 0)
                    return Enumerable.Empty<(string, int, double)>();

                double P(int c) => Math.Round((double)c / total * 100.0, 2);

                var rows = new List<(string Label, int Count, double Percent)>
                {
                    ("Q1",          _quartileStats.Q1Count,         P(_quartileStats.Q1Count)),
                    ("Q2",          _quartileStats.Q2Count,         P(_quartileStats.Q2Count)),
                    ("Q3",          _quartileStats.Q3Count,         P(_quartileStats.Q3Count)),
                    ("Q4",          _quartileStats.Q4Count,         P(_quartileStats.Q4Count)),
                    ("Sin cuartil", _quartileStats.NoQuartileCount, P(_quartileStats.NoQuartileCount))
                };

                return rows;
            }
        }

        // =========================================================
        // ETL + PDF
        // =========================================================
        private string BuildPdfPath()
            => ReportPdfPathBuilder.BuildArticlesStatisticsPath(
                _filterCreatedFrom,
                _filterCreatedTo,
                _includeKpis,
                _includeByYear,
                _includeByField,
                _includeByResearchLine,
                _includeQuartiles);

        protected async Task RunEtlAndReload()
        {
            _isLoading = true;
            _isRunningInstitutionalEtl = true;
            _errorMessage = null;
            _institutionalReportingError = null;
            StateHasChanged();

            try
            {
                await InstitutionalReporting.RunFullLoadAsync();

                await LoadInstitutionalReportingAsync();

                if (_showLegacyReporting)
                {
                    await LoadDashboardAsync(buildFilterFromUi: true);
                    ResetDetailView();
                }
            }
            catch (Exception ex)
            {
                _institutionalReportingError = $"No fue posible actualizar la información: {ex.Message}";
                Console.Error.WriteLine(ex);
            }
            finally
            {
                _isRunningInstitutionalEtl = false;
                _isLoading = false;
                StateHasChanged();
            }
        }

        protected async Task OpenPdfPreviewAsync()
        {
            _isPdfGenerating = true;
            _errorMessage = null;
            _pdfPreviewDataUrl = null;
            _showPdfPreview = true;
            StateHasChanged();

            try
            {
                var path = BuildPdfPath();
                var bytes = await Http.GetByteArrayAsync(path);
                var base64 = Convert.ToBase64String(bytes);
                _pdfPreviewDataUrl = $"data:application/pdf;base64,{base64}";
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error al generar PDF: {ex.Message}";
                Console.Error.WriteLine(ex);
                _showPdfPreview = false;
            }
            finally
            {
                _isPdfGenerating = false;
                StateHasChanged();
            }
        }

        protected string FormatEtlDate(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToString("dd/MM/yyyy HH:mm")
                : "Sin fecha";
        }

        protected string GetEtlStatusClass(string? status)
        {
            return string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
                ? "reports-dw-status reports-dw-status--success"
                : "reports-dw-status reports-dw-status--warning";
        }

        protected static string GetInstitutionalStatusLabel(string? status)
        {
            return string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
                ? "actualizada"
                : "pendiente de actualización";
        }

        protected bool HasInstitutionalReportingData =>
            (_institutionalDashboard?.Health.ArticleRows ?? 0) > 0
            || (_institutionalDashboard?.Health.BatchRows ?? 0) > 0
            || (_institutionalDashboard?.Health.WorkflowStageRows ?? 0) > 0;

        protected int MaxArticlesByYear =>
            Math.Max(1, _institutionalDashboard?.ArticlesByYear.Select(x => x.Count).DefaultIfEmpty(0).Max() ?? 0);

        protected int MaxIndexingArticles =>
            Math.Max(1, _institutionalDashboard?.ArticlesByIndexingSource.Select(x => x.TotalArticles).DefaultIfEmpty(0).Max() ?? 0);

        protected int MaxPublicationStatusArticles =>
            Math.Max(1, _institutionalDashboard?.ArticlesByPublicationStatus.Select(x => x.TotalArticles).DefaultIfEmpty(0).Max() ?? 0);

        protected int MaxResearchLineArticles =>
            Math.Max(1, _institutionalDashboard?.ArticlesByResearchLine.Select(x => x.TotalArticles).DefaultIfEmpty(0).Max() ?? 0);

        protected int MaxVenueArticles =>
            Math.Max(1, _institutionalDashboard?.ArticlesByVenue.Select(x => x.TotalArticles).DefaultIfEmpty(0).Max() ?? 0);

        protected int MaxMonthArticles =>
            Math.Max(1, _institutionalDashboard?.ArticlesByMonth.Select(x => x.TotalArticles).DefaultIfEmpty(0).Max() ?? 0);

        protected int MaxDayArticles =>
            Math.Max(1, _institutionalDashboard?.ArticlesByDay.Select(x => x.TotalArticles).DefaultIfEmpty(0).Max() ?? 0);

        protected int MaxAuthorArticles =>
            Math.Max(1, _institutionalDashboard?.ArticlesByAuthor.Select(x => x.TotalArticles).DefaultIfEmpty(0).Max() ?? 0);

        protected int MaxFacultyArticles =>
            Math.Max(1, _institutionalDashboard?.ArticlesByFaculty.Select(x => x.TotalArticles).DefaultIfEmpty(0).Max() ?? 0);

        protected int MaxVenueTypeArticles =>
            Math.Max(1, _institutionalDashboard?.ArticlesByVenueType.Select(x => x.TotalArticles).DefaultIfEmpty(0).Max() ?? 0);

        protected int MaxQuartileArticles =>
            Math.Max(1, _institutionalDashboard?.ArticlesByQuartile.Select(x => x.TotalArticles).DefaultIfEmpty(0).Max() ?? 0);

        protected int OpenAccessPercent =>
            (_institutionalDashboard?.ScientificProduction.TotalArticles ?? 0) <= 0
                ? 0
                : (int)Math.Round((_institutionalDashboard!.ScientificProduction.OpenAccessArticles / (double)_institutionalDashboard.ScientificProduction.TotalArticles) * 100);

        protected int ProjectResultPercent =>
            (_institutionalDashboard?.ScientificProduction.TotalArticles ?? 0) <= 0
                ? 0
                : (int)Math.Round((_institutionalDashboard!.ScientificProduction.ProjectResultArticles / (double)_institutionalDashboard.ScientificProduction.TotalArticles) * 100);

        protected string InstitutionalLastLoadedLabel =>
            _institutionalLastLoadedAt.HasValue
                ? _institutionalLastLoadedAt.Value.ToString("dd/MM/yyyy HH:mm:ss")
                : "Sin lectura reciente";

        protected string InstitutionalScopeLabel =>
            ActiveInstitutionalFilterChips.Count == 0
                ? "Vista general institucional"
                : $"{ActiveInstitutionalFilterChips.Count} filtros activos";

        protected string InstitutionalPeriodLabel =>
            string.Equals(_institutionalFilter.PeriodDateType, "published", StringComparison.OrdinalIgnoreCase)
                ? "Fecha de publicación"
                : "Fecha de registro";

        protected string TopInstitutionalYearLabel
        {
            get
            {
                var item = _institutionalDashboard?.ArticlesByYear
                    .OrderByDescending(x => x.Count)
                    .ThenByDescending(x => x.Year)
                    .FirstOrDefault();

                return item is null ? "Sin dato" : $"{item.Year} · {item.Count:N0}";
            }
        }

        protected string TopInstitutionalResearchLineLabel
        {
            get
            {
                var item = _institutionalDashboard?.ArticlesByResearchLine
                    .OrderByDescending(x => x.TotalArticles)
                    .FirstOrDefault();

                return item is null ? "Sin dato" : $"{ShortenLabel(item.Name, 34)} · {item.TotalArticles:N0}";
            }
        }

        protected string TopInstitutionalVenueLabel
        {
            get
            {
                var item = _institutionalDashboard?.ArticlesByVenue
                    .OrderByDescending(x => x.TotalArticles)
                    .FirstOrDefault();

                return item is null ? "Sin dato" : $"{ShortenLabel(item.VenueName, 34)} · {item.TotalArticles:N0}";
            }
        }

        protected string TopInstitutionalAuthorLabel
        {
            get
            {
                var item = _institutionalDashboard?.ArticlesByAuthor
                    .OrderByDescending(x => x.TotalArticles)
                    .FirstOrDefault();

                return item is null ? "Sin dato" : $"{ShortenLabel(item.Name, 34)} · {item.TotalArticles:N0}";
            }
        }

        protected string TopBroadFieldLabel => BuildTopFieldLabel(x => x.BroadField, "Sin campo amplio");

        protected string TopSpecificFieldLabel => BuildTopFieldLabel(x => x.SpecificField, "Sin campo específico");

        protected string TopDetailedFieldLabel => BuildTopFieldLabel(x => x.DetailedField, "Sin campo detallado");

        protected string TopQuartileLabel
        {
            get
            {
                var item = _institutionalDashboard?.ArticlesByQuartile
                    .OrderByDescending(x => x.TotalArticles)
                    .FirstOrDefault();

                return item is null ? "Sin dato" : $"{ShortenLabel(item.Name, 28)} · {item.TotalArticles:N0}";
            }
        }

        private string BuildTopFieldLabel(Func<ReportingFieldSummaryDto, string> selector, string fallback)
        {
            var item = _institutionalDashboard?.ArticlesByField
                .GroupBy(x => string.IsNullOrWhiteSpace(selector(x)) ? fallback : selector(x).Trim())
                .Select(g => new { Name = g.Key, TotalArticles = g.Sum(x => x.TotalArticles) })
                .OrderByDescending(x => x.TotalArticles)
                .FirstOrDefault();

            return item is null ? "Sin dato" : $"{ShortenLabel(item.Name, 30)} · {item.TotalArticles:N0}";
        }

        protected IReadOnlyList<(string Label, string Value)> ActiveInstitutionalFilterChips
        {
            get
            {
                var chips = new List<(string Label, string Value)>();

                AddDateChip(chips, "Creación desde", _institutionalFilter.CreatedFrom);
                AddDateChip(chips, "Creación hasta", _institutionalFilter.CreatedTo);
                AddDateChip(chips, "Publicación desde", _institutionalFilter.PublishedFrom);
                AddDateChip(chips, "Publicación hasta", _institutionalFilter.PublishedTo);
                AddChip(chips, "Periodo", _institutionalFilter.AcademicTerm);
                AddChip(chips, "Estado", _institutionalFilter.PublicationStatus);
                AddChip(chips, "Línea", _institutionalFilter.ResearchLine);
                AddChip(chips, "Campo amplio", _institutionalFilter.BroadField);
                AddChip(chips, "Campo específico", _institutionalFilter.SpecificField);
                AddChip(chips, "Campo detallado", _institutionalFilter.DetailedField);
                AddChip(chips, "Revista", _institutionalFilter.VenueName);
                AddChip(chips, "Tipo de publicación", _institutionalFilter.VenueType);
                AddChip(chips, "Cuartil", _institutionalFilter.Quartile);

                if (string.Equals(_institutionalFilter.PeriodDateType, "published", StringComparison.OrdinalIgnoreCase))
                {
                    AddChip(chips, "Periodo temporal", InstitutionalPeriodLabel);
                }

                if (_institutionalFilter.ArticleYear.HasValue)
                {
                    chips.Add(("Año", _institutionalFilter.ArticleYear.Value.ToString()));
                }

                if (_institutionalFilter.IsOpenAccess.HasValue)
                {
                    chips.Add(("Acceso", _institutionalFilter.IsOpenAccess.Value ? "Open Access" : "No Open Access"));
                }

                return chips;
            }
        }

        protected static string GetBarWidth(int value, int maxValue)
        {
            if (maxValue <= 0 || value <= 0)
            {
                return "0%";
            }

            var percent = Math.Clamp((value / (double)maxValue) * 100, 4, 100);
            return $"{percent:0.##}%";
        }

        protected string OpenAccessDonutStyle =>
            $"background: conic-gradient(#97b067 0 {OpenAccessPercent}%, rgba(67, 86, 99, 0.12) {OpenAccessPercent}% 100%);";

        private static void AddChip(List<(string Label, string Value)> chips, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                chips.Add((label, value.Trim()));
            }
        }

        private static void AddDateChip(List<(string Label, string Value)> chips, string label, DateTime? value)
        {
            if (value.HasValue)
            {
                chips.Add((label, value.Value.ToString("dd/MM/yyyy")));
            }
        }

        protected async Task ApplyInstitutionalFiltersAsync()
        {
            await LoadInstitutionalReportingAsync(keepCurrentDashboard: true);
            StateHasChanged();
        }

        protected async Task ClearInstitutionalFiltersAsync()
        {
            _institutionalFilter = new InstitutionalReportingFilterDto();
            await LoadInstitutionalReportingAsync(keepCurrentDashboard: true);
            StateHasChanged();
        }

        protected void ToggleInstitutionalFilters()
        {
            _showInstitutionalFilters = !_showInstitutionalFilters;
        }

        protected async Task OpenInstitutionalPdfPreviewAsync()
        {
            _isInstitutionalPdfGenerating = true;
            _errorMessage = null;
            _pdfPreviewDataUrl = null;
            _showPdfPreview = true;
            StateHasChanged();

            try
            {
                var path = tesisproject.frontend.Services.Implementations.InstitutionalReportingClient
                    .BuildDashboardUrl(_institutionalFilter, "api/reporting/dashboard/pdf");
                var bytes = await Http.GetByteArrayAsync(path);
                _pdfPreviewDataUrl = $"data:application/pdf;base64,{Convert.ToBase64String(bytes)}";
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error al generar PDF institucional: {ex.Message}";
                _showPdfPreview = false;
                Console.Error.WriteLine(ex);
            }
            finally
            {
                _isInstitutionalPdfGenerating = false;
                StateHasChanged();
            }
        }

        protected void ClosePdfPreview()
        {
            _showPdfPreview = false;
            _pdfPreviewDataUrl = null;
            _isPdfGenerating = false;
            _isInstitutionalPdfGenerating = false;
        }

        // =========================================================
        // ALTURAS DINÁMICAS PARA GRÁFICOS
        // =========================================================
        protected string YearChartHeight => ReportChartOptionsFactory.GetChartHeight(_filteredByYear.Count);
        protected string FieldChartHeight => ReportChartOptionsFactory.GetChartHeight(_topBroadFields.Count);
        protected string ResearchLineChartHeight => ReportChartOptionsFactory.GetChartHeight(_topResearchLines.Count);
        protected string TrendChartHeight => ReportChartOptionsFactory.GetChartHeight(_filteredByYear.Count);
        protected string QuartileChartHeight => ReportChartOptionsFactory.GetQuartileChartHeight(_quartileStats);

        // =========================================================
        // OPCIONES DE GRÁFICOS (ECharts)
        // =========================================================
        protected object? YearChartOption => ReportChartOptionsFactory.BuildVerticalBar(
            _filteredByYear?.Select(x => x.Year.ToString()).ToArray(),
            _filteredByYear?.Select(x => x.Count).ToArray(),
            "Artículos",
            "#435663"
        );

        protected object? FieldChartOption => ReportChartOptionsFactory.BuildVerticalBar(
            _topBroadFields?.Select(x => ShortenLabel(x.Name, 18)).ToArray(),
            _topBroadFields?.Select(x => x.Count).ToArray(),
            "Artículos",
            "#97B067",
            true
        );

        protected object? ResearchLineChartOption => ReportChartOptionsFactory.BuildHorizontalBar(
            _topResearchLines?.Select(x => ShortenLabel(x.Name, 25)).ToArray(),
            _topResearchLines?.Select(x => x.Count).ToArray(),
            "#2F5249"
        );

        protected object? QuartileChartOption => ReportChartOptionsFactory.BuildQuartileDonut(_quartileStats);

        protected object? YearTrendChartOption => ReportChartOptionsFactory.BuildTrend(_filteredByYear);

        // =========================================================
        // TABS / VISTA DETALLADA (CARGA DE DATOS)
        // =========================================================

        protected string GetTabButtonClass(string tabKey)
        {
            var isActive = _activeTab == tabKey;
            return isActive
                ? "inline-flex items-center px-3 py-1.5 border-b-2 border-[#7A1E19] text-xs font-semibold text-[#7A1E19]"
                : "inline-flex items-center px-3 py-1.5 border-b-2 border-transparent text-xs font-medium text-slate-500 hover:text-slate-800 hover:border-slate-200";
        }

        protected async Task SwitchTabAsync(string tab)
        {
            if (_activeTab == tab)
                return;

            _activeTab = tab;

            if (tab == "detail" && _detailedResult == null && !_isLoadingDetailed)
            {
                await LoadDetailedAsync();
            }
        }

        protected Task SwitchToDashboard() => SwitchTabAsync("dashboard");
        protected Task SwitchToDetail() => SwitchTabAsync("detail");

        private async Task LoadDetailedAsync()
        {
            _isLoadingDetailed = true;
            _detailedErrorMessage = null;
            StateHasChanged();

            try
            {
                var filter = BuildFilterFromUi();
                var result = await Insights.GetArticlesDetailedAsync(filter);

                _detailedResult = result;
                _detailedRows = result?.Rows ?? new List<ArticleReportRowDto>();

                // Inicializar lista filtrada
                _detailedFilteredRows = _detailedRows.ToList();

                // Construir lista de años disponibles (para combo local)
                _detailAvailableYears = _detailedRows
                    .Where(r => r.CreatedDate.HasValue)
                    .Select(r => r.CreatedDate!.Value.Year)
                    .Distinct()
                    .OrderBy(y => y)
                    .ToList();

                // Reset de filtros locales
                _detailYearFilter = null;
                _detailMonthFilter = null;

                // Recalcular KPIs de la vista detallada
                RebuildDetailKpis();
            }
            catch (Exception ex)
            {
                _detailedErrorMessage = $"Error al cargar vista detallada: {ex.Message}";
                Console.Error.WriteLine(ex);
                _detailedResult = null;
                _detailedRows = new();
                _detailedFilteredRows = new();
                _detailAvailableYears = new();
                _detailYearFilter = null;
                _detailMonthFilter = null;
                _detailKpis = new ReportDetailKpisModel();
            }
            finally
            {
                _isLoadingDetailed = false;
                StateHasChanged();
            }
        }

        private void ResetDetailView()
        {
            _detailedResult = null;
            _detailedRows = new();
            _detailedFilteredRows = new();
            _detailedErrorMessage = null;
            _isLoadingDetailed = false;
            _detailAvailableYears = new();
            _detailYearFilter = null;
            _detailMonthFilter = null;
            _detailKpis = new ReportDetailKpisModel();
        }

        // =========================================================
        // VISTA DETALLADA – FILTROS LOCALES (AÑO / MES)
        // =========================================================

        protected void OnDetailYearChanged(ChangeEventArgs e)
        {
            _detailYearFilter = null;

            if (int.TryParse(e.Value?.ToString(), out var year))
            {
                _detailYearFilter = year;
            }

            ApplyDetailFilters();
        }

        protected void OnDetailMonthChanged(ChangeEventArgs e)
        {
            _detailMonthFilter = null;

            if (int.TryParse(e.Value?.ToString(), out var month))
            {
                _detailMonthFilter = month;
            }

            ApplyDetailFilters();
        }

        protected void ClearDetailFilters()
        {
            _detailYearFilter = null;
            _detailMonthFilter = null;

            // Restaurar lista completa si ya se cargó la vista detallada
            _detailedFilteredRows = _detailedRows.ToList();
            RebuildDetailKpis();
        }

        private void ApplyDetailFilters()
        {
            if (_detailedRows == null || _detailedRows.Count == 0)
            {
                _detailedFilteredRows = new List<ArticleReportRowDto>();
                _detailKpis = new ReportDetailKpisModel();
                return;
            }

            IEnumerable<ArticleReportRowDto> query = _detailedRows;

            if (_detailYearFilter.HasValue)
            {
                query = query.Where(r =>
                    r.CreatedDate.HasValue &&
                    r.CreatedDate.Value.Year == _detailYearFilter.Value);
            }

            if (_detailMonthFilter.HasValue)
            {
                query = query.Where(r =>
                    r.CreatedDate.HasValue &&
                    r.CreatedDate.Value.Month == _detailMonthFilter.Value);
            }

            _detailedFilteredRows = query.ToList();
            RebuildDetailKpis();
        }

        private void RebuildDetailKpis()
        {
            var kpis = new ReportDetailKpisModel();

            if (_detailedFilteredRows == null || _detailedFilteredRows.Count == 0)
            {
                _detailKpis = kpis;
                return;
            }

            // Total de artículos en la tabla (suma ArticleCount)
            kpis.TotalArticles = _detailedFilteredRows.Sum(r => r.ArticleCount);

            // Año actual para "Publicados este año"
            var currentYear = DateTime.Now.Year;

            kpis.PublishedThisYear = _detailedFilteredRows
                .Where(r => r.PublicationDate.HasValue &&
                            r.PublicationDate.Value.Year == currentYear)
                .Sum(r => r.ArticleCount);

            // Q1 + Q2
            kpis.Q1Q2Count = _detailedFilteredRows
                .Where(r => string.Equals(r.Quartile, "Q1", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.Quartile, "Q2", StringComparison.OrdinalIgnoreCase))
                .Sum(r => r.ArticleCount);

            // Open Access
            kpis.OpenAccessCount = _detailedFilteredRows
                .Where(r => r.IsOpenAccess)
                .Sum(r => r.ArticleCount);

            // Indexados en Scopus
            kpis.IndexedInScopusCount = _detailedFilteredRows
                .Where(r => r.IndexedInScopus)
                .Sum(r => r.ArticleCount);

            _detailKpis = kpis;
        }
    }
}
