using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        protected bool _showPdfComposer = false;
        protected bool _isPdfPreviewGenerating = false;
        protected string? _pdfPreviewError;
        protected string? _pdfPreviewBase64;
        protected string _pdfPreviewGeneratedAtLabel = string.Empty;
        private byte[]? _pdfPreviewBytes;
        private string? _pdfPreviewRequestSignature;
        private int _pdfComposerRenderKey;
        protected InstitutionalReportingFilterDto _pdfComposerSelection = CreateDefaultPdfSelection();
        private CancellationTokenSource? _institutionalLoadCts;
        private CancellationTokenSource? _authorLoadCts;

        // =========================================================
        // CICLO DE VIDA
        // =========================================================
        protected override async Task OnInitializedAsync()
        {
            StateHasChanged();

            try
            {
                var filterSnapshot = CloneInstitutionalFilter(_institutionalFilter);
                await Task.WhenAll(
                    LoadInstitutionalReportingAsync(filterSnapshot: filterSnapshot),
                    LoadAuthorReportingAsync(filterSnapshot));

                if (_authorReportingDashboard is null && string.IsNullOrWhiteSpace(_authorReportingError))
                {
                    await LoadAuthorReportingAsync(filterSnapshot, allowRetry: false);
                }
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
                ? CloneInstitutionalFilter(_institutionalFilter)
                : CloneInstitutionalFilter(filterSnapshot);

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
                EnsureAuthorDashboardFallbackAvailability();
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

        private async Task LoadAuthorReportingAsync(InstitutionalReportingFilterDto? filterSnapshot = null, bool allowRetry = true)
        {
            _authorLoadCts?.Cancel();
            _authorLoadCts?.Dispose();
            _authorLoadCts = new CancellationTokenSource();
            var requestCts = _authorLoadCts;
            var requestFilter = filterSnapshot is null
                ? CloneInstitutionalFilter(_institutionalFilter)
                : CloneInstitutionalFilter(filterSnapshot);

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

                if (ShouldUseAuthorFallback(dashboard))
                {
                    _authorReportingDashboard = BuildAuthorDashboardFallback();
                    _authorDashboardUsingFallback = _authorReportingDashboard is not null;
                }
                else
                {
                    _authorReportingDashboard = dashboard;
                    _authorDashboardUsingFallback = false;
                }
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

                var fallbackDashboard = BuildAuthorDashboardFallback();
                if (fallbackDashboard is not null)
                {
                    _authorReportingDashboard = fallbackDashboard;
                    _authorDashboardUsingFallback = true;
                    _authorReportingError = "La analítica detallada de autores no respondió como esperaba. Se muestra un resumen autoral derivado del tablero institucional.";
                }
                else
                {
                    _authorReportingDashboard = null;
                    _authorDashboardUsingFallback = false;
                    _authorReportingError = $"No se pudo cargar la analítica de autores: {ex.Message}";
                }
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
                var filterSnapshot = CloneInstitutionalFilter(_institutionalFilter);

                await Task.WhenAll(
                    LoadInstitutionalReportingAsync(filterSnapshot: filterSnapshot),
                    LoadAuthorReportingAsync(filterSnapshot));

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
            (_institutionalDashboard?.ScientificProduction.TotalArticles ?? 0) > 0
            || (_institutionalDashboard?.ParticipationSummary.TotalArticles ?? 0) > 0
            || (_institutionalDashboard?.RecentArticles.Count ?? 0) > 0
            || (_institutionalDashboard?.PediIiitArticles.Count ?? 0) > 0
            || (_institutionalDashboard?.TddTotalArticles.Count ?? 0) > 0;

        protected bool IsInstitutionalBlockingBusy =>
            _isLoadingInstitutionalReporting
            || _isRefreshingInstitutionalReporting
            || _isRunningInstitutionalEtl
            || _isLoadingAuthorReporting;

        protected string InstitutionalBlockingBusyTitle
        {
            get
            {
                if (_isRunningInstitutionalEtl)
                {
                    return "Actualizando modelo analítico";
                }

                return _isRefreshingInstitutionalReporting
                    ? "Aplicando filtros de reportería"
                    : "Cargando reportería institucional";
            }
        }

        protected string InstitutionalBlockingBusyMessage
        {
            get
            {
                if (_isRunningInstitutionalEtl)
                {
                    return "Estamos ejecutando el ETL y sincronizando los indicadores del DW. Mantén esta pantalla abierta.";
                }

                return _isRefreshingInstitutionalReporting
                    ? "Se están recalculando los bloques, tablas y porcentajes con los filtros seleccionados."
                    : "Estamos preparando los indicadores institucionales, filtros y bloques analíticos.";
            }
        }

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

        protected string GetActiveFilterSummaryLabel() =>
            ActiveInstitutionalFilterChips.Count switch
            {
                0 => "Sin segmentación adicional",
                1 => "1 criterio aplicado",
                _ => $"{ActiveInstitutionalFilterChips.Count} criterios aplicados"
            };

        protected string InstitutionalDataPulseLabel =>
            $"{(_institutionalDashboard?.ScientificProduction.TotalArticles ?? 0):N0} artículos · {(_institutionalDashboard?.ParticipationSummary.TotalIndexingLinks ?? 0):N0} indexaciones · {(_institutionalDashboard?.ParticipationSummary.ByFaculty.Count ?? 0):N0} facultades";

        protected string PediIiitScopeLabel =>
            $"{(_institutionalDashboard?.PediIiitArticles.Count ?? 0):N0} registros disponibles · mostrando hasta 20";

        protected string TddTotalScopeLabel =>
            $"{(_institutionalDashboard?.TddTotalArticles.Count ?? 0):N0} registros disponibles · mostrando hasta 24";

        protected string ParticipationScopeLabel =>
            $"{(_institutionalDashboard?.ParticipationSummary.TotalArticles ?? 0):N0} artículos · {(_institutionalDashboard?.ParticipationSummary.TotalIndexingLinks ?? 0):N0} vínculos";

        protected string InstitutionalPeriodLabel =>
            string.Equals(_institutionalFilter.PeriodDateType, "published", StringComparison.OrdinalIgnoreCase)
                ? "Fecha de publicación"
                : "Fecha de registro";

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
                AddChip(chips, "Facultad", _institutionalFilter.Faculty);
                AddChip(chips, "Base de datos", _institutionalFilter.IndexingSource);
                AddChip(chips, "Campo amplio", _institutionalFilter.BroadField);
                AddChip(chips, "Campo específico", _institutionalFilter.SpecificField);
                AddChip(chips, "Campo detallado", _institutionalFilter.DetailedField);
                AddChip(chips, "Revista", _institutionalFilter.VenueName);
                AddChip(chips, "Tipo de publicación", _institutionalFilter.VenueType);
                AddChip(chips, "Cuartil", _institutionalFilter.Quartile);
                AddChip(chips, "Autor", _institutionalFilter.AuthorName);
                AddChip(chips, "Coautor", _institutionalFilter.CoauthorName);
                AddChip(chips, "Filiación autor", _institutionalFilter.AuthorAffiliation);
                AddChip(chips, "Tipo participante", _institutionalFilter.ParticipantType);
                if (_institutionalFilter.HasOrcid.HasValue)
                {
                    chips.Add(("ORCID", _institutionalFilter.HasOrcid.Value ? "Con ORCID" : "Sin ORCID"));
                }

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

                if (_institutionalFilter.IsProjectResult.HasValue)
                {
                    chips.Add(("Proyecto", _institutionalFilter.IsProjectResult.Value ? "Sí" : "No"));
                }

                if (_institutionalFilter.HasInterculturalComponent.HasValue)
                {
                    chips.Add(("Interculturalidad", _institutionalFilter.HasInterculturalComponent.Value ? "Sí" : "No"));
                }

                if (_institutionalFilter.OnlyPrimaryAuthors == true)
                {
                    chips.Add(("Autoría", "Solo autor principal"));
                }

                return chips;
            }
        }

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
            var filterSnapshot = CloneInstitutionalFilter(_institutionalFilter);
            await Task.WhenAll(
                LoadInstitutionalReportingAsync(keepCurrentDashboard: true, filterSnapshot: filterSnapshot),
                LoadAuthorReportingAsync(filterSnapshot));
            StateHasChanged();
        }

        protected async Task ClearInstitutionalFiltersAsync()
        {
            _institutionalFilter = new InstitutionalReportingFilterDto();
            _authorReportingDashboard = null;
            _authorDashboardUsingFallback = false;
            var filterSnapshot = CloneInstitutionalFilter(_institutionalFilter);
            await Task.WhenAll(
                LoadInstitutionalReportingAsync(keepCurrentDashboard: true, filterSnapshot: filterSnapshot),
                LoadAuthorReportingAsync(filterSnapshot));
            StateHasChanged();
        }

        protected void ToggleInstitutionalFilters()
        {
            _showInstitutionalFilters = !_showInstitutionalFilters;
        }

        protected void OpenPdfComposer()
        {
            _pdfComposerRenderKey++;
            _showPdfComposer = true;
            _isPdfPreviewGenerating = false;
            _pdfPreviewError = null;
            _pdfPreviewBase64 = null;
            _pdfPreviewGeneratedAtLabel = string.Empty;
            _pdfPreviewBytes = null;
            _pdfPreviewRequestSignature = null;

            if (_pdfComposerSelection is null)
            {
                _pdfComposerSelection = CreateDefaultPdfSelection();
            }

            StateHasChanged();
        }

        protected Task ClosePdfComposer()
        {
            _showPdfComposer = false;
            _isPdfPreviewGenerating = false;
            return Task.CompletedTask;
        }

        protected Task HandlePdfSelectionChanged()
        {
            StateHasChanged();
            return Task.CompletedTask;
        }

        protected bool IsPdfPreviewCurrent
            => _pdfPreviewBytes is not null
            && string.Equals(_pdfPreviewRequestSignature, BuildPdfRequestSignature(BuildPdfRequestFilter()), StringComparison.Ordinal);

        protected bool CanDownloadPdf
            => !_isPdfPreviewGenerating && IsPdfPreviewCurrent;

        protected async Task GeneratePdfPreviewAsync()
        {
            if (!HasAnyPdfSectionSelected(_pdfComposerSelection))
            {
                _pdfPreviewBytes = null;
                _pdfPreviewBase64 = null;
                _pdfPreviewRequestSignature = null;
                _pdfPreviewGeneratedAtLabel = string.Empty;
                _pdfPreviewError = "Selecciona al menos un bloque antes de generar la vista previa del PDF.";
                StateHasChanged();
                return;
            }

            _isPdfPreviewGenerating = true;
            _pdfPreviewError = null;
            StateHasChanged();

            try
            {
                var requestFilter = BuildPdfRequestFilter();
                var bytes = await RequestDashboardPdfAsync(requestFilter);
                _pdfPreviewBytes = bytes;
                _pdfPreviewBase64 = Convert.ToBase64String(bytes);
                _pdfPreviewRequestSignature = BuildPdfRequestSignature(requestFilter);
                _pdfPreviewGeneratedAtLabel = $"Vista previa generada: {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
            catch (Exception ex)
            {
                _pdfPreviewBytes = null;
                _pdfPreviewBase64 = null;
                _pdfPreviewRequestSignature = null;
                _pdfPreviewGeneratedAtLabel = string.Empty;
                _pdfPreviewError = $"No fue posible generar la vista previa del PDF: {ex.Message}";
                Console.Error.WriteLine(ex);
            }
            finally
            {
                _isPdfPreviewGenerating = false;
                StateHasChanged();
            }
        }

        protected async Task DownloadPdfAsync()
        {
            if (!HasAnyPdfSectionSelected(_pdfComposerSelection) || !IsPdfPreviewCurrent || _pdfPreviewBytes is null)
            {
                return;
            }

            var base64 = Convert.ToBase64String(_pdfPreviewBytes);
            await JS.InvokeVoidAsync(
                "tesisExport.downloadFileFromBase64",
                $"reporte-institucional-{DateTime.Now:yyyyMMddHHmm}.pdf",
                "application/pdf",
                base64);
        }

        protected void ShowInstitutionalOverview()
        {
            _institutionalReportTab = "overview";
        }

        protected void ShowInstitutionalParticipation()
        {
            _institutionalReportTab = "participation";
        }

        protected async Task ShowInstitutionalAuthors()
        {
            _institutionalReportTab = "authors";
            if (_authorReportingDashboard is null && !_isLoadingAuthorReporting)
            {
                await LoadAuthorReportingAsync(CloneInstitutionalFilter(_institutionalFilter));
            }
        }

        protected async Task ApplyAuthorFiltersAsync()
        {
            var filterSnapshot = CloneInstitutionalFilter(_institutionalFilter);
            await LoadInstitutionalReportingAsync(keepCurrentDashboard: true, filterSnapshot: filterSnapshot);
            await LoadAuthorReportingAsync(filterSnapshot);
            StateHasChanged();
        }

        protected async Task ClearAuthorFiltersAsync()
        {
            var filterSnapshot = CloneInstitutionalFilter(_institutionalFilter);
            await LoadInstitutionalReportingAsync(keepCurrentDashboard: true, filterSnapshot: filterSnapshot);
            await LoadAuthorReportingAsync(filterSnapshot);
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

        private void EnsureAuthorDashboardFallbackAvailability()
        {
            if (_authorReportingDashboard is not null || _isLoadingAuthorReporting)
            {
                return;
            }

            var fallbackDashboard = BuildAuthorDashboardFallback();
            if (fallbackDashboard is null)
            {
                return;
            }

            _authorReportingDashboard = fallbackDashboard;
            _authorDashboardUsingFallback = true;

            if (string.IsNullOrWhiteSpace(_authorReportingError))
            {
                _authorReportingError = "Se muestra un resumen autoral derivado del tablero institucional mientras se completa la analítica detallada.";
            }
        }

        private bool ShouldUseAuthorFallback(AuthorReportingDashboardDto dashboard)
        {
            return dashboard.Kpis.TotalAuthors == 0
                && (_institutionalDashboard?.ArticlesByAuthor.Count ?? 0) > 0;
        }

        private AuthorReportingDashboardDto? BuildAuthorDashboardFallback()
        {
            if (_institutionalDashboard is null)
            {
                return null;
            }

            var authorItems = _institutionalDashboard.ArticlesByAuthor
                .Where(x => x.TotalArticles > 0)
                .ToList();

            if (authorItems.Count == 0)
            {
                return null;
            }

            var tracedArticles = _institutionalDashboard.AuthorTraceCoverage.ArticlesWithAuthorTrace;
            var totalArticles = _institutionalDashboard.ScientificProduction.TotalArticles;

            return new AuthorReportingDashboardDto
            {
                Kpis = new AuthorReportingKpiDto
                {
                    TotalAuthors = authorItems.Count,
                    TotalArticles = totalArticles,
                    ArticlesWithAuthorTrace = tracedArticles,
                    ArticlesWithoutAuthorTrace = _institutionalDashboard.AuthorTraceCoverage.ArticlesWithoutAuthorTrace,
                    TotalAuthorArticleLinks = authorItems.Sum(x => x.TotalArticles),
                    PrimaryAuthorLinks = 0,
                    CoauthorLinks = 0,
                    AuthorsWithOrcid = 0,
                    AuthorsWithAffiliation = 0,
                    AverageAuthorsPerArticle = tracedArticles <= 0
                        ? 0
                        : Math.Round(authorItems.Sum(x => x.TotalArticles) / (decimal)tracedArticles, 2)
                },
                FilterOptions = new AuthorReportingFilterOptionsDto
                {
                    Authors = authorItems.Select(x => x.Name).ToList()
                },
                Authors = authorItems
                    .Select((x, index) => new AuthorReportingSummaryDto
                    {
                        AuthorKey = index + 1,
                        AuthorName = x.Name,
                        Affiliation = "Sin detalle",
                        ParticipantType = "Sin detalle",
                        TotalArticles = x.TotalArticles,
                        PrimaryAuthorArticles = 0,
                        CoauthorArticles = 0
                    })
                    .ToList(),
                Publications = new List<AuthorPublicationDto>(),
                Coauthors = new List<AuthorCoauthorDto>(),
                ArticlesByAffiliation = new List<ReportingSummaryItemDto>(),
                ArticlesByFaculty = _institutionalDashboard.ArticlesByFaculty
                    .Select(x => new ReportingSummaryItemDto { Name = x.Name, TotalArticles = x.TotalArticles })
                    .ToList(),
                ArticlesByIndexingSource = _institutionalDashboard.ArticlesByIndexingSource
                    .Select(x => new ReportingSummaryItemDto { Name = x.IndexingSourceName, TotalArticles = x.TotalArticles })
                    .ToList(),
                ArticlesByQuartile = _institutionalDashboard.ArticlesByQuartile
                    .Select(x => new ReportingSummaryItemDto { Name = x.Name, TotalArticles = x.TotalArticles })
                    .ToList(),
                ArticlesByMonth = _institutionalDashboard.ArticlesByMonth
                    .Select(x => new ReportingSummaryItemDto { Name = x.Name, TotalArticles = x.TotalArticles })
                    .ToList()
            };
        }

        private InstitutionalReportingFilterDto BuildPdfRequestFilter()
        {
            var filter = CloneInstitutionalFilter(_institutionalFilter);
            filter.IncludePdfKpis = _pdfComposerSelection.IncludePdfKpis;
            filter.IncludePdfFilters = _pdfComposerSelection.IncludePdfFilters;
            filter.IncludePdfPeriod = _pdfComposerSelection.IncludePdfPeriod;
            filter.IncludePdfFields = _pdfComposerSelection.IncludePdfFields;
            filter.IncludePdfVenues = _pdfComposerSelection.IncludePdfVenues;
            filter.IncludePdfAuthors = _pdfComposerSelection.IncludePdfAuthors;
            filter.IncludePdfPediIiit = _pdfComposerSelection.IncludePdfPediIiit;
            filter.IncludePdfTddTotal = _pdfComposerSelection.IncludePdfTddTotal;
            filter.IncludePdfParticipation = _pdfComposerSelection.IncludePdfParticipation;
            filter.IncludePdfArticles = _pdfComposerSelection.IncludePdfArticles;
            return filter;
        }

        private async Task<byte[]> RequestDashboardPdfAsync(InstitutionalReportingFilterDto requestFilter, CancellationToken ct = default)
        {
            var url = InstitutionalReportingClient.BuildDashboardUrl(
                requestFilter,
                "api/reporting/dashboard/pdf",
                includePdfOptions: true);

            using var response = await Http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        private static InstitutionalReportingFilterDto CreateDefaultPdfSelection()
            => new();

        private static bool HasAnyPdfSectionSelected(InstitutionalReportingFilterDto selection)
            => selection.IncludePdfKpis
            || selection.IncludePdfFilters
            || selection.IncludePdfPeriod
            || selection.IncludePdfFields
            || selection.IncludePdfVenues
            || selection.IncludePdfAuthors
            || selection.IncludePdfPediIiit
            || selection.IncludePdfTddTotal
            || selection.IncludePdfParticipation
            || selection.IncludePdfArticles;

        private static InstitutionalReportingFilterDto CloneInstitutionalFilter(InstitutionalReportingFilterDto source)
            => new()
            {
                CreatedFrom = source.CreatedFrom,
                CreatedTo = source.CreatedTo,
                PublishedFrom = source.PublishedFrom,
                PublishedTo = source.PublishedTo,
                AcademicTerm = source.AcademicTerm,
                PublicationStatus = source.PublicationStatus,
                ResearchLine = source.ResearchLine,
                Faculty = source.Faculty,
                IndexingSource = source.IndexingSource,
                BroadField = source.BroadField,
                SpecificField = source.SpecificField,
                DetailedField = source.DetailedField,
                VenueName = source.VenueName,
                VenueType = source.VenueType,
                ArticleYear = source.ArticleYear,
                Quartile = source.Quartile,
                IsOpenAccess = source.IsOpenAccess,
                IsProjectResult = source.IsProjectResult,
                HasInterculturalComponent = source.HasInterculturalComponent,
                PeriodDateType = source.PeriodDateType,
                AuthorName = source.AuthorName,
                AuthorAffiliation = source.AuthorAffiliation,
                ParticipantType = source.ParticipantType,
                HasOrcid = source.HasOrcid,
                OnlyPrimaryAuthors = source.OnlyPrimaryAuthors,
                CoauthorName = source.CoauthorName,
                IncludePdfKpis = source.IncludePdfKpis,
                IncludePdfFilters = source.IncludePdfFilters,
                IncludePdfPeriod = source.IncludePdfPeriod,
                IncludePdfFields = source.IncludePdfFields,
                IncludePdfVenues = source.IncludePdfVenues,
                IncludePdfAuthors = source.IncludePdfAuthors,
                IncludePdfPediIiit = source.IncludePdfPediIiit,
                IncludePdfTddTotal = source.IncludePdfTddTotal,
                IncludePdfParticipation = source.IncludePdfParticipation,
                IncludePdfArticles = source.IncludePdfArticles
            };

        private static string BuildPdfRequestSignature(InstitutionalReportingFilterDto filter)
            => InstitutionalReportingClient.BuildDashboardUrl(
                filter,
                "api/reporting/dashboard/pdf",
                includePdfOptions: true);

        public void Dispose()
        {
            _institutionalLoadCts?.Cancel();
            _institutionalLoadCts?.Dispose();
            _institutionalLoadCts = null;

            _authorLoadCts?.Cancel();
            _authorLoadCts?.Dispose();
            _authorLoadCts = null;
        }

    }
}
