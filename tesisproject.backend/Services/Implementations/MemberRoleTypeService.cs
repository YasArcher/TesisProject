using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.MemberRoleType.Request;
using tesisproject.shared.DTOs.Catalog.MemberRoleType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class MemberRoleTypeService : IMemberRoleTypeService
    {
        private const string InvalidIdMessage = "Invalid id.";
        private const string NotFoundMessage = "MemberRoleType not found.";
        private const string NameRequiredMessage = "Name is required.";
        private const string NameAlreadyExistsMessage = "Name already exists.";

        private readonly IUnitOfWork _uow;

        public MemberRoleTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<MemberRoleTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.MemberRoleTypeRepository.ListAsync(
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
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(InvalidIdMessage, ErrorType.Validation);

            var entity = await _uow.MemberRoleTypeRepository.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(NotFoundMessage, ErrorType.NotFound);

            var dto = ToDetailDto(entity);
            return ServiceResult<MemberRoleTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.MemberRoleTypeRepository.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<MemberRoleTypeDetailDTO>> CreateAsync(
            AddMemberRoleTypeRequestDTO request,
            CancellationToken ct = default)
        {
            var name = NormalizeName(request?.Name);

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(NameRequiredMessage, ErrorType.Validation);

            // En catálogos: evitamos duplicados por Name.
            var exists = await _uow.MemberRoleTypeRepository.NameExistsAsync(name, excludeId: null, ct);
            if (exists)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(NameAlreadyExistsMessage, ErrorType.Validation);

            var entity = new MemberRoleType
            {
                Name = name,
                IsActive = true,
                Flag = request.Flag
            };

            await _uow.MemberRoleTypeRepository.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = ToDetailDto(entity);
            return ServiceResult<MemberRoleTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<MemberRoleTypeDetailDTO>> UpdateAsync(
            UpdateMemberRoleTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(InvalidIdMessage, ErrorType.Validation);

            var name = NormalizeName(request.Name);
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(NameRequiredMessage, ErrorType.Validation);

            var entity = await _uow.MemberRoleTypeRepository.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(NotFoundMessage, ErrorType.NotFound);

            // Validar duplicado por Name, excluyendo el propio Id
            var duplicated = await _uow.MemberRoleTypeRepository.NameExistsAsync(name, excludeId: request.Id, ct);
            if (duplicated)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail(NameAlreadyExistsMessage, ErrorType.Validation);

            entity.Name = name;
            entity.IsActive = request.IsActive;
            entity.Flag = request.Flag;

            _uow.MemberRoleTypeRepository.Update(entity);
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