using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Request;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class IndexingSourceService : IIndexingSourceService
    {
        private readonly IUnitOfWork _uow;

        public IndexingSourceService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<IndexingSourceListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.IndexingSources.ListAsync(
                onlyActives: onlyActives,
                where: null,
                include: null,
                ct: ct);

            var dto = items.Select(x => new IndexingSourceListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                Abbreviation = x.Abbreviation,
                ReferenceUrl = x.ReferenceUrl,
                IsActive = x.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<IndexingSourceListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<IndexingSourceListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<IndexingSourceListItemDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.IndexingSources.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<IndexingSourceListItemDTO>.Fail("Indexing source not found.", ErrorType.NotFound);

            var dto = new IndexingSourceListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                Abbreviation = entity.Abbreviation,
                ReferenceUrl = entity.ReferenceUrl,
                IsActive = entity.IsActive
            };

            return ServiceResult<IndexingSourceListItemDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.IndexingSources.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<IndexingSourceListItemDTO>> CreateAsync(
            IndexingSourceCreateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<IndexingSourceListItemDTO>.Fail("Invalid request.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            var abbr = string.IsNullOrWhiteSpace(request.Abbreviation)
                ? null
                : request.Abbreviation!.Trim();
            var url = string.IsNullOrWhiteSpace(request.ReferenceUrl)
                ? null
                : request.ReferenceUrl!.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<IndexingSourceListItemDTO>.Fail("Name is required.", ErrorType.Validation);

            var nameExists = await _uow.IndexingSources.NameExistsAsync(name, excludeId: null, ct);
            if (nameExists)
                return ServiceResult<IndexingSourceListItemDTO>.Fail("Name already exists.", ErrorType.Validation);

            var entity = new IndexingSource
            {
                Name = name,
                Abbreviation = abbr,
                ReferenceUrl = url,
                IsActive = request.IsActive
            };

            await _uow.IndexingSources.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new IndexingSourceListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                Abbreviation = entity.Abbreviation,
                ReferenceUrl = entity.ReferenceUrl,
                IsActive = entity.IsActive
            };

            return ServiceResult<IndexingSourceListItemDTO>.Ok(dto);
        }

        public async Task<ServiceResult<IndexingSourceListItemDTO>> UpdateAsync(
            IndexingSourceUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<IndexingSourceListItemDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            var abbr = string.IsNullOrWhiteSpace(request.Abbreviation)
                ? null
                : request.Abbreviation!.Trim();
            var url = string.IsNullOrWhiteSpace(request.ReferenceUrl)
                ? null
                : request.ReferenceUrl!.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<IndexingSourceListItemDTO>.Fail("Name is required.", ErrorType.Validation);

            var entity = await _uow.IndexingSources.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<IndexingSourceListItemDTO>.Fail("Indexing source not found.", ErrorType.NotFound);

            var nameExists = await _uow.IndexingSources.NameExistsAsync(name, excludeId: request.Id, ct);
            if (nameExists)
                return ServiceResult<IndexingSourceListItemDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.Abbreviation = abbr;
            entity.ReferenceUrl = url;
            entity.IsActive = request.IsActive;

            _uow.IndexingSources.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new IndexingSourceListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                Abbreviation = entity.Abbreviation,
                ReferenceUrl = entity.ReferenceUrl,
                IsActive = entity.IsActive
            };

            return ServiceResult<IndexingSourceListItemDTO>.Ok(dto);
        }
    }
}