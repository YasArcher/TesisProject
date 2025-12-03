using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.Country.Request;
using tesisproject.shared.DTOs.Catalog.Country.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class CountryService : ICountryService
    {
        private readonly IUnitOfWork _uow;

        public CountryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<CountryListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // Usa el ICatalogRepository<Country>
            var items = await _uow.Countries.ListAsync(
                onlyActives: onlyActives,
                where: null,
                include: null,
                ct: ct);

            var dto = items.Select(x => new CountryListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsoCode = x.IsoCode,
                IsoAlpha3 = x.IsoAlpha3,
                IsActive = x.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<CountryListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<CountryDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<CountryDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.Countries.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<CountryDetailDTO>.Fail("Country not found.", ErrorType.NotFound);

            var dto = new CountryDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsoCode = entity.IsoCode,
                IsoAlpha3 = entity.IsoAlpha3,
                IsActive = entity.IsActive
            };

            return ServiceResult<CountryDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            // Usa GetKeyValuesAsync del ICatalogRepository
            var list = await _uow.Countries.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<CountryDetailDTO>> CreateAsync(
            AddCountryRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<CountryDetailDTO>.Fail("Invalid request.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            var isoCode = (request.IsoCode ?? string.Empty).Trim().ToUpper();
            var isoAlpha3 = (request.IsoAlpha3 ?? string.Empty).Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<CountryDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(isoCode) || isoCode.Length != 2)
                return ServiceResult<CountryDetailDTO>.Fail("IsoCode must have 2 characters.", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(isoAlpha3) || isoAlpha3.Length != 3)
                return ServiceResult<CountryDetailDTO>.Fail("IsoAlpha3 must have 3 characters.", ErrorType.Validation);

            // Reutiliza NameExistsAsync del ICatalogRepository
            var nameExists = await _uow.Countries.NameExistsAsync(name, excludeId: null, ct);
            if (nameExists)
                return ServiceResult<CountryDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            var entity = new Country
            {
                Name = name,
                IsoCode = isoCode,
                IsoAlpha3 = isoAlpha3,
                IsActive = true
            };

            await _uow.Countries.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new CountryDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsoCode = entity.IsoCode,
                IsoAlpha3 = entity.IsoAlpha3,
                IsActive = entity.IsActive
            };

            return ServiceResult<CountryDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<CountryDetailDTO>> UpdateAsync(
            UpdateCountryRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<CountryDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            var isoCode = (request.IsoCode ?? string.Empty).Trim().ToUpper();
            var isoAlpha3 = (request.IsoAlpha3 ?? string.Empty).Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<CountryDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(isoCode) || isoCode.Length != 2)
                return ServiceResult<CountryDetailDTO>.Fail("IsoCode must have 2 characters.", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(isoAlpha3) || isoAlpha3.Length != 3)
                return ServiceResult<CountryDetailDTO>.Fail("IsoAlpha3 must have 3 characters.", ErrorType.Validation);

            var entity = await _uow.Countries.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<CountryDetailDTO>.Fail("Country not found.", ErrorType.NotFound);

            var nameExists = await _uow.Countries.NameExistsAsync(name, excludeId: request.Id, ct);
            if (nameExists)
                return ServiceResult<CountryDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.IsoCode = isoCode;
            entity.IsoAlpha3 = isoAlpha3;
            entity.IsActive = request.IsActive;

            _uow.Countries.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new CountryDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsoCode = entity.IsoCode,
                IsoAlpha3 = entity.IsoAlpha3,
                IsActive = entity.IsActive
            };

            return ServiceResult<CountryDetailDTO>.Ok(dto);
        }
    }
}