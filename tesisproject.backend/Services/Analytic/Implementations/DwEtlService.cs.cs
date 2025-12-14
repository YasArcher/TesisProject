using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.shared.Entities.Analytics.Dw.Bridges;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Analytics.Dw.Facts;
using tesisproject.shared.Responses;
using tesisproject.backend.Services.Interfaces;

namespace tesisproject.backend.Services.Analytic.Implementations
{
    /// <summary>
    /// Default implementation of IDwEtlService.
    /// Reads from AppDbContext (operational) and writes into DwContext (DW).
    /// </summary>
    public class DwEtlService : IDwEtlService
    {
        private readonly AppDbContext _appDb;
        private readonly DwContext _dw;
        private readonly ILogger<DwEtlService> _logger;
        private readonly IExternalAcademicsService _externalAcademics;

        public DwEtlService(
            AppDbContext appDb,
            DwContext dw,
            ILogger<DwEtlService> logger,
            IExternalAcademicsService externalAcademics)
        {
            _appDb = appDb;
            _dw = dw;
            _logger = logger;
            _externalAcademics = externalAcademics;
        }

        // =========================================================
        // Public API
        // =========================================================

        public async Task<ServiceResult<NoContent>> RunFullLoadAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("DW ETL - Full load started");

            await ClearDwAsync(ct);
            await LoadDimensionsAsync(ct);
            await LoadFactsAsync(ct);
            await LoadBridgesAsync(ct);

            _logger.LogInformation("DW ETL - Full load finished");

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        // =========================================================
        // CLEAR DW
        // =========================================================

        private async Task ClearDwAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Clearing DW tables (bridges, facts, dimensions)...");

            // 1) Bridges (dependen de dimensiones y facts)
            _dw.BridgeProjectResearchCategories.RemoveRange(_dw.BridgeProjectResearchCategories);

            // 2) Facts (dependen de dimensiones)
            _dw.FactProducts.RemoveRange(_dw.FactProducts);
            _dw.FactBudgets.RemoveRange(_dw.FactBudgets);
            _dw.FactProjects.RemoveRange(_dw.FactProjects);

            // 3) Dimensions (base del modelo)
            _dw.DimResearchCategories.RemoveRange(_dw.DimResearchCategories);
            _dw.DimQuartiles.RemoveRange(_dw.DimQuartiles);
            _dw.DimIndexingDatabases.RemoveRange(_dw.DimIndexingDatabases);
            _dw.DimProductTypes.RemoveRange(_dw.DimProductTypes);
            _dw.DimFundingTypes.RemoveRange(_dw.DimFundingTypes);
            _dw.DimProjectStates.RemoveRange(_dw.DimProjectStates);
            _dw.DimFaculties.RemoveRange(_dw.DimFaculties);
            _dw.DimDates.RemoveRange(_dw.DimDates);

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation("DW ETL - DW tables cleared.");
        }

        // =========================================================
        // LOAD DIMENSIONS / FACTS / BRIDGES
        // =========================================================

        public async Task<ServiceResult<NoContent>> LoadDimensionsAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("DW ETL - Loading dimensions...");

            await LoadDimDateAsync(ct);
            await LoadDimProjectStateAsync(ct);
            await LoadDimFundingTypeAsync(ct);
            await LoadDimProductTypeAsync(ct);
            await LoadDimFacultyAsync(ct);
            await LoadDimResearchCategoryAsync(ct);
            await LoadDimIndexingDatabaseAsync(ct);
            await LoadDimQuartileAsync(ct);

            _logger.LogInformation("DW ETL - Dimensions loaded");

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        public async Task<ServiceResult<NoContent>> LoadBridgesAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("DW ETL - Loading bridge tables...");

            await LoadBridgeProjectResearchCategoryAsync(ct);

            _logger.LogInformation("DW ETL - Bridge tables loaded");

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        public async Task<ServiceResult<NoContent>> LoadFactsAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("DW ETL - Loading fact tables...");

            await LoadFactProjectAsync(ct);
            await LoadFactBudgetAsync(ct);
            await LoadFactProductAsync(ct);

            _logger.LogInformation("DW ETL - Fact tables loaded");

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        // =========================================================
        // DIMENSIONS
        // =========================================================

        private async Task LoadDimDateAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading DimDate...");

            // =============================
            // Rango desde Projects
            // =============================

            // StartDate (inicio de ejecución)
            var minStartDate = await _appDb.Projects
                .Select(p => (DateTime?)p.StartDate)
                .Where(d => d != null)
                .MinAsync(ct);

            var maxStartDate = await _appDb.Projects
                .Select(p => (DateTime?)p.StartDate)
                .Where(d => d != null)
                .MaxAsync(ct);

            // RealEndDate (fin real de ejecución)
            var minEndDate = await _appDb.Projects
                .Select(p => (DateTime?)p.RealEndDate)
                .Where(d => d != null)
                .MinAsync(ct);

            var maxEndDate = await _appDb.Projects
                .Select(p => (DateTime?)p.RealEndDate)
                .Where(d => d != null)
                .MaxAsync(ct);

            // ApprovalDate (fecha de aprobación del proyecto) - ahora puede ser null
            var minApprovalDate = await _appDb.Projects
                .Select(p => (DateTime?)p.ApprovalDate)
                .Where(d => d != null)
                .MinAsync(ct);

            var maxApprovalDate = await _appDb.Projects
                .Select(p => (DateTime?)p.ApprovalDate)
                .Where(d => d != null)
                .MaxAsync(ct);

            // =============================
            // Rango desde Budgets
            // =============================
            var minBudgetDate = await _appDb.Budgets
                .Select(b => (DateTime?)b.ApprovedAt)
                .Where(d => d != null)
                .MinAsync(ct);

            var maxBudgetDate = await _appDb.Budgets
                .Select(b => (DateTime?)b.ApprovedAt)
                .Where(d => d != null)
                .MaxAsync(ct);

            // =============================
            // Rango desde Products
            // =============================
            var minProductDate = await _appDb.Products
                .Select(p => (DateTime?)p.CreatedAt)
                .Where(d => d != null)
                .MinAsync(ct);

            var maxProductDate = await _appDb.Products
                .Select(p => (DateTime?)p.CreatedAt)
                .Where(d => d != null)
                .MaxAsync(ct);

            // =============================
            // Combinar todos los mínimos y máximos
            // =============================

            var minDate = new[]
                {
                    minStartDate,
                    minEndDate,
                    minApprovalDate,
                    minBudgetDate,
                    minProductDate
                }
                .Where(d => d.HasValue)
                .Select(d => d!.Value.Date)
                .DefaultIfEmpty(DateTime.Today.Date)
                .Min();

            var maxDate = new[]
                {
                    maxStartDate,
                    maxEndDate,
                    maxApprovalDate,
                    maxBudgetDate,
                    maxProductDate
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
            var academicPeriods = await _appDb.AcademicPeriods
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.StartDate)
                .ToListAsync(ct);

            var current = minDate;
            while (current <= maxDate)
            {
                var dateKey = current.Year * 10000 + current.Month * 100 + current.Day;

                string? periodName = null;
                var period = academicPeriods
                    .FirstOrDefault(p =>
                        p.StartDate.Date <= current.Date &&
                        current.Date <= p.EndDate.Date);

                if (period is not null)
                    periodName = period.Name;

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
                "DW ETL - DimDate loaded ({Count} rows)",
                await _dw.DimDates.CountAsync(ct));
        }

        private async Task LoadDimProjectStateAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading DimProjectState...");

            var states = await _appDb.ProjectStates
                .AsNoTracking()
                .ToListAsync(ct);

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
            _logger.LogInformation("DW ETL - Loading DimFundingType...");

            var fundingTypes = await _appDb.FundingTypes
                .AsNoTracking()
                .ToListAsync(ct);

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
            _logger.LogInformation("DW ETL - Loading DimProductType...");

            var productTypes = await _appDb.ProductTypes
                .AsNoTracking()
                .ToListAsync(ct);

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
            _logger.LogInformation("DW ETL - Loading DimFaculty...");

            // Facultades realmente usadas en Projects
            var facultyIdsInUse = await _appDb.Projects
                .AsNoTracking()
                .Select(p => p.FacultyId)
                .Distinct()
                .ToListAsync(ct);

            if (!facultyIdsInUse.Any())
            {
                _logger.LogInformation(
                    "DW ETL - No FacultyId found in Projects; DimFaculty remains empty.");
                return;
            }

            // Consultar API externa (todas las facultades)
            var apiResult = await _externalAcademics.GetFacultiesAsync(ct);

            if (!apiResult.Success || apiResult.Data is null)
            {
                _logger.LogWarning(
                    "DW ETL - External faculties API failed: {Message}. " +
                    "DimFaculty will be populated with placeholder names.",
                    apiResult.Message);

                // En este caso igual poblamos la dimensión, pero sin nombre real
                foreach (var facultyId in facultyIdsInUse)
                {
                    var dimFallback = new DimFaculty
                    {
                        FacultyId = facultyId,
                        FacultyCode = null,
                        FacultyName = $"Faculty {facultyId}"
                    };

                    _dw.DimFaculties.Add(dimFallback);
                }

                await _dw.SaveChangesAsync(ct);
                return;
            }

            // Construir lookup por FacultyId desde la API
            var apiFacultiesById = apiResult.Data
                .GroupBy(f => f.FacultyId)
                .ToDictionary(g => g.Key, g => g.First());

            // Poblar DimFaculty solo para las facultades que aparecen en Projects
            foreach (var facultyId in facultyIdsInUse)
            {
                apiFacultiesById.TryGetValue(facultyId, out var external);

                if (external is null)
                {
                    _logger.LogWarning(
                        "DW ETL - FacultyId {FacultyId} not found in external API. " +
                        "Using placeholder name.",
                        facultyId);
                }

                var dim = new DimFaculty
                {
                    FacultyId = facultyId,
                    FacultyCode = external?.Acronym,
                    FacultyName = external?.Name ?? $"Faculty {facultyId}"
                };

                _dw.DimFaculties.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation(
                "DW ETL - DimFaculty loaded ({Count} rows)",
                await _dw.DimFaculties.CountAsync(ct));
        }

        private async Task LoadDimResearchCategoryAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading DimResearchCategory...");

            // Traer categorías operacionales con su tipo y grupo
            var categories = await _appDb.ResearchCategories
                .Include(rc => rc.ResearchCategoryType)
                    .ThenInclude(t => t.ResearchCategoryGroup)
                .AsNoTracking()
                .ToListAsync(ct);

            // PRIMERA PASADA: insertar todas las filas sin ParentCategoryKey
            foreach (var rc in categories)
            {
                var type = rc.ResearchCategoryType;
                var group = type?.ResearchCategoryGroup;

                var dim = new DimResearchCategory
                {
                    ResearchCategoryId = rc.Id,
                    Name = rc.Name,

                    ResearchCategoryTypeId = rc.ResearchCategoryTypeId,
                    CategoryTypeName = type?.Name ?? string.Empty,

                    ResearchCategoryGroupId = type?.ResearchCategoryGroupId ?? 0,
                    ResearchCategoryGroupName = group?.Name ?? string.Empty,

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
                    _logger.LogWarning(
                        "DW ETL - DimResearchCategory not found for ResearchCategoryId {Id}",
                        rc.Id);
                    continue;
                }

                // padre en dimensión
                if (!dimByOperationalId.TryGetValue(rc.ParentCategoryId!.Value, out var parentDim))
                {
                    _logger.LogWarning(
                        "DW ETL - Parent DimResearchCategory not found for ResearchCategoryId {Id} (ParentId={ParentId})",
                        rc.Id, rc.ParentCategoryId);
                    continue;
                }

                // asignar el surrogate del padre
                childDim.ParentCategoryKey = parentDim.ResearchCategoryKey;
            }

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation(
                "DW ETL - DimResearchCategory loaded ({Count} rows)",
                await _dw.DimResearchCategories.CountAsync(ct));
        }

        private async Task LoadDimIndexingDatabaseAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading DimIndexingDatabase...");

            // Tomamos valores de ProductValues cuyo atributo sea "BASE DE DATOS"
            var rawNames = await _appDb.ProductValues
                .Include(v => v.AttributeDefinition!)
                    .ThenInclude(ad => ad.ProductAttribute!)
                .Where(v =>
                    v.AttributeDefinition != null &&
                    v.AttributeDefinition.ProductAttribute != null &&
                    v.AttributeDefinition.ProductAttribute.Name == "BASE DE DATOS")
                .Select(v => v.Value)
                .Where(v => v != null && v != "")
                .Distinct()
                .ToListAsync(ct);

            foreach (var name in rawNames)
            {
                var cleaned = name!.Trim();

                var dim = new DimIndexingDatabase
                {
                    Name = cleaned
                };

                _dw.DimIndexingDatabases.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation(
                "DW ETL - DimIndexingDatabase loaded ({Count} rows)",
                await _dw.DimIndexingDatabases.CountAsync(ct));
        }

        private async Task LoadDimQuartileAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading DimQuartile...");

            // Tomamos todos los valores distintos del atributo "CUARTIL"
            var rawCodes = await _appDb.ProductValues
                .Include(v => v.AttributeDefinition!)
                    .ThenInclude(ad => ad.ProductAttribute!)
                .Where(v =>
                    v.AttributeDefinition != null &&
                    v.AttributeDefinition.ProductAttribute != null &&
                    v.AttributeDefinition.ProductAttribute.Name == "CUARTIL")
                .Select(v => v.Value)
                .Where(v => v != null && v != "")
                .Distinct()
                .ToListAsync(ct);

            foreach (var code in rawCodes)
            {
                var cleaned = code!.Trim();

                var dim = new DimQuartile
                {
                    Code = cleaned,
                    Description = null // si luego quieres "Cuartil Q1", etc., lo llenas aquí
                };

                _dw.DimQuartiles.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation(
                "DW ETL - DimQuartile loaded ({Count} rows)",
                await _dw.DimQuartiles.CountAsync(ct));
        }

        // =========================================================
        // BRIDGES
        // =========================================================

        private async Task LoadBridgeProjectResearchCategoryAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading BridgeProjectResearchCategory...");

            var dimCategories = await _dw.DimResearchCategories
                .AsNoTracking()
                .ToListAsync(ct);

            var categoryLookup = dimCategories
                .ToDictionary(x => x.ResearchCategoryId, x => x.ResearchCategoryKey);

            var links = await _appDb.ProjectResearchCategories
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var link in links)
            {
                if (!categoryLookup.TryGetValue(link.ResearchCategoryId, out var rcKey))
                {
                    _logger.LogWarning(
                        "DW ETL - ResearchCategoryId {Id} not found in DimResearchCategory",
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

        // =========================================================
        // FACTS
        // =========================================================

        private async Task LoadFactProjectAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading FactProject...");

            var dimDate = await _dw.DimDates.AsNoTracking().ToListAsync(ct);
            var dimDateLookup = dimDate.ToDictionary(d => d.Date.Date, d => d.DateKey);

            var dimFaculty = await _dw.DimFaculties.AsNoTracking().ToListAsync(ct);
            var facultyLookup = dimFaculty.ToDictionary(f => f.FacultyId, f => f.FacultyKey);

            var dimStates = await _dw.DimProjectStates.AsNoTracking().ToListAsync(ct);
            var stateLookup = dimStates.ToDictionary(s => s.ProjectStateId, s => s.ProjectStateKey);

            var projects = await _appDb.Projects
                .AsNoTracking()
                .ToListAsync(ct);

            // Para fechas obligatorias (StartDate)
            int GetRequiredDateKey(DateTime date)
            {
                var d = date.Date;
                if (!dimDateLookup.TryGetValue(d, out var key))
                {
                    // Esto no debería pasar si DimDates está bien construida
                    throw new InvalidOperationException(
                        $"DW ETL - Date {d:yyyy-MM-dd} not found in DimDates for required field.");
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

                _logger.LogWarning(
                    "DW ETL - Optional date {Date} not found in DimDates. Using NULL EndDateKey.",
                    d);

                return null;
            }

            foreach (var p in projects)
            {
                if (!facultyLookup.TryGetValue(p.FacultyId, out var facultyKey))
                {
                    _logger.LogWarning("DW ETL - FacultyId {Id} not found in DimFaculty", p.FacultyId);
                    continue;
                }

                if (!stateLookup.TryGetValue(p.ProjectStateId, out var stateKey))
                {
                    _logger.LogWarning("DW ETL - ProjectStateId {Id} not found in DimProjectState", p.ProjectStateId);
                    continue;
                }

                // 🔥 NUEVO: StartDate puede ser null, lo validamos
                if (p.StartDate == null)
                {
                    _logger.LogWarning(
                        "DW ETL - Project {ProjectId} has null StartDate; skipping in FactProject.",
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
            _logger.LogInformation("DW ETL - Loading FactBudget...");

            var dimDate = await _dw.DimDates.AsNoTracking().ToListAsync(ct);
            var dimDateLookup = dimDate.ToDictionary(d => d.Date.Date, d => d.DateKey);

            var dimFaculty = await _dw.DimFaculties.AsNoTracking().ToListAsync(ct);
            var facultyLookup = dimFaculty.ToDictionary(f => f.FacultyId, f => f.FacultyKey);

            var dimFunding = await _dw.DimFundingTypes.AsNoTracking().ToListAsync(ct);
            var fundingLookup = dimFunding.ToDictionary(f => f.FundingTypeId, f => f.FundingTypeKey);

            var budgets = await _appDb.Budgets
                .Include(b => b.Project)
                .AsNoTracking()
                .ToListAsync(ct);

            int GetDateKey(DateTime? date)
            {
                if (date == null) return 0;
                var d = date.Value.Date;
                return dimDateLookup.TryGetValue(d, out var key) ? key : 0;
            }

            foreach (var b in budgets)
            {
                if (b.Project == null)
                {
                    _logger.LogWarning("DW ETL - Budget {Id} has no Project loaded", b.BudgetId);
                    continue;
                }

                if (!facultyLookup.TryGetValue(b.Project.FacultyId, out var facultyKey))
                {
                    _logger.LogWarning("DW ETL - FacultyId {Id} not found in DimFaculty", b.Project.FacultyId);
                    continue;
                }

                if (!fundingLookup.TryGetValue(b.FundingTypeId, out var fundingKey))
                {
                    _logger.LogWarning("DW ETL - FundingTypeId {Id} not found in DimFundingType", b.FundingTypeId);
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
            _logger.LogInformation("DW ETL - Loading FactProduct...");

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

            var products = await _appDb.Products
                .Include(p => p.Project)
                .Include(p => p.Values!)
                    .ThenInclude(v => v.AttributeDefinition!)
                        .ThenInclude(ad => ad.ProductAttribute!)
                .AsNoTracking()
                .ToListAsync(ct);

            int GetDateKey(DateTime? date)
            {
                if (date == null) return 0;
                var d = date.Value.Date;
                return dimDateLookup.TryGetValue(d, out var key) ? key : 0;
            }

            foreach (var p in products)
            {
                if (p.Project == null)
                {
                    _logger.LogWarning("DW ETL - Product {Id} has no Project loaded", p.Id);
                    continue;
                }

                if (!facultyLookup.TryGetValue(p.Project.FacultyId, out var facultyKey))
                {
                    _logger.LogWarning("DW ETL - FacultyId {Id} not found in DimFaculty", p.Project.FacultyId);
                    continue;
                }

                if (!productTypeLookup.TryGetValue(p.ProductTypeId, out var productTypeKey))
                {
                    _logger.LogWarning("DW ETL - ProductTypeId {Id} not found in DimProductType", p.ProductTypeId);
                    continue;
                }

                int? indexingKey = null;
                int? quartileKey = null;

                // ===== BASE DE DATOS (usa ProductAttribute.Name) =====
                var dbAttr = p.Values?
                    .FirstOrDefault(v =>
                        v.AttributeDefinition != null &&
                        v.AttributeDefinition.ProductAttribute != null &&
                        v.AttributeDefinition.ProductAttribute.Name == "BASE DE DATOS");

                if (dbAttr != null && !string.IsNullOrWhiteSpace(dbAttr.Value))
                {
                    var cleaned = dbAttr.Value.Trim();

                    if (indexingLookup.TryGetValue(cleaned, out var idxKey))
                    {
                        indexingKey = idxKey;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "DW ETL - Indexing database '{Name}' not found in DimIndexingDatabase (ProductId={ProductId})",
                            cleaned, p.Id);
                    }
                }

                // ===== CUARTIL (usa ProductAttribute.Name) =====
                var quartAttr = p.Values?
                    .FirstOrDefault(v =>
                        v.AttributeDefinition != null &&
                        v.AttributeDefinition.ProductAttribute != null &&
                        v.AttributeDefinition.ProductAttribute.Name == "CUARTIL");

                if (quartAttr != null && !string.IsNullOrWhiteSpace(quartAttr.Value))
                {
                    var cleaned = quartAttr.Value.Trim();

                    if (quartileLookup.TryGetValue(cleaned, out var qKey))
                    {
                        quartileKey = qKey;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "DW ETL - Quartile '{Code}' not found in DimQuartile (ProductId={ProductId})",
                            cleaned, p.Id);
                    }
                }

                var fact = new FactProduct
                {
                    ProductId = p.Id,
                    ProjectId = p.ProjectId,

                    ProductCount = 1,
                    IsActiveFlag = p.IsActive,

                    FacultyKey = facultyKey,
                    ProductTypeKey = productTypeKey,
                    CreatedDateKey = GetDateKey(p.CreatedAt),

                    IndexingDatabaseKey = indexingKey,
                    QuartileKey = quartileKey
                };

                _dw.FactProducts.Add(fact);
            }

            await _dw.SaveChangesAsync(ct);
        }
    }
}
