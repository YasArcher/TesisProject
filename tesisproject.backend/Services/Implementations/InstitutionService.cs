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

        private const string InvalidIdMessage = "Invalid id.";
        private const string InvalidRequestMessage = "Invalid request.";
        private const string NameRequiredMessage = "Name is required.";
        private const string NameAlreadyExistsMessage = "Name already exists.";
        private const string InstitutionNotFoundMessage = "Institution not found.";

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

            var dto = items.Select(ToListItemDTO).ToList();

            return ServiceResult<IReadOnlyList<InstitutionListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<InstitutionDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<InstitutionDetailDTO>.Fail(InvalidIdMessage, ErrorType.Validation);

            // Para detalle, podemos usar GetByIdAsync + rehidratado manual
            var entity = await _uow.Institutions.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<InstitutionDetailDTO>.Fail(InstitutionNotFoundMessage, ErrorType.NotFound);

            // Si necesitas CountryName aquí, puedes cargarlo con Countries.GetByIdAsync
            var countryName = await ResolveCountryNameAsync(entity.CountryId, ct);

            return ServiceResult<InstitutionDetailDTO>.Ok(ToDetailDTO(entity, countryName));
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
                return ServiceResult<InstitutionDetailDTO>.Fail(InvalidRequestMessage, ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<InstitutionDetailDTO>.Fail(NameRequiredMessage, ErrorType.Validation);

            // Valida nombre único usando NameExistsAsync del ICatalogRepository
            var nameExists = await _uow.Institutions.NameExistsAsync(name, excludeId: null, ct);
            if (nameExists)
                return ServiceResult<InstitutionDetailDTO>.Fail(NameAlreadyExistsMessage, ErrorType.Validation);

            var entity = new Institution
            {
                Name = name,
                IsActive = true,
                CountryId = request.CountryId
            };

            await _uow.Institutions.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var countryName = await ResolveCountryNameAsync(entity.CountryId, ct);

            return ServiceResult<InstitutionDetailDTO>.Ok(ToDetailDTO(entity, countryName));
        }

        public async Task<ServiceResult<InstitutionDetailDTO>> UpdateAsync(
            UpdateInstitutionRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<InstitutionDetailDTO>.Fail(InvalidIdMessage, ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<InstitutionDetailDTO>.Fail(NameRequiredMessage, ErrorType.Validation);

            var entity = await _uow.Institutions.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<InstitutionDetailDTO>.Fail(InstitutionNotFoundMessage, ErrorType.NotFound);

            var nameExists = await _uow.Institutions.NameExistsAsync(name, excludeId: request.Id, ct);
            if (nameExists)
                return ServiceResult<InstitutionDetailDTO>.Fail(NameAlreadyExistsMessage, ErrorType.Validation);

            entity.Name = name;
            entity.CountryId = request.CountryId;
            entity.IsActive = request.IsActive;

            _uow.Institutions.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var countryName = await ResolveCountryNameAsync(entity.CountryId, ct);

            return ServiceResult<InstitutionDetailDTO>.Ok(ToDetailDTO(entity, countryName));
        }

        // ================ HELPERS ================

        private static InstitutionListItemDTO ToListItemDTO(Institution entity)
        {
            return new InstitutionListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                CountryId = entity.CountryId,
                CountryName = entity.Country != null ? entity.Country.Name : null
            };
        }

        private static InstitutionDetailDTO ToDetailDTO(Institution entity, string? countryName)
        {
            return new InstitutionDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                CountryId = entity.CountryId,
                CountryName = countryName
            };
        }

        private async Task<string?> ResolveCountryNameAsync(int? countryId, CancellationToken ct)
        {
            if (!countryId.HasValue)
                return null;

            var country = await _uow.Countries.GetByIdAsync(new object[] { countryId.Value }, ct);
            return country?.Name;
        }
    }
}