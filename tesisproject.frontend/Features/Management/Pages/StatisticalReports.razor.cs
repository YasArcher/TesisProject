using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Features.Management.Pages
{
    public partial class StatisticalReports : ComponentBase
    {
        [Inject] private IInsightsService Insights { get; set; } = default!;
        [Inject] private IApiClient ApiClient { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;

        // Estado base
        protected bool _isLoading = true;
        protected string? _errorMessage;

        protected ArticlesKpiSummaryDto? _kpis;
        protected List<ArticlesByYearDto> _allByYear = new();
        protected List<ArticlesByYearDto> _filteredByYear = new();
        protected List<ArticlesByFieldDto> _byField = new();
        protected List<ArticlesByResearchLineDto> _byResearchLine = new();
        protected ArticlesQuartileStatsDto? _quartileStats;

        // Secciones a incluir en el PDF
        protected bool _includeKpis = true;
        protected bool _includeByYear = true;
        protected bool _includeByField = true;
        protected bool _includeQuartiles = true;
        protected bool _includeByResearchLine = true;

        // Filtros básicos (años)
        protected int? _filterMinYear;
        protected int? _filterMaxYear;

        // Top agregados para tablas/leyendas
        protected List<(string Name, int Count)> _topBroadFields = new();
        protected List<(string Name, int Count)> _topResearchLines = new();

        // Estado del PDF en el frontend
        protected bool _showPdfPreview = false;
        protected bool _isPdfGenerating = false;
        protected string? _pdfPreviewDataUrl;

        // =====================================================================
        // Ciclo de vida
        // =====================================================================

        protected override async Task OnInitializedAsync()
        {
            await LoadDashboardAsync(buildFilterFromUi: false);
        }

        // =====================================================================
        // Filas de cuartiles para la tabla
        // =====================================================================

        private IEnumerable<(string Label, int Count, double Percent)> QuartileRows
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

        // =====================================================================
        // Carga del dashboard (endpoint unificado)
        // =====================================================================

        private async Task LoadDashboardAsync(bool buildFilterFromUi)
        {
            _isLoading = true;
            _errorMessage = null;
            StateHasChanged();

            try
            {
                ArticlesDashboardFilterDto? filter = null;

                if (buildFilterFromUi)
                {
                    filter = BuildFilterFromUi();
                }

                var result = await Insights.GetArticlesDashboardAsync(filter);

                if (result == null)
                {
                    _kpis = null;
                    _allByYear = new();
                    _filteredByYear = new();
                    _byField = new();
                    _byResearchLine = new();
                    _quartileStats = null;
                    BuildAggregations();
                    return;
                }

                _kpis = result.Kpis;
                _allByYear = result.ByYear ?? new List<ArticlesByYearDto>();
                _byField = result.ByField ?? new List<ArticlesByFieldDto>();
                _byResearchLine = result.ByResearchLine ?? new List<ArticlesByResearchLineDto>();

                // 👇 AQUÍ ESTABA EL ERROR:
                // _quartileStats = result.QuartileStats;
                // Como ArticlesDashboardDto no tiene esa propiedad, por ahora dejamos los cuartiles sin fuente.
                _quartileStats = null;

                // Primer load: si no hay filtro de UI, fijamos rango base de años
                if (!buildFilterFromUi && _allByYear.Count > 0)
                {
                    _filterMinYear = _allByYear.Min(x => x.Year);
                    _filterMaxYear = _allByYear.Max(x => x.Year);
                }

                // Serie para la gráfica de años
                _filteredByYear = _allByYear
                    .OrderBy(x => x.Year)
                    .ToList();

                BuildAggregations();
            }
            catch (Exception ex)
            {
                _errorMessage = "Ocurrió un error al cargar los datos de reportería. " +
                                "Por favor, verifica la conexión con el servidor o ejecuta primero el ETL.";
                Console.Error.WriteLine(ex);
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }

        // =====================================================================
        // Construcción de filtro (solo años → fechas creadas)
        // =====================================================================

        private ArticlesDashboardFilterDto BuildFilterFromUi()
        {
            var filter = new ArticlesDashboardFilterDto();

            // Mapeamos años a CreatedFrom / CreatedTo
            if (_filterMinYear.HasValue)
            {
                filter.CreatedFrom = new DateTime(_filterMinYear.Value, 1, 1);
            }

            if (_filterMaxYear.HasValue)
            {
                filter.CreatedTo = new DateTime(_filterMaxYear.Value, 12, 31);
            }

            return filter;
        }

        // =====================================================================
        // Filtros y helpers
        // =====================================================================

        protected async Task ApplyFilters()
        {
            await LoadDashboardAsync(buildFilterFromUi: true);
        }

        private void BuildAggregations()
        {
            // Top áreas amplias
            _topBroadFields = _byField
                .GroupBy(f => string.IsNullOrWhiteSpace(f.BroadFieldName) ? "Sin área" : f.BroadFieldName!)
                .Select(g => (Name: g.Key, Count: g.Sum(x => x.Count)))
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            // Top líneas de investigación
            _topResearchLines = _byResearchLine
                .GroupBy(r => string.IsNullOrWhiteSpace(r.ResearchLineName) ? "Sin línea" : r.ResearchLineName!)
                .Select(g => (Name: g.Key, Count: g.Sum(x => x.Count)))
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();
        }

        private static string ShortenLabel(string? text, int maxLength = 18)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "Sin dato";

            return text.Length <= maxLength
                ? text
                : text.Substring(0, maxLength) + "…";
        }

        // =====================================================================
        // Construcción del path para el PDF
        // =====================================================================

        private string BuildPdfPath()
        {
            var parts = new List<string>();

            if (_filterMinYear.HasValue)
                parts.Add($"minYear={_filterMinYear.Value}");

            if (_filterMaxYear.HasValue)
                parts.Add($"maxYear={_filterMaxYear.Value}");

            parts.Add($"includeKpis={_includeKpis.ToString().ToLower()}");
            parts.Add($"includeByYear={_includeByYear.ToString().ToLower()}");
            parts.Add($"includeByField={_includeByField.ToString().ToLower()}");
            parts.Add($"includeByResearchLine={_includeByResearchLine.ToString().ToLower()}");
            parts.Add($"includeQuartiles={_includeQuartiles.ToString().ToLower()}");

            var query = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;

            return $"api/reports/articles/statistics/pdf{query}";
        }

        // =====================================================================
        // Acciones (ETL + PDF)
        // =====================================================================

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

                // Después de ejecutar el ETL, volvemos a cargar dashboard con filtros actuales
                await LoadDashboardAsync(buildFilterFromUi: true);
            }
            catch (Exception ex)
            {
                _errorMessage = "No se pudo ejecutar el ETL desde la interfaz. " +
                                "Verifica el backend o consulta al administrador.";
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
                _errorMessage = "No se pudo generar la previsualización del PDF. " +
                                "Verifica que el backend esté disponible.";
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

        // =====================================================================
        // Alturas dinámicas para las gráficas
        // =====================================================================

        protected string YearChartHeight =>
            _filteredByYear.Count switch
            {
                <= 4 => "220px",
                <= 8 => "260px",
                <= 15 => "320px",
                _ => "380px"
            };

        protected string FieldChartHeight =>
            _topBroadFields.Count switch
            {
                <= 4 => "220px",
                <= 8 => "260px",
                <= 15 => "320px",
                _ => "380px"
            };

        protected string ResearchLineChartHeight =>
            _topResearchLines.Count switch
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
                if (_quartileStats == null)
                    return "260px";

                int total =
                    _quartileStats.Q1Count +
                    _quartileStats.Q2Count +
                    _quartileStats.Q3Count +
                    _quartileStats.Q4Count +
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

        // =====================================================================
        // Opciones de gráficos (ECharts)
        // =====================================================================

        protected object? YearChartOption
        {
            get
            {
                if (_filteredByYear == null || _filteredByYear.Count == 0)
                    return null;

                var years = _filteredByYear.Select(x => x.Year).ToArray();
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
                        data = years,
                        axisLabel = new
                        {
                            interval = 0
                        }
                    },
                    yAxis = new
                    {
                        type = "value"
                    },
                    series = new object[]
                    {
                        new
                        {
                            name = "Artículos",
                            type = "bar",
                            data = counts,
                            itemStyle = new { color = "#7A1E19" }
                        }
                    }
                };
            }
        }

        protected object? FieldChartOption
        {
            get
            {
                if (_topBroadFields == null || _topBroadFields.Count == 0)
                    return null;

                var names = _topBroadFields
                    .Select(x => ShortenLabel(x.Name, 18))
                    .ToArray();

                var data = _topBroadFields.Select(x => x.Count).ToArray();

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
                        data = names,
                        axisLabel = new
                        {
                            rotate = 30,
                            interval = 0
                        }
                    },
                    yAxis = new
                    {
                        type = "value"
                    },
                    series = new object[]
                    {
                        new
                        {
                            name = "Artículos",
                            type = "bar",
                            data = data,
                            itemStyle = new { color = "#9F3B31" }
                        }
                    }
                };
            }
        }

        protected object? ResearchLineChartOption
        {
            get
            {
                if (_topResearchLines == null || _topResearchLines.Count == 0)
                    return null;

                var names = _topResearchLines
                    .Select(x => ShortenLabel(x.Name, 25))
                    .ToArray();

                var data = _topResearchLines.Select(x => x.Count).ToArray();

                return new
                {
                    tooltip = new
                    {
                        trigger = "axis",
                        axisPointer = new { type = "shadow" }
                    },
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
                    xAxis = new
                    {
                        type = "value"
                    },
                    yAxis = new
                    {
                        type = "category",
                        data = names,
                        axisLabel = new
                        {
                            interval = 0
                        }
                    },
                    series = new object[]
                    {
                        new
                        {
                            name = "Artículos",
                            type = "bar",
                            data = data,
                            itemStyle = new { color = "#C25B4A" }
                        }
                    }
                };
            }
        }

        protected object? QuartileChartOption
        {
            get
            {
                if (_quartileStats == null)
                    return null;

                int total =
                    _quartileStats.Q1Count +
                    _quartileStats.Q2Count +
                    _quartileStats.Q3Count +
                    _quartileStats.Q4Count +
                    _quartileStats.NoQuartileCount;

                if (total <= 0)
                    return null;

                var data = new List<Dictionary<string, object>>
                {
                    new() { ["value"] = _quartileStats.Q1Count,         ["name"] = "Q1" },
                    new() { ["value"] = _quartileStats.Q2Count,         ["name"] = "Q2" },
                    new() { ["value"] = _quartileStats.Q3Count,         ["name"] = "Q3" },
                    new() { ["value"] = _quartileStats.Q4Count,         ["name"] = "Q4" },
                    new() { ["value"] = _quartileStats.NoQuartileCount, ["name"] = "Sin cuartil" }
                };

                var tooltip = new Dictionary<string, object>
                {
                    ["trigger"] = "item",
                    ["formatter"] = "{b}: {c} artículos ({d}%)"
                };

                var legend = new Dictionary<string, object>
                {
                    ["orient"] = "vertical",
                    ["left"] = "left"
                };

                var label = new Dictionary<string, object>
                {
                    ["show"] = true,
                    ["formatter"] = "{b}: {d}%"
                };

                var labelLine = new Dictionary<string, object>
                {
                    ["show"] = true
                };

                var seriesItem = new Dictionary<string, object>
                {
                    ["name"] = "Distribución por cuartil",
                    ["type"] = "pie",
                    ["radius"] = new[] { "45%", "70%" },
                    ["avoidLabelOverlap"] = true,
                    ["center"] = new[] { "55%", "55%" },
                    ["data"] = data,
                    ["label"] = label,
                    ["labelLine"] = labelLine
                };

                var series = new List<Dictionary<string, object>> { seriesItem };

                var option = new Dictionary<string, object>
                {
                    ["tooltip"] = tooltip,
                    ["legend"] = legend,
                    ["series"] = series
                };

                return option;
            }
        }
    }
}
