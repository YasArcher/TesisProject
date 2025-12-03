using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.DTOs.Institution.Request;
using tesisproject.shared.DTOs.Institution.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class InstitutionService : IInstitutionService
    {
        private readonly IUnitOfWork _uow;

        public InstitutionService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<InstitutionListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // Usamos ICatalogRepository<Institution>.ListAsync + include Country
            var items = await _uow.Institutions.ListAsync(
                onlyActives: onlyActives,
                where: null,
                include: q => q.Include(i => i.Country),
                ct: ct);

            var dto = items.Select(x => new InstitutionListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                CountryId = x.CountryId,
                CountryName = x.Country != null ? x.Country.Name : null
            }).ToList();

            return ServiceResult<IReadOnlyList<InstitutionListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<InstitutionDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<InstitutionDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            // Para detalle, podemos usar GetByIdAsync + rehidratado manual
            var entity = await _uow.Institutions.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<InstitutionDetailDTO>.Fail("Institution not found.", ErrorType.NotFound);

            // Si necesitas CountryName aquí, puedes cargarlo con Countries.GetByIdAsync
            string? countryName = null;
            if (entity.CountryId.HasValue)
            {
                var country = await _uow.Countries.GetByIdAsync(
                    new object[] { entity.CountryId.Value }, ct);
                countryName = country?.Name;
            }

            var dto = new InstitutionDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                CountryId = entity.CountryId,
                CountryName = countryName
            };

            return ServiceResult<InstitutionDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.Institutions.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<InstitutionDetailDTO>> CreateAsync(
            AddInstitutionRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<InstitutionDetailDTO>.Fail("Invalid request.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<InstitutionDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            // Valida nombre único usando NameExistsAsync del ICatalogRepository
            var nameExists = await _uow.Institutions.NameExistsAsync(name, excludeId: null, ct);
            if (nameExists)
                return ServiceResult<InstitutionDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            var entity = new Institution
            {
                Name = name,
                IsActive = true,
                CountryId = request.CountryId
            };

            await _uow.Institutions.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            string? countryName = null;
            if (entity.CountryId.HasValue)
            {
                var country = await _uow.Countries.GetByIdAsync(
                    new object[] { entity.CountryId.Value }, ct);
                countryName = country?.Name;
            }

            var dto = new InstitutionDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                CountryId = entity.CountryId,
                CountryName = countryName
            };

            return ServiceResult<InstitutionDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<InstitutionDetailDTO>> UpdateAsync(
            UpdateInstitutionRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<InstitutionDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<InstitutionDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            var entity = await _uow.Institutions.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<InstitutionDetailDTO>.Fail("Institution not found.", ErrorType.NotFound);

            var nameExists = await _uow.Institutions.NameExistsAsync(name, excludeId: request.Id, ct);
            if (nameExists)
                return ServiceResult<InstitutionDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.CountryId = request.CountryId;
            entity.IsActive = request.IsActive;

            _uow.Institutions.Update(entity);
            await _uow.SaveChangesAsync(ct);

            string? countryName = null;
            if (entity.CountryId.HasValue)
            {
                var country = await _uow.Countries.GetByIdAsync(
                    new object[] { entity.CountryId.Value }, ct);
                countryName = country?.Name;
            }

            var dto = new InstitutionDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                CountryId = entity.CountryId,
                CountryName = countryName
            };

            return ServiceResult<InstitutionDetailDTO>.Ok(dto);
        }
    }
}