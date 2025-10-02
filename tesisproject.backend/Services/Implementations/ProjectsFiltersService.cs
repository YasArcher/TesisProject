using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _cache;

        public ProjectsFiltersService(
            ICatalogQueryService catalogs,
            ICatalogRepository<ProjectExtensionType> extTypes,
            IMemoryCache cache)
        {
            _catalogs = catalogs;
            _cache = cache;
        }

        public async Task<ServiceResult<ProjectsFilterBootstrapDTO>> GetBootstrapAsync(
            CancellationToken ct = default)
        {
            try
            {
                var dto = new ProjectsFilterBootstrapDTO();

                // states
                dto.ProjectStates = await _cache.GetOrCreateAsync(
                    "filters:projects:states",
                    async entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);

                        var res = await _catalogs.GetKeyValuesAsync<ProjectState>(ct: ct);
                        return res.Success && res.Data is not null
                            ? res.Data
                            : new List<KeyValueItemDTO>();
                    }
                ) ?? new List<KeyValueItemDTO>();

                // types
                dto.ProjectTypes = await _cache.GetOrCreateAsync(
                    "filters:projects:types",
                    async entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);

                        var res = await _catalogs.GetKeyValuesAsync<ProjectType>(ct: ct);
                        return res.Success && res.Data is not null
                            ? res.Data
                            : new List<KeyValueItemDTO>();
                    }
                ) ?? new List<KeyValueItemDTO>();

                // extensionTypes (si lo necesitas, activa este bloque)
                /*
                dto.ExtensionTypes = await _cache.GetOrCreateAsync(
                    "filters:projects:extTypes",
                    async entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);

                        var list = await _extTypes.Query()
                            .OrderBy(x => x.Name)
                            .Select(x => new ProjectExtensionTypeItemDTO
                            {
                                Id = x.Id,
                                Name = x.Name,
                                IsBudgetExecutable = x.IsBudgetExecutable
                            })
                            .ToListAsync(ct);

                        return list ?? new List<ProjectExtensionTypeItemDTO>();
                    }
                ) ?? new List<ProjectExtensionTypeItemDTO>();
                */

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
    }
}
