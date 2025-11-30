using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.FundingType.Request;
using tesisproject.shared.DTOs.Catalog.FundingType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class FundingTypeService : IFundingTypeService
    {
        private readonly IUnitOfWork _uow;

        public FundingTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<FundingTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.FundingTypes.ListAsync(
                onlyActives: onlyActives,
                ct: ct);

            var dto = items.Select(x => new FundingTypeListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<FundingTypeListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<FundingTypeListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<FundingTypeListItemDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.FundingTypes.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<FundingTypeListItemDTO>.Fail("FundingType not found.", ErrorType.NotFound);

            var dto = new FundingTypeListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<FundingTypeListItemDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.FundingTypes.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<FundingTypeListItemDTO>> CreateAsync(
            AddFundingTypeRequestDTO request,
            CancellationToken ct = default)
        {
            var name = (request?.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<FundingTypeListItemDTO>.Fail("Name is required.", ErrorType.Validation);

            // Evitar duplicado por Name
            var exists = await _uow.FundingTypes.NameExistsAsync(name, excludeId: null, ct);
            if (exists)
                return ServiceResult<FundingTypeListItemDTO>.Fail("Name already exists.", ErrorType.Validation);

            var entity = new FundingType
            {
                Name = name,
                IsActive = true
            };

            await _uow.FundingTypes.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new FundingTypeListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<FundingTypeListItemDTO>.Ok(dto);
        }

        public async Task<ServiceResult<FundingTypeListItemDTO>> UpdateAsync(
            UpdateFundingTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<FundingTypeListItemDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<FundingTypeListItemDTO>.Fail("Name is required.", ErrorType.Validation);

            var entity = await _uow.FundingTypes.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<FundingTypeListItemDTO>.Fail("FundingType not found.", ErrorType.NotFound);

            // Validar duplicado por Name excluyendo el propio Id
            var duplicated = await _uow.FundingTypes.NameExistsAsync(name, excludeId: request.Id, ct);
            if (duplicated)
                return ServiceResult<FundingTypeListItemDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.IsActive = request.IsActive;

            _uow.FundingTypes.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new FundingTypeListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<FundingTypeListItemDTO>.Ok(dto);
        }
    }
}