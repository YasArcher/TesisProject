using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using tesisproject.frontend.Features.Management.Components;
using tesisproject.frontend.Services.Implementations;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Features.Management.Pages
{
    public partial class StatisticalReports : ComponentBase, IDisposable
    {
        // =========================================================
        // INYECCIONES
        // =========================================================
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IInstitutionalReportingClient InstitutionalReporting { get; set; } = default!;
        [Inject] private InstitutionalReportingPageStateStore ReportingState { get; set; } = default!;
        // =========================================================
        // ESTADO BASE
        // =========================================================
        protected bool _showInstitutionalFilters = false;
        protected bool _isLoadingInstitutionalReporting = true;
        protected bool _isRefreshingInstitutionalReporting = false;
        protected bool _isRunningInstitutionalEtl = false;
        protected string? _institutionalReportingError;
        protected InstitutionalReportingDashboardDto? _institutionalDashboard;
        protected AuthorReportingDashboardDto? _authorReportingDashboard;
        protected bool _authorDashboardUsingFallback;
        protected InstitutionalReportingFilterDto _institutionalFilter = new();
        protected string _institutionalReportTab = "overview";
        protected bool _isLoadingAuthorReporting = false;
        protected string? _authorReportingError;
        protected DateTime? _institutionalLastLoadedAt;
        protected string? _activeReportPreset;
        protected bool _showPdfComposer = false;
        protected ReportPdfComposerState _pdfComposerState = new();
        private int _pdfComposerRenderKey;
        protected InstitutionalReportingFilterDto _pdfComposerSelection = ReportPdfRequestBuilder.CreateDefaultSelection();
        protected bool _isExportingExcel = false;
        protected bool _isExportingAuthorPdf = false;
        protected InstitutionalReportingFilterDto _authorPdfScopeFilter = new();
        private CancellationTokenSource? _institutionalLoadCts;
        private CancellationTokenSource? _authorLoadCts;

        private static readonly IReadOnlyList<ReportChartCaptureDefinition> PdfChartCaptureDefinitions =
        [
            new("chart-production-year", "Tendencia de producción por año", "Producción científica"),
            new("chart-production-quartiles", "Artículos por cuartil", "Producción científica"),
            new("chart-production-months", "Ritmo mensual de registro", "Producción científica"),
            new("chart-quality-indexing", "Indexación por volumen", "Calidad editorial"),
            new("chart-quality-status", "Estado de publicación", "Calidad editorial"),
            new("chart-quality-venues-quartile", "Revistas por cuartil", "Calidad editorial"),
            new("dashboard-year-trend", "Producción anual", "Dashboard"),
            new("dashboard-open-access-gauge", "Cobertura Open Access", "Dashboard"),
            new("dashboard-open-access-year", "Open Access por año", "Dashboard"),
            new("dashboard-month-heatmap", "Intensidad mensual", "Dashboard"),
            new("dashboard-faculty-treemap", "Facultades", "Dashboard"),
            new("dashboard-researchline-treemap", "Líneas de investigación", "Dashboard"),
            new("dashboard-quartile-donut", "Cuartiles", "Dashboard"),
            new("dashboard-indexing-rose", "Bases de indexación", "Dashboard"),
            new("dashboard-status-donut", "Estado editorial", "Dashboard"),
            new("dashboard-coverage-radar", "Opciones disponibles", "Dashboard"),
            new("dashboard-venue-scatter", "Métricas de revistas", "Dashboard"),
            new("dashboard-field-treemap", "Campos de conocimiento", "Dashboard"),
            new("dashboard-load-workflow", "Carga y flujo", "Dashboard")
        ];

        // =========================================================
        // CICLO DE VIDA
        // =========================================================
        protected override async Task OnInitializedAsync()
        {
            if (TryRestoreReportingState())
            {
                if (_authorDashboardUsingFallback)
                {
                    ClearAuthorFallbackState();
                }

                if (ShouldLoadAuthorDashboard && !_isLoadingAuthorReporting)
                {
                    await LoadAuthorReportingAsync(ReportInstitutionalFilterState.Clone(_institutionalFilter));
                }

                StateHasChanged();
                return;
            }

            StateHasChanged();

            try
            {
                var filterSnapshot = ReportInstitutionalFilterState.Clone(_institutionalFilter);
                await LoadInstitutionalReportingAsync(filterSnapshot: filterSnapshot);
                StateHasChanged();
                await LoadAuthorReportingAsync(filterSnapshot);
            }
            finally
            {
                StateHasChanged();
            }
        }

        private async Task LoadInstitutionalReportingAsync(bool keepCurrentDashboard = false, InstitutionalReportingFilterDto? filterSnapshot = null)
        {
            _institutionalLoadCts?.Cancel();
            _institutionalLoadCts?.Dispose();
            _institutionalLoadCts = new CancellationTokenSource();
            var requestCts = _institutionalLoadCts;
            var requestFilter = filterSnapshot is null
                ? ReportInstitutionalFilterState.Clone(_institutionalFilter)
                : ReportInstitutionalFilterState.Clone(filterSnapshot);

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
                _institutionalDashboard = await InstitutionalReporting.GetDashboardAsync(requestFilter, requestCts.Token);
                _institutionalLastLoadedAt = DateTime.Now;
                SaveReportingState();
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
                SaveReportingState();
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

        private async Task LoadAuthorReportingAsync(InstitutionalReportingFilterDto? filterSnapshot = null, bool allowRetry = true)
        {
            _authorLoadCts?.Cancel();
            _authorLoadCts?.Dispose();
            _authorLoadCts = new CancellationTokenSource();
            var requestCts = _authorLoadCts;
            var requestFilter = filterSnapshot is null
                ? ReportInstitutionalFilterState.Clone(_institutionalFilter)
                : ReportInstitutionalFilterState.Clone(filterSnapshot);

            _isLoadingAuthorReporting = true;
            _authorReportingError = null;

            try
            {
                var dashboard = await InstitutionalReporting.GetAuthorDashboardAsync(requestFilter, requestCts.Token);
                if (!ReferenceEquals(_authorLoadCts, requestCts))
                {
                    return;
                }

                if (dashboard is null)
                {
                    throw new InvalidOperationException("El servicio de autores no devolvió un resultado válido.");
                }

                _authorReportingDashboard = dashboard;
                _authorDashboardUsingFallback = false;

                SaveReportingState();
            }
            catch (OperationCanceledException) when (requestCts.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                if (allowRetry && ReferenceEquals(_authorLoadCts, requestCts))
                {
                    try
                    {
                        _authorLoadCts = null;
                        await LoadAuthorReportingAsync(requestFilter, allowRetry: false);
                        return;
                    }
                    catch
                    {
                        // Si el reintento también falla, dejamos caer al manejo de error de abajo.
                    }
                }

                _authorReportingDashboard = null;
                _authorDashboardUsingFallback = false;
                _authorReportingError = $"No se pudo cargar la analítica de autores: {ex.Message}";
                SaveReportingState();
                Console.Error.WriteLine(ex);
            }
            finally
            {
                if (ReferenceEquals(_authorLoadCts, requestCts))
                {
                    _isLoadingAuthorReporting = false;
                    _authorLoadCts.Dispose();
                    _authorLoadCts = null;
                }
            }
        }

        protected async Task RunEtlAndReload()
        {
            _isRunningInstitutionalEtl = true;
            _institutionalReportingError = null;
            StateHasChanged();

            try
            {
                await InstitutionalReporting.RunFullLoadAsync();
                var filterSnapshot = ReportInstitutionalFilterState.Clone(_institutionalFilter);

                await Task.WhenAll(
                    LoadInstitutionalReportingAsync(filterSnapshot: filterSnapshot),
                    LoadAuthorReportingAsync(filterSnapshot));
                SaveReportingState();

            }
            catch (Exception ex)
            {
                _institutionalReportingError = $"No fue posible actualizar la información: {ex.Message}";
                Console.Error.WriteLine(ex);
            }
            finally
            {
                _isRunningInstitutionalEtl = false;
                StateHasChanged();
            }
        }

        protected string FormatEtlDate(DateTime? value)
            => ReportInstitutionalStatusText.FormatEtlDate(value);

        protected string GetEtlStatusClass(string? status)
            => ReportInstitutionalStatusText.GetEtlStatusClass(status);

        protected static string GetInstitutionalStatusLabel(string? status)
            => ReportInstitutionalStatusText.GetInstitutionalStatusLabel(status);

        protected bool HasInstitutionalReportingData =>
            ReportInstitutionalStatusText.HasReportingData(_institutionalDashboard);

        protected bool IsInstitutionalBlockingBusy =>
            _isLoadingInstitutionalReporting
            || _isRefreshingInstitutionalReporting
            || _isRunningInstitutionalEtl;

        protected string InstitutionalBlockingBusyTitle
            => ReportInstitutionalStatusText.GetBlockingTitle(
                _isRunningInstitutionalEtl,
                _isRefreshingInstitutionalReporting);

        protected string InstitutionalBlockingBusyMessage
            => ReportInstitutionalStatusText.GetBlockingMessage(
                _isRunningInstitutionalEtl,
                _isRefreshingInstitutionalReporting);

        protected int ProjectResultPercent =>
            ReportInstitutionalStatusText.GetProjectResultPercent(_institutionalDashboard);

        protected string InstitutionalLastLoadedLabel =>
            ReportInstitutionalStatusText.GetLastLoadedLabel(_institutionalLastLoadedAt);

        protected string InstitutionalScopeLabel =>
            ReportInstitutionalStatusText.GetScopeLabel(ActiveInstitutionalFilterCount);

        protected string GetActiveFilterSummaryLabel() =>
            ReportInstitutionalStatusText.GetActiveFilterSummaryLabel(ActiveInstitutionalFilterCount);

        protected string InstitutionalDataPulseLabel =>
            ReportInstitutionalStatusText.GetDataPulseLabel(_institutionalDashboard);

        protected string PediIiitScopeLabel =>
            ReportInstitutionalStatusText.GetPediIiitScopeLabel(_institutionalDashboard);

        protected string TddTotalScopeLabel =>
            ReportInstitutionalStatusText.GetTddTotalScopeLabel(_institutionalDashboard);

        protected string ParticipationScopeLabel =>
            ReportInstitutionalStatusText.GetParticipationScopeLabel(_institutionalDashboard);

        protected string InstitutionalPeriodLabel =>
            ReportInstitutionalFilterState.GetPeriodLabel(_institutionalFilter);

        protected IReadOnlyList<(string Label, string Value)> ActiveInstitutionalFilterChips
            => ReportInstitutionalFilterState.BuildActiveChips(_institutionalFilter);

        protected int ActiveInstitutionalFilterCount => ActiveInstitutionalFilterChips.Count;

        protected async Task ApplyInstitutionalFiltersAsync()
        {
            var filterSnapshot = ReportInstitutionalFilterState.Clone(_institutionalFilter);
            var shouldRefreshAuthors = ShouldRefreshAuthorDashboardWithFilters;
            ResetAuthorDashboardForFilterChange();
            await RefreshReportingForFilterAsync(filterSnapshot, shouldRefreshAuthors);

            SaveReportingState();
            StateHasChanged();
        }

        protected async Task ClearInstitutionalFiltersAsync()
        {
            _institutionalFilter = new InstitutionalReportingFilterDto();
            var shouldRefreshAuthors = ShouldRefreshAuthorDashboardWithFilters;
            ResetAuthorDashboardForFilterChange();
            var filterSnapshot = ReportInstitutionalFilterState.Clone(_institutionalFilter);
            await RefreshReportingForFilterAsync(filterSnapshot, shouldRefreshAuthors);

            SaveReportingState();
            StateHasChanged();
        }

        protected void ToggleInstitutionalFilters()
        {
            _showInstitutionalFilters = !_showInstitutionalFilters;
            SaveReportingState();
        }

        protected void OpenPdfComposer()
        {
            if (!CanOpenPdfComposer)
            {
                return;
            }

            _pdfComposerRenderKey++;
            _showPdfComposer = true;
            _pdfComposerState.ResetForOpen();

            if (_pdfComposerSelection is null)
            {
                _pdfComposerSelection = ReportPdfRequestBuilder.CreateDefaultSelection();
            }

            SaveReportingState();
            StateHasChanged();
        }

        protected Task ClosePdfComposer()
        {
            _showPdfComposer = false;
            _pdfComposerState.Close();
            SaveReportingState();
            return Task.CompletedTask;
        }

        protected Task HandlePdfSelectionChanged()
        {
            StateHasChanged();
            return Task.CompletedTask;
        }

        protected bool IsPdfPreviewCurrent
            => _pdfComposerState.IsPreviewCurrent(BuildPdfRequestSignature(BuildPdfRequestFilter()));

        protected bool CanDownloadPdf
            => _pdfComposerState.CanDownload(BuildPdfRequestSignature(BuildPdfRequestFilter()));

        protected bool CanOpenPdfComposer
            => _institutionalDashboard is not null
            && HasInstitutionalReportingData
            && !IsInstitutionalBlockingBusy;

        protected bool CanExportExcel
            => _institutionalDashboard is not null
            && HasInstitutionalReportingData
            && !IsInstitutionalBlockingBusy
            && !_isExportingExcel;

        protected bool CanExportAuthorPdf
            => IsAuthorTabActive
            && _authorReportingDashboard is not null
            && !_authorDashboardUsingFallback
            && !_isLoadingAuthorReporting
            && !IsInstitutionalBlockingBusy
            && !_isExportingAuthorPdf;

        protected async Task GeneratePdfPreviewAsync()
        {
            if (!ReportPdfRequestBuilder.HasAnySectionSelected(_pdfComposerSelection))
            {
                _pdfComposerState.StoreValidationError("Selecciona al menos un bloque antes de generar la vista previa del PDF.");
                StateHasChanged();
                return;
            }

            _pdfComposerState.MarkGenerating();
            StateHasChanged();

            try
            {
                var requestFilter = BuildPdfRequestFilter();
                var bytes = await RequestDashboardPdfAsync(requestFilter);
                _pdfComposerState.StorePreview(bytes, BuildPdfRequestSignature(requestFilter), DateTime.Now);
            }
            catch (Exception ex)
            {
                _pdfComposerState.StoreGenerationError($"No fue posible generar la vista previa del PDF: {ex.Message}");
                Console.Error.WriteLine(ex);
            }
            finally
            {
                _pdfComposerState.FinishGenerating();
                StateHasChanged();
            }
        }

        protected async Task DownloadPdfAsync()
        {
            if (!ReportPdfRequestBuilder.HasAnySectionSelected(_pdfComposerSelection)
                || !IsPdfPreviewCurrent
                || _pdfComposerState.PreviewBytes is null)
            {
                return;
            }

            var base64 = Convert.ToBase64String(_pdfComposerState.PreviewBytes);
            await JS.InvokeVoidAsync(
                "tesisExport.downloadFileFromBase64",
                $"reporte-institucional-{DateTime.Now:yyyyMMddHHmm}.pdf",
                "application/pdf",
                base64);
        }

        protected async Task DownloadExcelAsync()
        {
            if (!CanExportExcel)
            {
                return;
            }

            _isExportingExcel = true;
            _institutionalReportingError = null;
            StateHasChanged();

            try
            {
                var filter = ReportInstitutionalFilterState.Clone(_institutionalFilter);
                var url = InstitutionalReportingClient.BuildDashboardUrl(filter, "api/reporting/dashboard/excel");
                using var response = await Http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var base64 = Convert.ToBase64String(bytes);
                await JS.InvokeVoidAsync(
                    "tesisExport.downloadFileFromBase64",
                    $"reporte-institucional-{DateTime.Now:yyyyMMddHHmm}.xlsx",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    base64);
            }
            catch (Exception ex)
            {
                _institutionalReportingError = $"No fue posible exportar Excel: {ex.Message}";
                Console.Error.WriteLine(ex);
            }
            finally
            {
                _isExportingExcel = false;
                StateHasChanged();
            }
        }

        protected async Task DownloadAuthorPdfAsync()
        {
            if (!CanExportAuthorPdf)
            {
                return;
            }

            _isExportingAuthorPdf = true;
            _authorReportingError = null;
            StateHasChanged();

            try
            {
                var filter = ReportInstitutionalFilterState.Clone(_institutionalFilter);
                ApplyAuthorPdfScope(filter, _authorPdfScopeFilter);
                var url = InstitutionalReportingClient.BuildDashboardUrl(filter, "api/reporting/authors/pdf");
                using var response = await Http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var base64 = Convert.ToBase64String(bytes);
                await JS.InvokeVoidAsync(
                    "tesisExport.downloadFileFromBase64",
                    $"reporte-autores-{DateTime.Now:yyyyMMddHHmm}.pdf",
                    "application/pdf",
                    base64);
            }
            catch (Exception ex)
            {
                _authorReportingError = $"No fue posible generar el PDF de autores: {ex.Message}";
                Console.Error.WriteLine(ex);
            }
            finally
            {
                _isExportingAuthorPdf = false;
                StateHasChanged();
            }
        }

        protected Task HandleAuthorPdfScopeChanged(InstitutionalReportingFilterDto scope)
        {
            _authorPdfScopeFilter = ReportInstitutionalFilterState.Clone(scope);
            return Task.CompletedTask;
        }

        private static void ApplyAuthorPdfScope(InstitutionalReportingFilterDto target, InstitutionalReportingFilterDto scope)
        {
            if (!string.IsNullOrWhiteSpace(scope.AuthorName))
            {
                target.AuthorName = scope.AuthorName;
            }

            if (!string.IsNullOrWhiteSpace(scope.Faculty))
            {
                target.Faculty = scope.Faculty;
            }

            if (scope.OnlyPrimaryAuthors.HasValue)
            {
                target.OnlyPrimaryAuthors = scope.OnlyPrimaryAuthors;
            }
        }

        protected async Task SelectInstitutionalReportTabAsync(string tab)
        {
            _institutionalReportTab = tab?.ToLowerInvariant() switch
            {
                "participation" => "participation",
                "dashboard" => "dashboard",
                "authors" => "authors",
                _ => "overview"
            };

            if (IsAuthorTabActive
                && ShouldLoadAuthorDashboard
                && !_isLoadingAuthorReporting)
            {
                StateHasChanged();
                await LoadAuthorReportingAsync(ReportInstitutionalFilterState.Clone(_institutionalFilter));
            }

            SaveReportingState();
        }

        protected async Task ApplyReportPresetAsync(string preset)
        {
            if (IsInstitutionalBlockingBusy)
            {
                return;
            }

            _activeReportPreset = NormalizeReportPreset(preset);
            _institutionalReportTab = _activeReportPreset switch
            {
                "tdd-management" => "participation",
                _ => "overview"
            };

            _pdfComposerSelection = BuildPresetPdfSelection(_activeReportPreset);
            OpenPdfComposer();

            if (IsAuthorTabActive
                && ShouldLoadAuthorDashboard
                && !_isLoadingAuthorReporting)
            {
                await LoadAuthorReportingAsync(ReportInstitutionalFilterState.Clone(_institutionalFilter));
            }

            SaveReportingState();
            StateHasChanged();
        }

        protected async Task ApplyAuthorFiltersAsync()
        {
            var filterSnapshot = ReportInstitutionalFilterState.Clone(_institutionalFilter);
            await RefreshReportingForFilterAsync(filterSnapshot, refreshAuthors: true);
            SaveReportingState();
            StateHasChanged();
        }

        protected async Task ClearAuthorFiltersAsync()
        {
            var filterSnapshot = ReportInstitutionalFilterState.Clone(_institutionalFilter);
            await RefreshReportingForFilterAsync(filterSnapshot, refreshAuthors: true);
            SaveReportingState();
            StateHasChanged();
        }

        protected string AuthorTabSubtitle
            => _isLoadingAuthorReporting && _authorReportingDashboard is null
                ? "Cargando..."
                : _authorDashboardUsingFallback && _authorReportingDashboard is not null
                    ? $"{_authorReportingDashboard.Kpis.TotalAuthors:N0} autores (resumen)"
                    : !string.IsNullOrWhiteSpace(_authorReportingError)
                        ? "Error de carga"
                            : _authorReportingDashboard is null
                                ? "Pendiente de cargar"
                                : $"{_authorReportingDashboard.Kpis.TotalAuthors:N0} autores";

        private bool IsAuthorTabActive
            => string.Equals(_institutionalReportTab, "authors", StringComparison.OrdinalIgnoreCase);

        private bool ShouldLoadAuthorDashboard
            => _authorReportingDashboard is null || _authorDashboardUsingFallback;

        private bool ShouldRefreshAuthorDashboardWithFilters
            => IsAuthorTabActive || _authorReportingDashboard is not null || _authorDashboardUsingFallback;

        private async Task RefreshReportingForFilterAsync(
            InstitutionalReportingFilterDto filterSnapshot,
            bool refreshAuthors)
        {
            if (refreshAuthors)
            {
                await Task.WhenAll(
                    LoadInstitutionalReportingAsync(keepCurrentDashboard: true, filterSnapshot: filterSnapshot),
                    LoadAuthorReportingAsync(filterSnapshot));
                return;
            }

            await LoadInstitutionalReportingAsync(keepCurrentDashboard: true, filterSnapshot: filterSnapshot);
        }

        private void ResetAuthorDashboardForFilterChange()
        {
            _authorLoadCts?.Cancel();
            _authorReportingDashboard = null;
            _authorDashboardUsingFallback = false;
            _authorReportingError = null;
            _isLoadingAuthorReporting = false;
            ReportingState.ClearAuthorState();
        }

        private void ClearAuthorFallbackState()
        {
            _authorReportingDashboard = null;
            _authorDashboardUsingFallback = false;
            if (string.Equals(_authorReportingError, ReportAuthorFallbackBuilder.AvailabilityMessage, StringComparison.Ordinal)
                || string.Equals(_authorReportingError, ReportAuthorFallbackBuilder.ErrorMessage, StringComparison.Ordinal))
            {
                _authorReportingError = null;
            }

            ReportingState.ClearAuthorState();
        }

        private bool TryRestoreReportingState()
        {
            if (!ReportingState.HasFreshInstitutionalDashboard)
            {
                return false;
            }

            _institutionalDashboard = ReportingState.InstitutionalDashboard;
            _authorReportingDashboard = ReportingState.AuthorDashboard;
            _authorDashboardUsingFallback = ReportingState.AuthorDashboardUsingFallback;
            _institutionalFilter = ReportInstitutionalFilterState.Clone(ReportingState.Filter);
            _institutionalReportTab = string.IsNullOrWhiteSpace(ReportingState.ActiveTab)
                ? "overview"
                : ReportingState.ActiveTab;
            _activeReportPreset = ReportingState.ActivePreset;
            _institutionalReportingError = ReportingState.InstitutionalError;
            _authorReportingError = ReportingState.AuthorError;
            _institutionalLastLoadedAt = ReportingState.LastLoadedAt;
            _isLoadingInstitutionalReporting = false;
            _isRefreshingInstitutionalReporting = false;
            _isLoadingAuthorReporting = false;
            _isRunningInstitutionalEtl = false;

            return true;
        }

        private void SaveReportingState()
        {
            ReportingState.InstitutionalDashboard = _institutionalDashboard;
            ReportingState.AuthorDashboard = _authorReportingDashboard;
            ReportingState.AuthorDashboardUsingFallback = _authorDashboardUsingFallback;
            ReportingState.Filter = ReportInstitutionalFilterState.Clone(_institutionalFilter);
            ReportingState.ActiveTab = _institutionalReportTab;
            ReportingState.ActivePreset = _activeReportPreset;
            ReportingState.InstitutionalError = _institutionalReportingError;
            ReportingState.AuthorError = _authorReportingError;
            ReportingState.LastLoadedAt = _institutionalLastLoadedAt;
        }

        private InstitutionalReportingFilterDto BuildPdfRequestFilter()
        {
            var filter = ReportInstitutionalFilterState.Clone(_institutionalFilter);
            ReportPdfRequestBuilder.ApplySelection(filter, _pdfComposerSelection);
            return filter;
        }

        private async Task<byte[]> RequestDashboardPdfAsync(InstitutionalReportingFilterDto requestFilter, CancellationToken ct = default)
        {
            var charts = requestFilter.IncludePdfCharts
                ? await CapturePdfChartImagesAsync()
                : [];

            var request = new InstitutionalPdfReportRequestDto
            {
                Filter = requestFilter,
                Charts = charts
            };

            using var response = await Http.PostAsJsonAsync(ReportPdfRequestBuilder.DashboardPdfEndpoint, request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                var detail = string.IsNullOrWhiteSpace(error)
                    ? response.ReasonPhrase
                    : error;

                throw new InvalidOperationException($"El backend no pudo generar el PDF ({(int)response.StatusCode}). {detail}");
            }

            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        private async Task<List<ReportChartImageDto>> CapturePdfChartImagesAsync()
        {
            try
            {
                var charts = await JS.InvokeAsync<List<ReportChartImageDto>>(
                    "tesisExport.collectReportChartImages",
                    PdfChartCaptureDefinitions);

                return charts ?? [];
            }
            catch
            {
                return [];
            }
        }

        private static string BuildPdfRequestSignature(InstitutionalReportingFilterDto filter)
            => ReportPdfRequestBuilder.BuildDashboardPdfUrl(filter);

        private static string NormalizeReportPreset(string? preset)
            => preset?.Trim().ToLowerInvariant() switch
            {
                "pedi-quarter" => "pedi-quarter",
                "tdd-management" => "tdd-management",
                "pedi-iiit" => "pedi-iiit",
                "tdd-total" => "tdd-total",
                _ => "pedi-quarter"
            };

        private static InstitutionalReportingFilterDto BuildPresetPdfSelection(string? preset)
        {
            var selection = new InstitutionalReportingFilterDto
            {
                IncludePdfKpis = true,
                IncludePdfFilters = true,
                IncludePdfCharts = true,
                IncludePdfPeriod = true,
                IncludePdfFields = false,
                IncludePdfVenues = false,
                IncludePdfAuthors = false,
                IncludePdfPediIiit = false,
                IncludePdfTddTotal = false,
                IncludePdfParticipation = false,
                IncludePdfArticles = true
            };

            switch (NormalizeReportPreset(preset))
            {
                case "pedi-quarter":
                    selection.IncludePdfFields = true;
                    selection.IncludePdfVenues = true;
                    selection.IncludePdfAuthors = true;
                    selection.IncludePdfPediIiit = true;
                    selection.IncludePdfParticipation = true;
                    break;
                case "tdd-management":
                    selection.IncludePdfVenues = true;
                    selection.IncludePdfAuthors = true;
                    selection.IncludePdfTddTotal = true;
                    selection.IncludePdfParticipation = true;
                    break;
                case "pedi-iiit":
                    selection.IncludePdfPediIiit = true;
                    break;
                case "tdd-total":
                    selection.IncludePdfTddTotal = true;
                    selection.IncludePdfParticipation = true;
                    break;
            }

            return selection;
        }

        public void Dispose()
        {
            _institutionalLoadCts?.Cancel();
            _institutionalLoadCts?.Dispose();
            _institutionalLoadCts = null;

            _authorLoadCts?.Cancel();
            _authorLoadCts?.Dispose();
            _authorLoadCts = null;
        }

        private sealed record ReportChartCaptureDefinition(string ChartId, string Title, string Section);
    }
}
