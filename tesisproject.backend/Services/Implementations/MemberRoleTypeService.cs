using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Interfaces;
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

            var dto = items.Select(x => new MemberRoleTypeListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                Flag = x.Flag
            }).ToList();

            return ServiceResult<IReadOnlyList<MemberRoleTypeListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<MemberRoleTypeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.MemberRoleTypeRepository.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail("MemberRoleType not found.", ErrorType.NotFound);

            var dto = new MemberRoleTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                Flag = entity.Flag
            };

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
            var name = (request?.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            // En catálogos: evitamos duplicados por Name.
            var exists = await _uow.MemberRoleTypeRepository.NameExistsAsync(name, excludeId: null, ct);
            if (exists)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            var entity = new MemberRoleType
            {
                Name = name,
                IsActive = true,
                Flag = request.Flag
            };

            await _uow.MemberRoleTypeRepository.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new MemberRoleTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                Flag = entity.Flag
            };

            return ServiceResult<MemberRoleTypeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<MemberRoleTypeDetailDTO>> UpdateAsync(
            UpdateMemberRoleTypeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            var entity = await _uow.MemberRoleTypeRepository.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail("MemberRoleType not found.", ErrorType.NotFound);

            // Validar duplicado por Name, excluyendo el propio Id
            var duplicated = await _uow.MemberRoleTypeRepository.NameExistsAsync(name, excludeId: request.Id, ct);
            if (duplicated)
                return ServiceResult<MemberRoleTypeDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.IsActive = request.IsActive;
            entity.Flag = request.Flag;

            _uow.MemberRoleTypeRepository.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new MemberRoleTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                Flag = entity.Flag
            };

            return ServiceResult<MemberRoleTypeDetailDTO>.Ok(dto);
        }
    }
}