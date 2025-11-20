using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.DocumentType.Request;
using tesisproject.shared.DTOs.Catalog.DocumentType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class DocumentTypeService : IDocumentTypeService
    {
        private readonly IUnitOfWork _uow;

        public DocumentTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<DocumentTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.DocumentTypes.ListAsync(
                onlyActives: onlyActives,
                ct: ct);

            var dto = items.Select(x => new DocumentTypeListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<DocumentTypeListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<DocumentTypeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<DocumentTypeDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.DocumentTypes.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<DocumentTypeDetailDTO>.Fail("DocumentType not found.", ErrorType.NotFound);

            var dto = new DocumentTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<DocumentTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.DocumentTypes.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<DocumentTypeDetailDTO>> CreateAsync(
            AddDocumentTypeRequestDTO request,
            CancellationToken ct = default)
        {
            var name = (request?.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<DocumentTypeDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            var exists = await _uow.DocumentTypes.NameExistsAsync(name, excludeId: null, ct);
            if (exists)
                return ServiceResult<DocumentTypeDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            var entity = new DocumentType
            {
                Name = name,
                IsActive = true
            };

            await _uow.DocumentTypes.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new DocumentTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<DocumentTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<DocumentTypeDetailDTO>> UpdateAsync(
            UpdateDocumentTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<DocumentTypeDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<DocumentTypeDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            var entity = await _uow.DocumentTypes.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<DocumentTypeDetailDTO>.Fail("DocumentType not found.", ErrorType.NotFound);

            var duplicated = await _uow.DocumentTypes.NameExistsAsync(name, excludeId: request.Id, ct);
            if (duplicated)
                return ServiceResult<DocumentTypeDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.IsActive = request.IsActive;

            _uow.DocumentTypes.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new DocumentTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<DocumentTypeDetailDTO>.Ok(dto);
        }
    }
}
