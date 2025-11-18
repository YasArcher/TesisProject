using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.ObjectiveType.Request;
using tesisproject.shared.DTOs.Catalog.ObjectiveType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ObjectiveTypeService : IObjectiveTypeService
    {
        private readonly IUnitOfWork _uow;

        public ObjectiveTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<ObjectiveTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.ObjectiveTypes.ListAsync(
                onlyActives: onlyActives,
                ct: ct);

            var dto = items.Select(x => new ObjectiveTypeListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<ObjectiveTypeListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ObjectiveTypeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ObjectiveTypeDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ObjectiveTypes.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<ObjectiveTypeDetailDTO>.Fail("ObjectiveType not found.", ErrorType.NotFound);

            var dto = new ObjectiveTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<ObjectiveTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.ObjectiveTypes.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<ObjectiveTypeDetailDTO>> CreateAsync(
            AddObjectiveTypeRequestDTO request,
            CancellationToken ct = default)
        {
            var name = (request?.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ObjectiveTypeDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            // En catálogos: evitamos duplicados por Name.
            var exists = await _uow.ObjectiveTypes.NameExistsAsync(name, excludeId: null, ct);
            if (exists)
                return ServiceResult<ObjectiveTypeDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            var entity = new ObjectiveType
            {
                Name = name,
                IsActive = true
            };

            await _uow.ObjectiveTypes.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new ObjectiveTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<ObjectiveTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ObjectiveTypeDetailDTO>> UpdateAsync(
            UpdateObjectiveTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ObjectiveTypeDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ObjectiveTypeDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            var entity = await _uow.ObjectiveTypes.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<ObjectiveTypeDetailDTO>.Fail("ObjectiveType not found.", ErrorType.NotFound);

            // Validar duplicado por Name, excluyendo el propio Id
            var duplicated = await _uow.ObjectiveTypes.NameExistsAsync(name, excludeId: request.Id, ct);
            if (duplicated)
                return ServiceResult<ObjectiveTypeDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.IsActive = request.IsActive;

            _uow.ObjectiveTypes.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new ObjectiveTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<ObjectiveTypeDetailDTO>.Ok(dto);
        }
    }
}