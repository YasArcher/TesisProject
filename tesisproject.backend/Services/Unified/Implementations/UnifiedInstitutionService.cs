using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.DTOs.Institution.Request;
using tesisproject.shared.DTOs.Institution.Response;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedInstitutionService : IUnifiedInstitutionService
    {
        private readonly IUnifiedUnitOfWork _uow;






        public UnifiedInstitutionService(IUnifiedUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<InstitutionListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            // Usamos IUnifiedCatalogRepository<Institution>.ListAsync + include Country
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
                return ServiceResult<InstitutionDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            // Para detalle, podemos usar GetByIdAsync + rehidratado manual
            var entity = await _uow.Institutions.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<InstitutionDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.InstitutionService_InstitutionNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

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
                return ServiceResult<InstitutionDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidRequestMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var name = (request.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<InstitutionDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            // Valida nombre único usando NameExistsAsync del IUnifiedCatalogRepository
            var nameExists = await _uow.Institutions.NameExistsAsync(name, excludeId: null, ct);
            if (nameExists)
                return ServiceResult<InstitutionDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_NameAlreadyExistsMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

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
                return ServiceResult<InstitutionDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var name = (request.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<InstitutionDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = await _uow.Institutions.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<InstitutionDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.InstitutionService_InstitutionNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            var nameExists = await _uow.Institutions.NameExistsAsync(name, excludeId: request.Id, ct);
            if (nameExists)
                return ServiceResult<InstitutionDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_NameAlreadyExistsMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

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