using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.TransactionTypes.Request;
using tesisproject.shared.DTOs.Catalog.TransactionTypes.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class TransactionTypeService : ITransactionTypeService
    {
        private readonly IUnitOfWork _uow;

        public TransactionTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<TransactionTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.TransactionTypes.ListAsync(
                onlyActives: onlyActives,
                ct: ct);

            var dto = items.Select(x => new TransactionTypeListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<TransactionTypeListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<TransactionTypeListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<TransactionTypeListItemDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.TransactionTypes.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<TransactionTypeListItemDTO>.Fail("TransactionType not found.", ErrorType.NotFound);

            var dto = new TransactionTypeListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<TransactionTypeListItemDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.TransactionTypes.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<TransactionTypeListItemDTO>> CreateAsync(
            AddTransactionTypeRequestDTO request,
            CancellationToken ct = default)
        {
            var name = (request?.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<TransactionTypeListItemDTO>.Fail("Name is required.", ErrorType.Validation);

            // Evitar duplicado por Name
            var exists = await _uow.TransactionTypes.NameExistsAsync(name, excludeId: null, ct);
            if (exists)
                return ServiceResult<TransactionTypeListItemDTO>.Fail("Name already exists.", ErrorType.Validation);

            var entity = new TransactionType
            {
                Name = name,
                IsActive = true
            };

            await _uow.TransactionTypes.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new TransactionTypeListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<TransactionTypeListItemDTO>.Ok(dto);
        }

        public async Task<ServiceResult<TransactionTypeListItemDTO>> UpdateAsync(
            UpdateTransactionTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<TransactionTypeListItemDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<TransactionTypeListItemDTO>.Fail("Name is required.", ErrorType.Validation);

            var entity = await _uow.TransactionTypes.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<TransactionTypeListItemDTO>.Fail("TransactionType not found.", ErrorType.NotFound);

            // Validar duplicado por Name excluyendo el propio Id
            var duplicated = await _uow.TransactionTypes.NameExistsAsync(name, excludeId: request.Id, ct);
            if (duplicated)
                return ServiceResult<TransactionTypeListItemDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.IsActive = request.IsActive;

            _uow.TransactionTypes.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new TransactionTypeListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<TransactionTypeListItemDTO>.Ok(dto);
        }
    }
}