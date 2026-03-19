using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
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

        // =========================================================
        // ESTADO BASE
        // =========================================================
        protected bool _isLoading = true;
        protected string? _errorMessage;

        protected bool _showAdvancedFilters = false;

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

        // Modelo de mes para el combo local (DetailMonths)
        protected sealed class DetailMonthItem
        {
            public int Value { get; set; }
            public string Label { get; set; } = string.Empty;
        }

        // Lista de meses mostrada en el .razor
        protected List<DetailMonthItem> DetailMonths { get; } = new()
        {
            new DetailMonthItem { Value = 1,  Label = "Enero" },
            new DetailMonthItem { Value = 2,  Label = "Febrero" },
            new DetailMonthItem { Value = 3,  Label = "Marzo" },
            new DetailMonthItem { Value = 4,  Label = "Abril" },
            new DetailMonthItem { Value = 5,  Label = "Mayo" },
            new DetailMonthItem { Value = 6,  Label = "Junio" },
            new DetailMonthItem { Value = 7,  Label = "Julio" },
            new DetailMonthItem { Value = 8,  Label = "Agosto" },
            new DetailMonthItem { Value = 9,  Label = "Septiembre" },
            new DetailMonthItem { Value = 10, Label = "Octubre" },
            new DetailMonthItem { Value = 11, Label = "Noviembre" },
            new DetailMonthItem { Value = 12, Label = "Diciembre" },
        };

        // KPIs locales de la vista detallada
        protected sealed class DetailKpisModel
        {
            public int TotalArticles { get; set; }
            public int PublishedThisYear { get; set; }
            public int Q1Q2Count { get; set; }
            public int OpenAccessCount { get; set; }
            public int IndexedInScopusCount { get; set; }
        }

        protected DetailKpisModel _detailKpis = new();

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
                await LoadCatalogsAsync();
                await LoadDashboardAsync(buildFilterFromUi: false);
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

        // Construye el DTO con todos los filtros actuales
        private ArticlesDashboardFilterDto BuildFilterFromUi()
        {
            var filter = new ArticlesDashboardFilterDto
            {
                CreatedFrom = _filterCreatedFrom,
                CreatedTo = _filterCreatedTo,
                PublicationFrom = _filterPublicationFrom,
                PublicationTo = _filterPublicationTo
            };

            // Open Access
            if (!string.IsNullOrEmpty(_filterIsOpenAccess) &&
                bool.TryParse(_filterIsOpenAccess, out var oaValue))
            {
                filter.IsOpenAccess = oaValue;
            }

            // Cuartil
            if (!string.IsNullOrEmpty(_selectedQuartile))
            {
                filter.Quartiles = new List<string> { _selectedQuartile };
            }

            // Dimensiones: si el combo > 0, mandamos una lista con ese Id
            if (_selectedAcademicTermId > 0)
                filter.AcademicTermKeys = new List<int> { _selectedAcademicTermId };

            if (_selectedProjectId > 0)
                filter.ProjectKeys = new List<int> { _selectedProjectId };

            if (_selectedResearchLineId > 0)
                filter.ResearchLineKeys = new List<int> { _selectedResearchLineId };

            // Campo OCDE – prioridad: detallado > específico > amplio
            if (_selectedDetailedFieldId > 0)
                filter.FieldKeys = new List<int> { _selectedDetailedFieldId };
            else if (_selectedSpecificFieldId > 0)
                filter.FieldKeys = new List<int> { _selectedSpecificFieldId };
            else if (_selectedBroadFieldId > 0)
                filter.FieldKeys = new List<int> { _selectedBroadFieldId };

            if (_selectedPublicationStatusId > 0)
                filter.PublicationStatusKeys = new List<int> { _selectedPublicationStatusId };

            if (_selectedVenueId > 0)
                filter.VenueKeys = new List<int> { _selectedVenueId };

            if (_selectedIndexingSourceId > 0)
                filter.IndexingSourceKeys = new List<int> { _selectedIndexingSourceId };

            return filter;
        }

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
            _topBroadFields = _byField
                .GroupBy(f => string.IsNullOrWhiteSpace(f.BroadFieldName) ? "Sin área" : f.BroadFieldName!)
                .Select(g => (Name: g.Key, Count: g.Sum(x => x.Count)))
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            _topResearchLines = _byResearchLine
                .GroupBy(r => string.IsNullOrWhiteSpace(r.ResearchLineName) ? "Sin línea" : r.ResearchLineName!)
                .Select(g => (Name: g.Key, Count: g.Sum(x => x.Count)))
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();
        }

        private void RebuildTrendSummary()
        {
            if (_filteredByYear == null || _filteredByYear.Count < 2)
            {
                _trendGrowthPercent = null;
                _trendStartYear = null;
                _trendEndYear = null;
                return;
            }

            var ordered = _filteredByYear.OrderBy(x => x.Year).ToList();
            _trendStartYear = ordered.First().Year;
            _trendEndYear = ordered.Last().Year;

            var firstValue = ordered.First().Count;
            var lastValue = ordered.Last().Count;

            if (firstValue <= 0)
            {
                _trendGrowthPercent = null;
                return;
            }

            _trendGrowthPercent = Math.Round(((lastValue - firstValue) / (double)firstValue) * 100.0, 2);
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
        {
            var parts = new List<string>();

            if (_filterCreatedFrom.HasValue)
                parts.Add($"minYear={_filterCreatedFrom.Value.Year}");

            if (_filterCreatedTo.HasValue)
                parts.Add($"maxYear={_filterCreatedTo.Value.Year}");

            parts.Add($"includeKpis={_includeKpis.ToString().ToLower()}");
            parts.Add($"includeByYear={_includeByYear.ToString().ToLower()}");
            parts.Add($"includeByField={_includeByField.ToString().ToLower()}");
            parts.Add($"includeByResearchLine={_includeByResearchLine.ToString().ToLower()}");
            parts.Add($"includeQuartiles={_includeQuartiles.ToString().ToLower()}");

            var query = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;

            return $"api/reports/articles/statistics/pdf{query}";
        }

        protected async Task RunEtlAndReload()
        {
            _isLoading = true;
            _errorMessage = null;
            StateHasChanged();

            try
            {
                await ApiClient.PostAsync<object, object>(
                    "api/bi/etl/full",
                    new { });

                await LoadDashboardAsync(buildFilterFromUi: true);
                ResetDetailView();
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error en ETL: {ex.Message}";
                Console.Error.WriteLine(ex);
            }
            finally
            {
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

        protected void ClosePdfPreview()
        {
            _showPdfPreview = false;
            _pdfPreviewDataUrl = null;
        }

        // =========================================================
        // ALTURAS DINÁMICAS PARA GRÁFICOS
        // =========================================================
        protected string YearChartHeight => GetChartHeight(_filteredByYear.Count);
        protected string FieldChartHeight => GetChartHeight(_topBroadFields.Count);
        protected string ResearchLineChartHeight => GetChartHeight(_topResearchLines.Count);
        protected string TrendChartHeight => GetChartHeight(_filteredByYear.Count);

        private string GetChartHeight(int count) => count switch
        {
            <= 4 => "220px",
            <= 8 => "260px",
            <= 15 => "320px",
            _ => "380px"
        };

        protected string QuartileChartHeight
        {
            get
            {
                if (_quartileStats == null) return "260px";
                int total = _quartileStats.Q1Count + _quartileStats.Q2Count +
                           _quartileStats.Q3Count + _quartileStats.Q4Count +
                           _quartileStats.NoQuartileCount;

                return total switch
                {
                    <= 0 => "260px",
                    <= 20 => "280px",
                    <= 50 => "320px",
                    _ => "360px"
                };
            }
        }

        // =========================================================
        // OPCIONES DE GRÁFICOS (ECharts)
        // =========================================================
        protected object? YearChartOption => BuildBarChartOption(
            _filteredByYear?.Select(x => x.Year.ToString()).ToArray(),
            _filteredByYear?.Select(x => x.Count).ToArray(),
            "#7A1E19",
            "Artículos"
        );

        protected object? FieldChartOption => BuildBarChartOption(
            _topBroadFields?.Select(x => ShortenLabel(x.Name, 18)).ToArray(),
            _topBroadFields?.Select(x => x.Count).ToArray(),
            "#9F3B31",
            "Artículos",
            true
        );

        protected object? ResearchLineChartOption => BuildHorizontalBarChartOption(
            _topResearchLines?.Select(x => ShortenLabel(x.Name, 25)).ToArray(),
            _topResearchLines?.Select(x => x.Count).ToArray(),
            "#C25B4A"
        );

        protected object? QuartileChartOption => BuildPieChartOption();

        protected object? YearTrendChartOption => BuildYearTrendChartOption();

        private object? BuildBarChartOption(string[]? categories, int[]? data, string color, string seriesName, bool rotateLabels = false)
        {
            if (categories == null || data == null || categories.Length == 0)
                return null;

            return new
            {
                tooltip = new { trigger = "axis" },
                toolbox = new
                {
                    show = true,
                    orient = "horizontal",
                    right = "8",
                    top = "8",
                    feature = new
                    {
                        saveAsImage = new
                        {
                            show = true,
                            title = "Descargar PNG",
                            pixelRatio = 2
                        }
                    }
                },
                xAxis = new
                {
                    type = "category",
                    data = categories,
                    axisLabel = rotateLabels
                        ? (object)new { rotate = 30, interval = 0 }
                        : new { interval = 0 } as object
                },
                yAxis = new { type = "value" },
                series = new object[]
                {
                    new
                    {
                        name = seriesName,
                        type = "bar",
                        data = data,
                        itemStyle = new { color = color }
                    }
                }
            };
        }

        private object? BuildHorizontalBarChartOption(string[]? categories, int[]? data, string color)
        {
            if (categories == null || data == null || categories.Length == 0)
                return null;

            return new
            {
                tooltip = new { trigger = "axis", axisPointer = new { type = "shadow" } },
                toolbox = new
                {
                    show = true,
                    orient = "horizontal",
                    right = "8",
                    top = "8",
                    feature = new
                    {
                        saveAsImage = new
                        {
                            show = true,
                            title = "Descargar PNG",
                            pixelRatio = 2
                        }
                    }
                },
                grid = new { left = "25%", right = "5%", bottom = "5%", top = "10%" },
                xAxis = new { type = "value" },
                yAxis = new
                {
                    type = "category",
                    data = categories,
                    axisLabel = new { interval = 0 }
                },
                series = new object[]
                {
                    new
                    {
                        name = "Artículos",
                        type = "bar",
                        data = data,
                        itemStyle = new { color = color }
                    }
                }
            };
        }

        private object? BuildPieChartOption()
        {
            if (_quartileStats == null)
                return null;

            int total = _quartileStats.Q1Count + _quartileStats.Q2Count +
                       _quartileStats.Q3Count + _quartileStats.Q4Count +
                       _quartileStats.NoQuartileCount;

            if (total <= 0)
                return null;

            var data = new List<Dictionary<string, object>>
            {
                new() { ["value"] = _quartileStats.Q1Count, ["name"] = "Q1" },
                new() { ["value"] = _quartileStats.Q2Count, ["name"] = "Q2" },
                new() { ["value"] = _quartileStats.Q3Count, ["name"] = "Q3" },
                new() { ["value"] = _quartileStats.Q4Count, ["name"] = "Q4" },
                new() { ["value"] = _quartileStats.NoQuartileCount, ["name"] = "Sin cuartil" }
            };

            return new
            {
                tooltip = new { trigger = "item", formatter = "{b}: {c} artículos ({d}%)" },
                legend = new { orient = "vertical", left = "left" },
                series = new object[]
                {
                    new
                    {
                        name = "Distribución por cuartil",
                        type = "pie",
                        radius = new[] { "45%", "70%" },
                        avoidLabelOverlap = true,
                        center = new[] { "55%", "55%" },
                        data = data,
                        label = new { show = true, formatter = "{b}: {d}%" },
                        labelLine = new { show = true }
                    }
                }
            };
        }

        private object? BuildYearTrendChartOption()
        {
            if (_filteredByYear == null || _filteredByYear.Count == 0)
                return null;

            var years = _filteredByYear.Select(x => x.Year.ToString()).ToArray();
            var counts = _filteredByYear.Select(x => x.Count).ToArray();

            return new
            {
                tooltip = new { trigger = "axis" },
                toolbox = new
                {
                    show = true,
                    orient = "horizontal",
                    right = "8",
                    top = "8",
                    feature = new
                    {
                        saveAsImage = new
                        {
                            show = true,
                            title = "Descargar PNG",
                            pixelRatio = 2
                        }
                    }
                },
                xAxis = new
                {
                    type = "category",
                    data = years
                },
                yAxis = new { type = "value" },
                series = new object[]
                {
                    new
                    {
                        name = "Artículos",
                        type = "line",
                        smooth = true,
                        data = counts,
                        itemStyle = new { color = "#0f766e" },
                        areaStyle = new { opacity = 0.08 }
                    }
                }
            };
        }

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
                _detailKpis = new DetailKpisModel();
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
            _detailKpis = new DetailKpisModel();
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
                _detailKpis = new DetailKpisModel();
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
            var kpis = new DetailKpisModel();

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
