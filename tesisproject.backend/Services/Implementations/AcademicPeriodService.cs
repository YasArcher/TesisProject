using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.AcademicPeriod.Request;
using tesisproject.shared.DTOs.Catalog.AcademicPeriod.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class AcademicPeriodService : IAcademicPeriodService
    {
        private readonly IUnitOfWork _uow;

        public AcademicPeriodService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<AcademicPeriodListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.AcademicPeriods.ListAsync(
                onlyActives: onlyActives,
                where: null,
                include: null,
                ct: ct);

            var dto = items.Select(x => new AcademicPeriodListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<AcademicPeriodListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<AcademicPeriodListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<AcademicPeriodListItemDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.AcademicPeriods.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<AcademicPeriodListItemDTO>.Fail("Academic period not found.", ErrorType.NotFound);

            var dto = new AcademicPeriodListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                IsActive = entity.IsActive
            };

            return ServiceResult<AcademicPeriodListItemDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.AcademicPeriods.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<AcademicPeriodListItemDTO>> CreateAsync(
            AcademicPeriodCreateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<AcademicPeriodListItemDTO>.Fail("Invalid request.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<AcademicPeriodListItemDTO>.Fail("Name is required.", ErrorType.Validation);

            if (request.EndDate < request.StartDate)
                return ServiceResult<AcademicPeriodListItemDTO>.Fail(
                    "End date cannot be earlier than start date.",
                    ErrorType.Validation);

            var nameExists = await _uow.AcademicPeriods.NameExistsAsync(name, excludeId: null, ct);
            if (nameExists)
                return ServiceResult<AcademicPeriodListItemDTO>.Fail("Name already exists.", ErrorType.Validation);

            var entity = new AcademicPeriod
            {
                Name = name,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            await _uow.AcademicPeriods.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new AcademicPeriodListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                IsActive = entity.IsActive
            };

            return ServiceResult<AcademicPeriodListItemDTO>.Ok(dto);
        }

        public async Task<ServiceResult<AcademicPeriodListItemDTO>> UpdateAsync(
            AcademicPeriodUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<AcademicPeriodListItemDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<AcademicPeriodListItemDTO>.Fail("Name is required.", ErrorType.Validation);

            if (request.EndDate < request.StartDate)
                return ServiceResult<AcademicPeriodListItemDTO>.Fail(
                    "End date cannot be earlier than start date.",
                    ErrorType.Validation);

            var entity = await _uow.AcademicPeriods.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<AcademicPeriodListItemDTO>.Fail("Academic period not found.", ErrorType.NotFound);

            var nameExists = await _uow.AcademicPeriods.NameExistsAsync(name, excludeId: request.Id, ct);
            if (nameExists)
                return ServiceResult<AcademicPeriodListItemDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.StartDate = request.StartDate;
            entity.EndDate = request.EndDate;
            entity.IsActive = request.IsActive;

            _uow.AcademicPeriods.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new AcademicPeriodListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                IsActive = entity.IsActive
            };

            return ServiceResult<AcademicPeriodListItemDTO>.Ok(dto);
        }
    }
}