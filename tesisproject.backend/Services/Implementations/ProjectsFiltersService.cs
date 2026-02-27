using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class ProjectsFiltersService : IProjectsFiltersService
    {
        private readonly ICatalogQueryService _catalogs;
        private readonly IExternalAcademicsService _extTypes;
        private readonly ICatalogRepository<ResearchCategoryType> _researchTypes;

        public ProjectsFiltersService(
            ICatalogQueryService catalogs,
            IExternalAcademicsService externalAcademicsService,
            ICatalogRepository<ResearchCategoryType> researchTypes)
        {
            _catalogs = catalogs;
            _extTypes = externalAcademicsService;
            _researchTypes = researchTypes;
        }

        public async Task<ServiceResult<ProjectsFilterBootstrapDTO>> GetBootstrapAsync(
            CancellationToken ct = default)
        {
            try
            {
                var dto = new ProjectsFilterBootstrapDTO();

                // =======================
                // ProjectStates
                // =======================
                var statesRes = await _catalogs.GetKeyValuesAsync<ProjectState>(ct: ct);
                dto.ProjectStates = GetDataOrEmpty(statesRes, static () => new List<KeyValueItemDTO>());

                // =======================
                // ProjectTypes
                // =======================
                var typesRes = await _catalogs.GetKeyValuesAsync<ProjectType>(ct: ct);
                dto.ProjectTypes = GetDataOrEmpty(typesRes, static () => new List<KeyValueItemDTO>());

                // =======================
                // Faculties (API externa)
                // =======================
                var facRes = await _extTypes.GetFacultiesKeyValuesAsync(ct);
                dto.Faculties = GetDataOrEmpty(facRes, static () => new List<KeyValueItemDTO>());

                // =======================
                // Funding
                // =======================
                var fundingRes = await _catalogs.GetKeyValuesAsync<FundingType>(ct: ct);
                dto.Funding = GetDataOrEmpty(fundingRes, static () => new List<KeyValueItemDTO>());

                // =======================
                // ResearchCategoryTypes dinámicos
                // =======================
                dto.ResearchCategoryTypes = await BuildResearchCategoryTypesAsync(ct);

                return ServiceResult<ProjectsFilterBootstrapDTO>
                    .Ok(dto, "Projects filters bootstrap generated.");
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<ProjectsFilterBootstrapDTO>
                    .Fail("Operation was canceled.", ErrorType.Unexpected);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectsFilterBootstrapDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        private static TCollection GetDataOrEmpty<TCollection>(
            ServiceResult<TCollection> result,
            Func<TCollection> emptyFactory)
            where TCollection : class
        {
            return result.Success && result.Data is not null
                ? result.Data
                : emptyFactory();
        }

        private async Task<List<ResearchCategoryFilterTypeDTO>> BuildResearchCategoryTypesAsync(
            CancellationToken ct)
        {
            var typesQuery = _researchTypes.Query()
                .Where(t => t.IsActive && t.IsFilterEnabled)
                .Include(t => t.ResearchCategoryGroup)   // 👈 para traer el nombre del grupo
                .Include(t => t.ResearchCategories);

            var types = await typesQuery.ToListAsync(ct);

            if (types is null || types.Count == 0)
                return new List<ResearchCategoryFilterTypeDTO>();

            return types
                // solo tipos que tengan al menos una categoría activa
                .Where(t => t.ResearchCategories.Any(c => c.IsActive))
                .OrderBy(t => t.ResearchCategoryGroupId)
                .ThenBy(t => t.Id)
                .Select(t => new ResearchCategoryFilterTypeDTO
                {
                    Id = t.Id,
                    Name = t.Name,

                    // 👇 nuevos campos para agrupar en el front
                    ResearchCategoryGroupId = t.ResearchCategoryGroupId,
                    ResearchCategoryGroupName = t.ResearchCategoryGroup.Name,

                    Categories = t.ResearchCategories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .Select(c => new ResearchCategoryItemDTO
                        {
                            Id = c.Id,
                            Name = c.Name,
                            ParentCategoryId = c.ParentCategoryId,
                            ResearchCategoryTypeId = c.ResearchCategoryTypeId
                        })
                        .ToList()
                })
                .ToList();
        }
    }
}