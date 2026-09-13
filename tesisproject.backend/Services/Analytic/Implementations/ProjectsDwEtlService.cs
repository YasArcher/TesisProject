using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Analytic.Contracts;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Contracts.Administration;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Entities.Analytics.Dw.Bridges;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Analytics.Dw.Facts;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Analytic.Implementations
{
    /// <summary>
    /// Default implementation of IProjectsDwEtlService.
    /// Reads Projects operational data through the Unified query source and writes into ProjectsDwContext.
    /// </summary>
    public class ProjectsDwEtlService : IProjectsDwEtlService
    {
        private readonly IUnifiedProjectsDwSource _projectsSource;
        private readonly ProjectsDwContext _dw;
        private readonly ILogger<ProjectsDwEtlService> _logger;
        private readonly IExternalPeriodsClient _externalPeriods;
        private readonly IOperationExecutionHistoryService _history;
        private readonly IUnifiedArticleUserContext _userContext;
        private WarningCollector? _activeWarnings;

        public ProjectsDwEtlService(
            IUnifiedProjectsDwSource projectsSource,
            ProjectsDwContext dw,
            ILogger<ProjectsDwEtlService> logger,
            IExternalPeriodsClient externalPeriods,
            IOperationExecutionHistoryService history,
            IUnifiedArticleUserContext userContext)
        {
            _projectsSource = projectsSource;
            _dw = dw;
            _logger = logger;
            _externalPeriods = externalPeriods;
            _history = history;
            _userContext = userContext;
        }

        // =========================================================
        // Public API
        // =========================================================

        public async Task<ServiceResult<NoContent>> RunFullLoadAsync(CancellationToken ct = default)
        {
            var execution = await _history.StartAsync(new(
                OperationExecutionTypes.Etl,
                OperationCodes.ProjectsDwFullLoad,
                ExecutedByAppUserId: await _userContext.GetAppUserIdAsync(ct),
                ExecutedByName: _userContext.DisplayName,
                Source: _userContext.IsAuthenticated ? "HTTP" : "System"), ct);
            var stopwatch = Stopwatch.StartNew();
            _activeWarnings = new WarningCollector();
            try
            {
                _logger.LogInformation("Projects DW ETL - Full load started");

                await ClearDwAsync(ct);
                await LoadDimensionsAsync(ct);
                await LoadFactsAsync(ct);
                await LoadBridgesAsync(ct);

                _logger.LogInformation("Projects DW ETL - Full load finished");
                stopwatch.Stop();
                var summary = await BuildHistoryResultAsync(stopwatch.ElapsedMilliseconds, ct);
                await _history.CompleteSuccessAsync(execution.ExecutionId, summary, ct);
                return ServiceResult<NoContent>.Ok(new NoContent());
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                await RecordFailureAsync(execution.ExecutionId, "DW_ETL_CANCELED",
                    "The DW full load was canceled.", stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                await RecordFailureAsync(execution.ExecutionId, "DW_ETL_FULL_LOAD_FAILED",
                    "The DW full load failed.", stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                _activeWarnings = null;
            }
        }

        private async Task<DwFullLoadHistoryResult> BuildHistoryResultAsync(long durationMilliseconds, CancellationToken ct)
            => new(durationMilliseconds, new(
                await _dw.DimDates.CountAsync(ct),
                await _dw.DimProjectStates.CountAsync(ct),
                await _dw.DimFundingTypes.CountAsync(ct),
                await _dw.DimProductTypes.CountAsync(ct),
                await _dw.DimFaculties.CountAsync(ct),
                await _dw.DimResearchCategories.CountAsync(ct),
                await _dw.DimIndexingDatabases.CountAsync(ct),
                await _dw.DimQuartiles.CountAsync(ct),
                await _dw.DimAuthors.CountAsync(ct),
                await _dw.DimJournals.CountAsync(ct),
                await _dw.FactProjects.CountAsync(ct),
                await _dw.FactBudgets.CountAsync(ct),
                await _dw.FactProducts.CountAsync(ct),
                await _dw.BridgeProjectResearchCategories.CountAsync(ct),
                await _dw.BridgeProductAuthors.CountAsync(ct)),
                _activeWarnings?.Snapshot() ?? []);

        private async Task RecordFailureAsync(Guid executionId, string errorCode, string errorMessage,
            long durationMilliseconds)
        {
            try
            {
                var summary = new DwFullLoadHistoryResult(durationMilliseconds,
                    null,
                    _activeWarnings?.Snapshot() ?? []);
                await _history.CompleteFailureAsync(executionId, errorCode, errorMessage,
                    summary, CancellationToken.None);
            }
            catch (Exception historyException)
            {
                _logger.LogError(historyException,
                    "Projects DW ETL - Failed to persist FAILED status for execution {ExecutionId}.", executionId);
            }
        }

        private void RecordWarning(string code) => _activeWarnings?.Add(code);

        // =========================================================
        // CLEAR DW
        // =========================================================

        private async Task ClearDwAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Clearing DW tables (bridges, facts, dimensions)...");

            // 1) Bridges (dependen de dimensiones y facts)
            _dw.BridgeProductAuthors.RemoveRange(_dw.BridgeProductAuthors);
            _dw.BridgeProjectResearchCategories.RemoveRange(_dw.BridgeProjectResearchCategories);

            // 2) Facts (dependen de dimensiones)
            _dw.FactProducts.RemoveRange(_dw.FactProducts);
            _dw.FactBudgets.RemoveRange(_dw.FactBudgets);
            _dw.FactProjects.RemoveRange(_dw.FactProjects);

            // 3) Dimensions (base del modelo)
            _dw.DimAuthors.RemoveRange(_dw.DimAuthors);
            _dw.DimJournals.RemoveRange(_dw.DimJournals);
            _dw.DimResearchCategories.RemoveRange(_dw.DimResearchCategories);
            _dw.DimQuartiles.RemoveRange(_dw.DimQuartiles);
            _dw.DimIndexingDatabases.RemoveRange(_dw.DimIndexingDatabases);
            _dw.DimProductTypes.RemoveRange(_dw.DimProductTypes);
            _dw.DimFundingTypes.RemoveRange(_dw.DimFundingTypes);
            _dw.DimProjectStates.RemoveRange(_dw.DimProjectStates);
            _dw.DimFaculties.RemoveRange(_dw.DimFaculties);
            _dw.DimDates.RemoveRange(_dw.DimDates);

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation("Projects DW ETL - DW tables cleared.");
        }

        // =========================================================
        // LOAD DIMENSIONS / FACTS / BRIDGES
        // =========================================================

        public async Task<ServiceResult<NoContent>> LoadDimensionsAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Projects DW ETL - Loading dimensions...");

            await LoadDimDateAsync(ct);
            await LoadDimProjectStateAsync(ct);
            await LoadDimFundingTypeAsync(ct);
            await LoadDimProductTypeAsync(ct);
            await LoadDimFacultyAsync(ct);
            await LoadDimResearchCategoryAsync(ct);
            await LoadDimIndexingDatabaseAsync(ct);
            await LoadDimQuartileAsync(ct);
            await LoadDimJournalAsync(ct);
            await LoadDimAuthorAsync(ct);

            _logger.LogInformation("Projects DW ETL - Dimensions loaded");

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        public async Task<ServiceResult<NoContent>> LoadBridgesAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Projects DW ETL - Loading bridge tables...");

            await LoadBridgeProjectResearchCategoryAsync(ct);
            await LoadBridgeProductAuthorAsync(ct);

            _logger.LogInformation("Projects DW ETL - Bridge tables loaded");

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        public async Task<ServiceResult<NoContent>> LoadFactsAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Projects DW ETL - Loading fact tables...");

            await LoadFactProjectAsync(ct);
            await LoadFactBudgetAsync(ct);
            await LoadFactProductAsync(ct);

            _logger.LogInformation("Projects DW ETL - Fact tables loaded");

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        // =========================================================
        // DIMENSIONS
        // =========================================================

        private async Task LoadDimDateAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading DimDate...");

            var bounds = await _projectsSource.GetDateBoundsAsync(ct);

            // =============================
            // Combinar todos los mínimos y máximos
            // =============================

            var minDate = new[]
                {
                    bounds.MinProjectStartDate,
                    bounds.MinProjectEndDate,
                    bounds.MinProjectApprovalDate,
                    bounds.MinBudgetApprovedDate,
                    bounds.MinProductCreatedDate
                }
                .Where(d => d.HasValue)
                .Select(d => d!.Value.Date)
                .DefaultIfEmpty(DateTime.Today.Date)
                .Min();

            var maxDate = new[]
                {
                    bounds.MaxProjectStartDate,
                    bounds.MaxProjectEndDate,
                    bounds.MaxProjectApprovalDate,
                    bounds.MaxBudgetApprovedDate,
                    bounds.MaxProductCreatedDate
                }
                .Where(d => d.HasValue)
                .Select(d => d!.Value.Date)
                .DefaultIfEmpty(DateTime.Today.Date)
                .Max();

            if (minDate > maxDate)
            {
                minDate = maxDate = DateTime.Today.Date;
            }

            // === Periodos académicos ===
            var periodsResult = await _externalPeriods.GetAllAsync(ct);

            if (!periodsResult.Success || periodsResult.Data is null || periodsResult.Data.Count == 0)
            {
                RecordWarning("ACADEMIC_PERIODS_UNAVAILABLE");
                _logger.LogWarning("Projects DW ETL - Cannot retrieve external academic periods. PeriodName will be NULL.");
            }

            var academicPeriods = (periodsResult.Data ?? Array.Empty<ExternalAcademicPeriodModel>())
                .OrderBy(p => p.StartDate)
                .ToList();


            var current = minDate;
            var periodIndex = 0;

            while (current <= maxDate)
            {
                var dateKey = current.Year * 10000 + current.Month * 100 + current.Day;

                string? periodName = null;

                if (academicPeriods.Count > 0)
                {
                    // avanzamos el puntero mientras la fecha ya se pasó del EndDate
                    while (periodIndex < academicPeriods.Count &&
                           current.Date > academicPeriods[periodIndex].EndDate.Date)
                    {
                        periodIndex++;
                    }

                    // si el puntero está dentro de un periodo válido y la fecha cae en rango, usamos ese
                    if (periodIndex < academicPeriods.Count)
                    {
                        var p = academicPeriods[periodIndex];

                        if (p.StartDate.Date <= current.Date && current.Date <= p.EndDate.Date)
                            periodName = p.Name;
                    }
                }


                var dim = new DimDate
                {
                    DateKey = dateKey,
                    Date = current,
                    Year = current.Year,
                    Month = current.Month,
                    Day = current.Day,
                    PeriodName = periodName
                };

                _dw.DimDates.Add(dim);
                current = current.AddDays(1);
            }

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Projects DW ETL - DimDate loaded ({Count} rows)",
                await _dw.DimDates.CountAsync(ct));
        }

        private async Task LoadDimProjectStateAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading DimProjectState...");

            var states = await _projectsSource.ListProjectStatesAsync(ct);

            foreach (var s in states)
            {
                var dim = new DimProjectState
                {
                    ProjectStateId = s.Id,
                    Name = s.Name,
                    IsActive = s.IsActive
                };

                _dw.DimProjectStates.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimFundingTypeAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading DimFundingType...");

            var fundingTypes = await _projectsSource.ListFundingTypesAsync(ct);

            foreach (var ft in fundingTypes)
            {
                var dim = new DimFundingType
                {
                    FundingTypeId = ft.Id,
                    Name = ft.Name,
                    IsActive = ft.IsActive
                };

                _dw.DimFundingTypes.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimProductTypeAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading DimProductType...");

            var productTypes = await _projectsSource.ListProductTypesAsync(ct);

            foreach (var pt in productTypes)
            {
                var dim = new DimProductType
                {
                    ProductTypeId = pt.Id,
                    Name = pt.Name,
                    IsActive = pt.IsActive
                };

                _dw.DimProductTypes.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimFacultyAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading DimFaculty...");

            // Project.FacultyId references the local Unified Faculty key.
            var facultiesInUse = await _projectsSource.ListUsedFacultiesAsync(ct);

            if (facultiesInUse.Count == 0)
            {
                _logger.LogInformation(
                    "Projects DW ETL - No FacultyId found in Projects; DimFaculty remains empty.");
                return;
            }

            foreach (var faculty in facultiesInUse)
            {
                if (string.IsNullOrWhiteSpace(faculty.Name))
                {
                    RecordWarning("FACULTY_NAME_MISSING");
                    _logger.LogWarning(
                        "Projects DW ETL - Local FacultyId {FacultyId} was not found. Using placeholder name.",
                        faculty.FacultyId);
                }

                var dim = new DimFaculty
                {
                    FacultyId = faculty.FacultyId,
                    FacultyCode = faculty.Acronym,
                    FacultyName = faculty.Name ?? $"Faculty {faculty.FacultyId}"
                };

                _dw.DimFaculties.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Projects DW ETL - DimFaculty loaded ({Count} rows)",
                await _dw.DimFaculties.CountAsync(ct));
        }

        private async Task LoadDimResearchCategoryAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading DimResearchCategory...");

            var categories = await _projectsSource.ListResearchCategoriesAsync(ct);

            // PRIMERA PASADA: insertar todas las filas sin ParentCategoryKey
            foreach (var rc in categories)
            {
                var dim = new DimResearchCategory
                {
                    ResearchCategoryId = rc.Id,
                    Name = rc.Name,

                    ResearchCategoryTypeId = rc.ResearchCategoryTypeId,
                    CategoryTypeName = rc.CategoryTypeName,

                    ResearchCategoryGroupId = rc.ResearchCategoryGroupId,
                    ResearchCategoryGroupName = rc.ResearchCategoryGroupName,

                    ParentCategoryKey = null // se rellena en segunda pasada
                };

                _dw.DimResearchCategories.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);

            // SEGUNDA PASADA: resolver ParentCategoryKey
            var dimCategories = await _dw.DimResearchCategories
                .ToListAsync(ct);

            // Lookup por Id operacional
            var dimByOperationalId = dimCategories
                .ToDictionary(d => d.ResearchCategoryId, d => d);

            // Recorremos las categorías operacionales con padre
            foreach (var rc in categories.Where(c => c.ParentCategoryId != null))
            {
                // hijo en dimensión
                if (!dimByOperationalId.TryGetValue(rc.Id, out var childDim))
                {
                    RecordWarning("RESEARCH_CATEGORY_NOT_LOADED");
                    _logger.LogWarning(
                        "Projects DW ETL - DimResearchCategory not found for ResearchCategoryId {Id}",
                        rc.Id);
                    continue;
                }

                // padre en dimensión
                if (!dimByOperationalId.TryGetValue(rc.ParentCategoryId!.Value, out var parentDim))
                {
                    RecordWarning("RESEARCH_CATEGORY_PARENT_NOT_LOADED");
                    _logger.LogWarning(
                        "Projects DW ETL - Parent DimResearchCategory not found for ResearchCategoryId {Id} (ParentId={ParentId})",
                        rc.Id, rc.ParentCategoryId);
                    continue;
                }

                // asignar el surrogate del padre
                childDim.ParentCategoryKey = parentDim.ResearchCategoryKey;
            }

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Projects DW ETL - DimResearchCategory loaded ({Count} rows)",
                await _dw.DimResearchCategories.CountAsync(ct));
        }

        private async Task LoadDimIndexingDatabaseAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading DimIndexingDatabase...");

            var rawNames = await _projectsSource.ListProjectProductAttributeValuesAsync(
                BaseProductAttributeId.IndexingDatabase,
                ct);

            foreach (var name in rawNames)
            {
                var cleaned = name.Trim();

                var dim = new DimIndexingDatabase
                {
                    Name = cleaned
                };

                _dw.DimIndexingDatabases.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Projects DW ETL - DimIndexingDatabase loaded ({Count} rows)",
                await _dw.DimIndexingDatabases.CountAsync(ct));
        }

        private async Task LoadDimQuartileAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading DimQuartile...");

            var rawCodes = await _projectsSource.ListProjectProductAttributeValuesAsync(
                BaseProductAttributeId.Quartile,
                ct);

            foreach (var code in rawCodes)
            {
                var cleaned = code.Trim();

                var dim = new DimQuartile
                {
                    Code = cleaned,
                    Description = null // si luego quieres "Cuartil Q1", etc., lo llenas aquí
                };

                _dw.DimQuartiles.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Projects DW ETL - DimQuartile loaded ({Count} rows)",
                await _dw.DimQuartiles.CountAsync(ct));
        }

        private async Task LoadDimJournalAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading DimJournal...");

            var products = await _projectsSource.ListProjectProductsAsync(ct);
            var journals = products
                .Select(product => product.Journal?.Trim())
                .Where(journal => !string.IsNullOrEmpty(journal))
                .Select(journal => journal!)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var journal in journals)
            {
                _dw.DimJournals.Add(new DimJournal { Name = journal });
            }

            await _dw.SaveChangesAsync(ct);
            _logger.LogInformation("Projects DW ETL - DimJournal loaded ({Count} rows)", journals.Count);
        }

        private async Task LoadDimAuthorAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading DimAuthor...");

            var productAuthors = await _projectsSource.ListProjectProductAuthorsAsync(ct);

            foreach (var group in productAuthors.GroupBy(author => author.AuthorId))
            {
                var identities = group
                    .Select(author => new
                    {
                        author.IsInstitutional,
                        author.AppUserId,
                        author.IdAsp,
                        author.ExternalResearcherId,
                        author.ExternalFullName,
                        author.Orcid
                    })
                    .Distinct()
                    .ToList();

                if (identities.Count != 1)
                {
                    _logger.LogError(
                        "Projects DW ETL - Contradictory canonical identity data for AuthorId {AuthorId}.",
                        group.Key);
                    throw new InvalidOperationException(
                        $"Projects DW ETL - Contradictory canonical identity data for AuthorId {group.Key}.");
                }

                var identity = identities[0];
                _dw.DimAuthors.Add(new DimAuthor
                {
                    AuthorId = group.Key,
                    IsInstitutional = identity.IsInstitutional,
                    AppUserId = identity.AppUserId,
                    IdAsp = identity.IdAsp,
                    ExternalResearcherId = identity.ExternalResearcherId,
                    ExternalFullName = identity.ExternalFullName,
                    Orcid = identity.Orcid
                });
            }

            await _dw.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Projects DW ETL - DimAuthor loaded ({Count} rows)",
                await _dw.DimAuthors.CountAsync(ct));
        }

        // =========================================================
        // BRIDGES
        // =========================================================

        private async Task LoadBridgeProjectResearchCategoryAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading BridgeProjectResearchCategory...");

            var dimCategories = await _dw.DimResearchCategories
                .AsNoTracking()
                .ToListAsync(ct);

            var categoryLookup = dimCategories
                .ToDictionary(x => x.ResearchCategoryId, x => x.ResearchCategoryKey);

            var links = await _projectsSource.ListProjectResearchCategoriesAsync(ct);

            foreach (var link in links)
            {
                if (!categoryLookup.TryGetValue(link.ResearchCategoryId, out var rcKey))
                {
                    RecordWarning("BRIDGE_RESEARCH_CATEGORY_NOT_LOADED");
                    _logger.LogWarning(
                        "Projects DW ETL - ResearchCategoryId {Id} not found in DimResearchCategory",
                        link.ResearchCategoryId);
                    continue;
                }

                var bridge = new BridgeProjectResearchCategory
                {
                    ProjectId = link.ProjectId,
                    ResearchCategoryKey = rcKey
                };

                _dw.BridgeProjectResearchCategories.Add(bridge);
            }

            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadBridgeProductAuthorAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading BridgeProductAuthor...");

            var productLookup = await _dw.FactProducts
                .AsNoTracking()
                .ToDictionaryAsync(product => product.ProductId, product => product.FactProductId, ct);
            var authorLookup = await _dw.DimAuthors
                .AsNoTracking()
                .ToDictionaryAsync(author => author.AuthorId, author => author.AuthorKey, ct);
            var productAuthors = await _projectsSource.ListProjectProductAuthorsAsync(ct);

            foreach (var productAuthor in productAuthors)
            {
                if (!productLookup.TryGetValue(productAuthor.ProductId, out var factProductId))
                {
                    RecordWarning("BRIDGE_PRODUCT_NOT_LOADED");
                    _logger.LogWarning(
                        "Projects DW ETL - ProductAuthor {ProductAuthorId} references Product {ProductId}, which was not loaded into FactProduct.",
                        productAuthor.ProductAuthorId,
                        productAuthor.ProductId);
                    continue;
                }

                if (!authorLookup.TryGetValue(productAuthor.AuthorId, out var authorKey))
                {
                    _logger.LogError(
                        "Projects DW ETL - AuthorId {AuthorId} for ProductAuthor {ProductAuthorId} was not loaded into DimAuthor.",
                        productAuthor.AuthorId,
                        productAuthor.ProductAuthorId);
                    throw new InvalidOperationException(
                        $"Projects DW ETL - AuthorId {productAuthor.AuthorId} was not loaded into DimAuthor.");
                }

                _dw.BridgeProductAuthors.Add(new BridgeProductAuthor
                {
                    ProductAuthorId = productAuthor.ProductAuthorId,
                    FactProductId = factProductId,
                    AuthorKey = authorKey,
                    AuthorOrder = productAuthor.AuthorOrder,
                    IsPrimaryAuthor = productAuthor.IsPrimaryAuthor,
                    Participation = productAuthor.Participation,
                    NameSnapshot = productAuthor.NameSnapshot
                });
            }

            await _dw.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Projects DW ETL - BridgeProductAuthor loaded ({Count} rows)",
                await _dw.BridgeProductAuthors.CountAsync(ct));
        }

        // =========================================================
        // FACTS
        // =========================================================

        private async Task LoadFactProjectAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading FactProject...");

            var dimDate = await _dw.DimDates.AsNoTracking().ToListAsync(ct);
            var dimDateLookup = dimDate.ToDictionary(d => d.Date.Date, d => d.DateKey);

            var dimFaculty = await _dw.DimFaculties.AsNoTracking().ToListAsync(ct);
            var facultyLookup = dimFaculty.ToDictionary(f => f.FacultyId, f => f.FacultyKey);

            var dimStates = await _dw.DimProjectStates.AsNoTracking().ToListAsync(ct);
            var stateLookup = dimStates.ToDictionary(s => s.ProjectStateId, s => s.ProjectStateKey);

            var projects = await _projectsSource.ListProjectsAsync(ct);

            // Para fechas obligatorias (StartDate)
            int GetRequiredDateKey(DateTime date)
            {
                var d = date.Date;
                if (!dimDateLookup.TryGetValue(d, out var key))
                {
                    // Esto no debería pasar si DimDates está bien construida
                    throw new InvalidOperationException(
                        $"Projects DW ETL - Date {d:yyyy-MM-dd} not found in DimDates for required field.");
                }

                return key;
            }

            // Para fechas opcionales (EndDateKey)
            int? GetOptionalDateKey(DateTime? date)
            {
                if (date == null) return null;

                var d = date.Value.Date;
                if (dimDateLookup.TryGetValue(d, out var key))
                {
                    return key;
                }

                RecordWarning("OPTIONAL_PROJECT_DATE_NOT_LOADED");
                _logger.LogWarning(
                    "Projects DW ETL - Optional date {Date} not found in DimDates. Using NULL EndDateKey.",
                    d);

                return null;
            }

            foreach (var p in projects)
            {
                if (!facultyLookup.TryGetValue(p.FacultyId, out var facultyKey))
                {
                    RecordWarning("PROJECT_FACULTY_NOT_LOADED");
                    _logger.LogWarning("Projects DW ETL - FacultyId {Id} not found in DimFaculty", p.FacultyId);
                    continue;
                }

                if (!stateLookup.TryGetValue(p.ProjectStateId, out var stateKey))
                {
                    RecordWarning("PROJECT_STATE_NOT_LOADED");
                    _logger.LogWarning("Projects DW ETL - ProjectStateId {Id} not found in DimProjectState", p.ProjectStateId);
                    continue;
                }

                // 🔥 NUEVO: StartDate puede ser null, lo validamos
                if (p.StartDate == null)
                {
                    RecordWarning("PROJECT_START_DATE_MISSING");
                    _logger.LogWarning(
                        "Projects DW ETL - Project {ProjectId} has null StartDate; skipping in FactProject.",
                        p.ProjectId);
                    continue;
                }

                var fact = new FactProject
                {
                    ProjectId = p.ProjectId,
                    ProjectCount = 1,
                    DurationInMonths = p.DurationInMonths,
                    ExecutionPercentage = p.ExecutionPercentage ?? 0m,

                    FacultyKey = facultyKey,
                    ProjectStateKey = stateKey,

                    // ApprovalDateKey ahora es opcional: si no hay fecha, usamos -1 (sentinel)
                    ApprovalDateKey = p.ApprovalDate.HasValue
                        ? GetRequiredDateKey(p.ApprovalDate.Value)
                        : -1,

                    // ✅ ya no usamos .Value a lo loco
                    StartDateKey = GetRequiredDateKey(p.StartDate.Value),

                    // Este es opcional → int?
                    EndDateKey = GetOptionalDateKey(p.RealEndDate)
                };

                _dw.FactProjects.Add(fact);
            }

            await _dw.SaveChangesAsync(ct);
        }


        private async Task LoadFactBudgetAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading FactBudget...");

            var dimDate = await _dw.DimDates.AsNoTracking().ToListAsync(ct);
            var dimDateLookup = dimDate.ToDictionary(d => d.Date.Date, d => d.DateKey);

            var dimFaculty = await _dw.DimFaculties.AsNoTracking().ToListAsync(ct);
            var facultyLookup = dimFaculty.ToDictionary(f => f.FacultyId, f => f.FacultyKey);

            var dimFunding = await _dw.DimFundingTypes.AsNoTracking().ToListAsync(ct);
            var fundingLookup = dimFunding.ToDictionary(f => f.FundingTypeId, f => f.FundingTypeKey);

            var budgets = await _projectsSource.ListBudgetsAsync(ct);

            int GetDateKey(DateTime? date)
            {
                if (date == null) return 0;
                var d = date.Value.Date;
                return dimDateLookup.TryGetValue(d, out var key) ? key : 0;
            }

            foreach (var b in budgets)
            {
                if (!facultyLookup.TryGetValue(b.FacultyId, out var facultyKey))
                {
                    RecordWarning("BUDGET_FACULTY_NOT_LOADED");
                    _logger.LogWarning("Projects DW ETL - FacultyId {Id} not found in DimFaculty", b.FacultyId);
                    continue;
                }

                if (!fundingLookup.TryGetValue(b.FundingTypeId, out var fundingKey))
                {
                    RecordWarning("BUDGET_FUNDING_TYPE_NOT_LOADED");
                    _logger.LogWarning("Projects DW ETL - FundingTypeId {Id} not found in DimFundingType", b.FundingTypeId);
                    continue;
                }

                var fact = new FactBudget
                {
                    BudgetId = b.BudgetId,
                    ProjectId = b.ProjectId,

                    InitialAmount = b.InitialAmount,
                    CertifiedAmount = b.CertifiedAmount,
                    ExecutedAmount = b.ExecutedAmount,

                    FacultyKey = facultyKey,
                    FundingTypeKey = fundingKey,
                    ApprovedDateKey = GetDateKey(b.ApprovedAt)
                };

                _dw.FactBudgets.Add(fact);
            }

            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadFactProductAsync(CancellationToken ct)
        {
            _logger.LogInformation("Projects DW ETL - Loading FactProduct...");

            var dimDate = await _dw.DimDates.AsNoTracking().ToListAsync(ct);
            var dimDateLookup = dimDate.ToDictionary(d => d.Date.Date, d => d.DateKey);

            var dimFaculty = await _dw.DimFaculties.AsNoTracking().ToListAsync(ct);
            var facultyLookup = dimFaculty.ToDictionary(f => f.FacultyId, f => f.FacultyKey);

            var dimProductTypes = await _dw.DimProductTypes.AsNoTracking().ToListAsync(ct);
            var productTypeLookup = dimProductTypes.ToDictionary(p => p.ProductTypeId, p => p.ProductTypeKey);

            var dimIndexing = await _dw.DimIndexingDatabases.AsNoTracking().ToListAsync(ct);
            var indexingLookup = dimIndexing
                .ToDictionary(d => d.Name.Trim(), d => d.IndexingDatabaseKey);

            var dimQuartiles = await _dw.DimQuartiles.AsNoTracking().ToListAsync(ct);
            var quartileLookup = dimQuartiles
                .ToDictionary(q => q.Code.Trim(), q => q.QuartileKey);

            var dimJournals = await _dw.DimJournals.AsNoTracking().ToListAsync(ct);
            var journalLookup = dimJournals
                .ToDictionary(journal => journal.Name, journal => journal.JournalKey);

            var products = await _projectsSource.ListProjectProductsAsync(ct);

            int GetDateKey(DateTime? date)
            {
                if (date == null) return 0;
                var d = date.Value.Date;
                return dimDateLookup.TryGetValue(d, out var key) ? key : 0;
            }

            foreach (var p in products)
            {
                if (!facultyLookup.TryGetValue(p.FacultyId, out var facultyKey))
                {
                    RecordWarning("PRODUCT_FACULTY_NOT_LOADED");
                    _logger.LogWarning("Projects DW ETL - FacultyId {Id} not found in DimFaculty", p.FacultyId);
                    continue;
                }

                if (!productTypeLookup.TryGetValue(p.ProductTypeId, out var productTypeKey))
                {
                    RecordWarning("PRODUCT_TYPE_NOT_LOADED");
                    _logger.LogWarning("Projects DW ETL - ProductTypeId {Id} not found in DimProductType", p.ProductTypeId);
                    continue;
                }

                int? indexingKey = null;
                int? quartileKey = null;
                int? journalKey = null;

                // ===== BASE DE DATOS (atributo base canónico 4) =====
                if (!string.IsNullOrWhiteSpace(p.IndexingDatabase))
                {
                    var cleaned = p.IndexingDatabase.Trim();

                    if (indexingLookup.TryGetValue(cleaned, out var idxKey))
                    {
                        indexingKey = idxKey;
                    }
                    else
                    {
                        RecordWarning("INDEXING_DATABASE_NOT_LOADED");
                        _logger.LogWarning(
                            "Projects DW ETL - Indexing database '{Name}' not found in DimIndexingDatabase (ProductId={ProductId})",
                            cleaned, p.ProductId);
                    }
                }

                // ===== CUARTIL (atributo base canónico 6) =====
                if (!string.IsNullOrWhiteSpace(p.Quartile))
                {
                    var cleaned = p.Quartile.Trim();

                    if (quartileLookup.TryGetValue(cleaned, out var qKey))
                    {
                        quartileKey = qKey;
                    }
                    else
                    {
                        RecordWarning("QUARTILE_NOT_LOADED");
                        _logger.LogWarning(
                            "Projects DW ETL - Quartile '{Code}' not found in DimQuartile (ProductId={ProductId})",
                            cleaned, p.ProductId);
                    }
                }

                if (!string.IsNullOrWhiteSpace(p.Journal))
                {
                    var journal = p.Journal.Trim();
                    if (journalLookup.TryGetValue(journal, out var resolvedJournalKey))
                    {
                        journalKey = resolvedJournalKey;
                    }
                    else
                    {
                        RecordWarning("JOURNAL_NOT_LOADED");
                        _logger.LogWarning(
                            "Projects DW ETL - Journal '{Journal}' not found in DimJournal (ProductId={ProductId})",
                            journal,
                            p.ProductId);
                    }
                }

                int? publicationYear = null;
                if (!string.IsNullOrWhiteSpace(p.PublicationYearRaw))
                {
                    var rawYear = p.PublicationYearRaw.Trim();
                    if (int.TryParse(rawYear, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedYear))
                    {
                        publicationYear = parsedYear;
                    }
                    else
                    {
                        RecordWarning("PUBLICATION_YEAR_INVALID");
                        _logger.LogWarning(
                            "Projects DW ETL - Publication year '{PublicationYear}' is not an integer (ProductId={ProductId}); using NULL.",
                            rawYear,
                            p.ProductId);
                    }
                }

                var fact = new FactProduct
                {
                    ProductId = p.ProductId,
                    ProjectId = p.ProjectId,

                    ProductCount = 1,
                    AuthorCount = p.AuthorCount,
                    IsActiveFlag = p.IsActive,

                    Title = p.Title,
                    Doi = p.Doi,
                    PublicationYear = publicationYear,
                    IssnIsbn = p.IssnIsbn,

                    FacultyKey = facultyKey,
                    ProductTypeKey = productTypeKey,
                    CreatedDateKey = GetDateKey(p.CreatedAt),

                    IndexingDatabaseKey = indexingKey,
                    QuartileKey = quartileKey,
                    JournalKey = journalKey
                };

                _dw.FactProducts.Add(fact);
            }

            await _dw.SaveChangesAsync(ct);
        }

        private sealed class WarningCollector
        {
            private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

            public void Add(string code)
                => _counts[code] = _counts.GetValueOrDefault(code) + 1;

            public IReadOnlyList<DwEtlWarningCount> Snapshot()
                => _counts.OrderBy(x => x.Key, StringComparer.Ordinal)
                    .Select(x => new DwEtlWarningCount(x.Key, x.Value))
                    .ToArray();
        }
    }
}


