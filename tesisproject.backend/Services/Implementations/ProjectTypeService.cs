using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProjectType.Request;
using tesisproject.shared.DTOs.Catalog.ProjectType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectTypeService : IProjectTypeService
    {
        private readonly IUnitOfWork _uow;

        public ProjectTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<ProjectTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.ProjectTypeRepository.ListAsync(
                onlyActives: onlyActives,
                ct: ct);

            var dto = items.Select(x => new ProjectTypeListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<ProjectTypeListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ProjectTypeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ProjectTypeDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProjectTypeRepository.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<ProjectTypeDetailDTO>.Fail("ProjectType not found.", ErrorType.NotFound);

            var dto = new ProjectTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<ProjectTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.ProjectTypeRepository.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<ProjectTypeDetailDTO>> CreateAsync(
            AddProjectTypeRequestDTO request,
            CancellationToken ct = default)
        {
            var name = (request?.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ProjectTypeDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            // Evitar duplicados por Name en catálogos
            var exists = await _uow.ProjectTypeRepository.NameExistsAsync(name, excludeId: null, ct);
            if (exists)
                return ServiceResult<ProjectTypeDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            var entity = new ProjectType
            {
                Name = name,
                IsActive = true
            };

            await _uow.ProjectTypeRepository.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new ProjectTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<ProjectTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ProjectTypeDetailDTO>> UpdateAsync(
            UpdateProjectTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ProjectTypeDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ProjectTypeDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            var entity = await _uow.ProjectTypeRepository.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<ProjectTypeDetailDTO>.Fail("ProjectType not found.", ErrorType.NotFound);

            // Validar duplicado por Name, excluyendo el propio Id
            var duplicated = await _uow.ProjectTypeRepository.NameExistsAsync(name, excludeId: request.Id, ct);
            if (duplicated)
                return ServiceResult<ProjectTypeDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.IsActive = request.IsActive;

            _uow.ProjectTypeRepository.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new ProjectTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<ProjectTypeDetailDTO>.Ok(dto);
        }
    }
}