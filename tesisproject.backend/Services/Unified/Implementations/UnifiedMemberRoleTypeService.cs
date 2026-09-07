using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.MemberRoleType.Request;
using tesisproject.shared.DTOs.Catalog.MemberRoleType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedMemberRoleTypeService : IUnifiedMemberRoleTypeService
    {





        private readonly IUnifiedUnitOfWork _uow;

        public UnifiedMemberRoleTypeService(IUnifiedUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<MemberRoleTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.MemberRoleTypes.ListAsync(
                onlyActives: onlyActives,
                ct: ct);

            var dto = items
                .Select(ToListItemDto)
                .ToList();

            return ServiceResult<IReadOnlyList<MemberRoleTypeListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<MemberRoleTypeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = await _uow.MemberRoleTypes.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.MemberRoleTypeService_NotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            var dto = ToDetailDto(entity);
            return ServiceResult<MemberRoleTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.MemberRoleTypes.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<MemberRoleTypeDetailDTO>> CreateAsync(
            AddMemberRoleTypeRequestDTO request,
            CancellationToken ct = default)
        {
            var name = NormalizeName(request?.Name);

            if (request is null || string.IsNullOrWhiteSpace(name))
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            // En catálogos: evitamos duplicados por Name.
            var exists = await _uow.MemberRoleTypes.NameExistsAsync(name, excludeId: null, ct);
            if (exists)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_NameAlreadyExistsMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = new MemberRoleType
            {
                Name = name,
                IsActive = true,
                Flag = request.Flag
            };

            await _uow.MemberRoleTypes.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = ToDetailDto(entity);
            return ServiceResult<MemberRoleTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<MemberRoleTypeDetailDTO>> UpdateAsync(
            UpdateMemberRoleTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var name = NormalizeName(request.Name);
            if (request is null || string.IsNullOrWhiteSpace(name))
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = await _uow.MemberRoleTypes.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.MemberRoleTypeService_NotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            // Validar duplicado por Name, excluyendo el propio Id
            var duplicated = await _uow.MemberRoleTypes.NameExistsAsync(name, excludeId: request.Id, ct);
            if (duplicated)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_NameAlreadyExistsMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            entity.Name = name;
            entity.IsActive = request.IsActive;
            entity.Flag = request.Flag;

            _uow.MemberRoleTypes.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = ToDetailDto(entity);
            return ServiceResult<MemberRoleTypeDetailDTO>.Ok(dto);
        }

        // ================= PRIVATE HELPERS =================

        private static string NormalizeName(string? value)
            => (value ?? string.Empty).Trim();

        private static MemberRoleTypeListItemDTO ToListItemDto(MemberRoleType entity)
            => new MemberRoleTypeListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                Flag = entity.Flag
            };

        private static MemberRoleTypeDetailDTO ToDetailDto(MemberRoleType entity)
            => new MemberRoleTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                Flag = entity.Flag
            };
    }
}