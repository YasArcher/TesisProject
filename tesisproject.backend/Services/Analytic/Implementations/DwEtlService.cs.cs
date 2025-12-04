using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Services.Analytic.Interfaces;
using tesisproject.shared.Entities.Analytics.Dw.Bridges;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;
using tesisproject.shared.Entities.Analytics.Dw.Facts;
using tesisproject.shared.Responses;

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

        public DwEtlService(
            AppDbContext appDb,
            DwContext dw,
            ILogger<DwEtlService> logger)
        {
            _appDb = appDb;
            _dw = dw;
            _logger = logger;
        }

        // =========================================================
        // Public API
        // =========================================================

        public async Task<ServiceResult<NoContent>> RunFullLoadAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("DW ETL - Full load started");

            await LoadDimensionsAsync(ct);
            await LoadBridgesAsync(ct);
            await LoadFactsAsync(ct);

            _logger.LogInformation("DW ETL - Full load finished");

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

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

            _dw.DimDates.RemoveRange(_dw.DimDates);
            await _dw.SaveChangesAsync(ct);

            // Obtener rangos desde Projects / Budgets / Products
            var minProjectDate = await _appDb.Projects
                .Select(p => (DateTime?)p.StartDate)
                .Where(d => d != null)
                .MinAsync(ct);

            var maxProjectDate = await _appDb.Projects
                .Select(p => (DateTime?)p.RealEndDate)
                .Where(d => d != null)
                .MaxAsync(ct);

            var minBudgetDate = await _appDb.Budgets
                .Select(b => (DateTime?)b.ApprovedAt)
                .Where(d => d != null)
                .MinAsync(ct);

            var maxBudgetDate = await _appDb.Budgets
                .Select(b => (DateTime?)b.ApprovedAt)
                .Where(d => d != null)
                .MaxAsync(ct);

            var minProductDate = await _appDb.Products
                .Select(p => (DateTime?)p.CreatedAt)
                .Where(d => d != null)
                .MinAsync(ct);

            var maxProductDate = await _appDb.Products
                .Select(p => (DateTime?)p.CreatedAt)
                .Where(d => d != null)
                .MaxAsync(ct);

            var minDate = new[] { minProjectDate, minBudgetDate, minProductDate }
                .Where(d => d.HasValue)
                .Select(d => d!.Value.Date)
                .DefaultIfEmpty(DateTime.Today.Date)
                .Min();

            var maxDate = new[] { maxProjectDate, maxBudgetDate, maxProductDate }
                .Where(d => d.HasValue)
                .Select(d => d!.Value.Date)
                .DefaultIfEmpty(DateTime.Today.Date)
                .Max();

            if (minDate > maxDate)
            {
                minDate = maxDate = DateTime.Today.Date;
            }

            var current = minDate;
            while (current <= maxDate)
            {
                var dateKey = current.Year * 10000 + current.Month * 100 + current.Day;

                var dim = new DimDate
                {
                    DateKey = dateKey,
                    Date = current,
                    Year = current.Year,
                    Month = current.Month,
                    Day = current.Day,
                    PeriodName = null
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

            _dw.DimProjectStates.RemoveRange(_dw.DimProjectStates);
            await _dw.SaveChangesAsync(ct);

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
        private async Task LoadDimIndexingDatabaseAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading DimIndexingDatabase...");

            _dw.DimIndexingDatabases.RemoveRange(_dw.DimIndexingDatabases);
            await _dw.SaveChangesAsync(ct);

            var rawNames = await _appDb.ProductValues
                .Include(v => v.AttributeDefinition)
                .Where(v => v.AttributeDefinition != null &&
                            v.AttributeDefinition.AttributeName == "BASE DE DATOS")
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


        private async Task LoadDimFundingTypeAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading DimFundingType...");

            _dw.DimFundingTypes.RemoveRange(_dw.DimFundingTypes);
            await _dw.SaveChangesAsync(ct);

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

            _dw.DimProductTypes.RemoveRange(_dw.DimProductTypes);
            await _dw.SaveChangesAsync(ct);

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

            _dw.DimFaculties.RemoveRange(_dw.DimFaculties);
            await _dw.SaveChangesAsync(ct);

            var facultyIds = await _appDb.Projects
                .AsNoTracking()
                .Select(p => p.FacultyId)
                .Distinct()
                .ToListAsync(ct);

            // TODO: integrar cliente de API externa para obtener FacultyCode / FacultyName reales.
            foreach (var facultyId in facultyIds)
            {
                var dim = new DimFaculty
                {
                    FacultyId = facultyId,
                    FacultyCode = null,
                    FacultyName = $"Faculty {facultyId}" // placeholder
                };

                _dw.DimFaculties.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadDimResearchCategoryAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading DimResearchCategory...");

            _dw.DimResearchCategories.RemoveRange(_dw.DimResearchCategories);
            await _dw.SaveChangesAsync(ct);

            var categories = await _appDb.ResearchCategories
                .Include(rc => rc.ResearchCategoryType)
                    .ThenInclude(t => t.ResearchCategoryGroup)
                .AsNoTracking()
                .ToListAsync(ct);

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

                    // Por ahora no resolvemos ParentCategoryKey (requiere segunda pasada).
                    ParentCategoryKey = null
                };

                _dw.DimResearchCategories.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);

            // Si luego quieres resolver ParentCategoryKey:
            // - Cargar DimResearchCategory
            // - Hacer lookup por ResearchCategoryId del padre
            // - Actualizar ParentCategoryKey y SaveChanges
        }

        private async Task LoadDimQuartileAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading DimQuartile...");

            _dw.DimQuartiles.RemoveRange(_dw.DimQuartiles);
            await _dw.SaveChangesAsync(ct);

            // Tomamos todos los valores distintos del atributo "CUARTIL"
            var rawCodes = await _appDb.ProductValues
                .Include(v => v.AttributeDefinition)
                .Where(v => v.AttributeDefinition != null &&
                            v.AttributeDefinition.AttributeName == "CUARTIL")
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
                    Description = null // si luego quieres "Cuartil Q1", etc., lo llenamos
                };

                _dw.DimQuartiles.Add(dim);
            }

            await _dw.SaveChangesAsync(ct);

            _logger.LogInformation(
                "DW ETL - DimQuartile loaded ({Count} rows)",
                await _dw.DimQuartiles.CountAsync(ct));
        }

        // =========================================================
        // BRIDGE
        // =========================================================

        private async Task LoadBridgeProjectResearchCategoryAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading BridgeProjectResearchCategory...");

            _dw.BridgeProjectResearchCategories.RemoveRange(_dw.BridgeProjectResearchCategories);
            await _dw.SaveChangesAsync(ct);

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

            _dw.FactProjects.RemoveRange(_dw.FactProjects);
            await _dw.SaveChangesAsync(ct);

            var dimDate = await _dw.DimDates.AsNoTracking().ToListAsync(ct);
            var dimDateLookup = dimDate.ToDictionary(d => d.Date.Date, d => d.DateKey);

            var dimFaculty = await _dw.DimFaculties.AsNoTracking().ToListAsync(ct);
            var facultyLookup = dimFaculty.ToDictionary(f => f.FacultyId, f => f.FacultyKey);

            var dimStates = await _dw.DimProjectStates.AsNoTracking().ToListAsync(ct);
            var stateLookup = dimStates.ToDictionary(s => s.ProjectStateId, s => s.ProjectStateKey);

            var projects = await _appDb.Projects
                .AsNoTracking()
                .ToListAsync(ct);

            int GetDateKey(DateTime? date)
            {
                if (date == null) return 0;
                var d = date.Value.Date;
                return dimDateLookup.TryGetValue(d, out var key) ? key : 0;
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

                var fact = new FactProject
                {
                    ProjectId = p.ProjectId,
                    ProjectCount = 1,
                    DurationInMonths = p.DurationInMonths,
                    ExecutionPercentage = p.ExecutionPercentage ?? 0m,

                    FacultyKey = facultyKey,
                    ProjectStateKey = stateKey,

                    ApprovalDateKey = GetDateKey(p.ApprovalDate),
                    StartDateKey = GetDateKey(p.StartDate),
                    EndDateKey = GetDateKey(p.RealEndDate)
                };

                _dw.FactProjects.Add(fact);
            }

            await _dw.SaveChangesAsync(ct);
        }

        private async Task LoadFactBudgetAsync(CancellationToken ct)
        {
            _logger.LogInformation("DW ETL - Loading FactBudget...");

            _dw.FactBudgets.RemoveRange(_dw.FactBudgets);
            await _dw.SaveChangesAsync(ct);

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

            _dw.FactProducts.RemoveRange(_dw.FactProducts);
            await _dw.SaveChangesAsync(ct);

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
                    .ThenInclude(v => v.AttributeDefinition)
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

                // ===== BASE DE DATOS =====
                var dbAttr = p.Values?
                    .FirstOrDefault(v =>
                        v.AttributeDefinition != null &&
                        v.AttributeDefinition.AttributeName == "BASE DE DATOS");

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

                // ===== CUARTIL =====
                var quartAttr = p.Values?
                    .FirstOrDefault(v =>
                        v.AttributeDefinition != null &&
                        v.AttributeDefinition.AttributeName == "CUARTIL");

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