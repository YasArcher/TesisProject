using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Request;
using tesisproject.shared.DTOs.Catalog.IndexingSource.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Implementations
{
    public class IndexingSourceService : IIndexingSourceService
    {
        private readonly IUnitOfWork _uow;

        private const string InvalidIdMessage = "Invalid id.";
        private const string InvalidRequestMessage = "Invalid request.";
        private const string NameRequiredMessage = "Name is required.";
        private const string NameAlreadyExistsMessage = "Name already exists.";
        private const string IndexingSourceNotFoundMessage = "Indexing source not found.";

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

            var dto = items.Select(ToListItemDTO).ToList();

            return ServiceResult<IReadOnlyList<IndexingSourceListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<IndexingSourceListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<IndexingSourceListItemDTO>.Fail(InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = await _uow.IndexingSources.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<IndexingSourceListItemDTO>.Fail(IndexingSourceNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            return ServiceResult<IndexingSourceListItemDTO>.Ok(ToListItemDTO(entity));
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
                return ServiceResult<IndexingSourceListItemDTO>.Fail(InvalidRequestMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var name = NormalizeRequired(request.Name);
            var abbr = NormalizeOptional(request.Abbreviation);
            var url = NormalizeOptional(request.ReferenceUrl);

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<IndexingSourceListItemDTO>.Fail(NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var nameExists = await _uow.IndexingSources.NameExistsAsync(name, excludeId: null, ct);
            if (nameExists)
                return ServiceResult<IndexingSourceListItemDTO>.Fail(NameAlreadyExistsMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = new IndexingSource
            {
                Name = name,
                Abbreviation = abbr,
                ReferenceUrl = url,
                IsActive = request.IsActive
            };

            await _uow.IndexingSources.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<IndexingSourceListItemDTO>.Ok(ToListItemDTO(entity));
        }

        public async Task<ServiceResult<IndexingSourceListItemDTO>> UpdateAsync(
            IndexingSourceUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<IndexingSourceListItemDTO>.Fail(InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var name = NormalizeRequired(request.Name);
            var abbr = NormalizeOptional(request.Abbreviation);
            var url = NormalizeOptional(request.ReferenceUrl);

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<IndexingSourceListItemDTO>.Fail(NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = await _uow.IndexingSources.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<IndexingSourceListItemDTO>.Fail(IndexingSourceNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            var nameExists = await _uow.IndexingSources.NameExistsAsync(name, excludeId: request.Id, ct);
            if (nameExists)
                return ServiceResult<IndexingSourceListItemDTO>.Fail(NameAlreadyExistsMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            entity.Name = name;
            entity.Abbreviation = abbr;
            entity.ReferenceUrl = url;
            entity.IsActive = request.IsActive;

            _uow.IndexingSources.Update(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<IndexingSourceListItemDTO>.Ok(ToListItemDTO(entity));
        }

        // ================ HELPERS ================

        private static IndexingSourceListItemDTO ToListItemDTO(IndexingSource entity)
        {
            return new IndexingSourceListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                Abbreviation = entity.Abbreviation,
                ReferenceUrl = entity.ReferenceUrl,
                IsActive = entity.IsActive
            };
        }

        private static string NormalizeRequired(string? value)
            => (value ?? string.Empty).Trim();

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}