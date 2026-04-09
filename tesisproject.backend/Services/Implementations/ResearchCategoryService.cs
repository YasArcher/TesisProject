using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ResearchCategoryService : IResearchCategoryService
    {
        private readonly IUnitOfWork _uow;

        public ResearchCategoryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<ResearchCategoryListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            IQueryable<ResearchCategory> query = _uow.ResearchCategories
                .Query()
                .Include(x => x.ResearchCategoryType)
                .Include(x => x.ParentCategory);

            if (onlyActives)
                query = query.Where(x => x.IsActive);

            var items = await query.ToListAsync(ct);

            var dto = items
                .Select(ToListItemDto)
                .ToList();

            return ServiceResult<IReadOnlyList<ResearchCategoryListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<List<ResearchCategoryTreeItemDTO>>> GetTreeAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            IQueryable<ResearchCategory> query = _uow.ResearchCategories
                .Query()
                .Include(x => x.ResearchCategoryType);

            if (onlyActives)
                query = query.Where(x => x.IsActive);

            var items = await query.ToListAsync(ct);

            var roots = BuildTree(items);

            return ServiceResult<List<ResearchCategoryTreeItemDTO>>.Ok(roots);
        }

        public async Task<ServiceResult<ResearchCategoryDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
            {
                return ValidationFailure<ResearchCategoryDetailDTO>(
                    ErrorMessages.ResearchCategory.InvalidId,
                    ErrorCodes.ResearchCategory.InvalidId,
                    nameof(ResearchCategoryDetailDTO.Id));
            }

            var entity = await QueryWithTypeAndParent()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null)
            {
                return ServiceResult<ResearchCategoryDetailDTO>.Fail(
                    ErrorMessages.ResearchCategory.NotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ResearchCategory.NotFound);
            }

            var dto = ToDetailDto(entity);

            return ServiceResult<ResearchCategoryDetailDTO>.Ok(dto);
        }

        // ================= WRITES =================

        public async Task<ServiceResult<ResearchCategoryDetailDTO>> CreateAsync(
            AddResearchCategoryRequestDTO request,
            CancellationToken ct = default)
        {
            var name = NormalizeName(request?.Name);

            if (string.IsNullOrWhiteSpace(name))
            {
                return ValidationFailure<ResearchCategoryDetailDTO>(
                    ErrorMessages.Common.NameRequired,
                    ErrorCodes.Common.NameRequired,
                    nameof(AddResearchCategoryRequestDTO.Name));
            }

            if (request!.ResearchCategoryTypeId <= 0)
            {
                return ValidationFailure<ResearchCategoryDetailDTO>(
                    ErrorMessages.ResearchCategory.TypeIdRequired,
                    ErrorCodes.ResearchCategory.TypeIdRequired,
                    nameof(AddResearchCategoryRequestDTO.ResearchCategoryTypeId));
            }

            // Validar duplicado por nombre (catálogo)
            var duplicated = await _uow.ResearchCategories.ExistsAsync(
                x => x.Name == name,
                ct);

            if (duplicated)
            {
                return ValidationFailure<ResearchCategoryDetailDTO>(
                    ErrorMessages.Common.NameAlreadyExists,
                    ErrorCodes.Common.NameAlreadyExists,
                    nameof(AddResearchCategoryRequestDTO.Name));
            }

            // Validar parent (si viene)
            if (request.ParentCategoryId.HasValue)
            {
                var parentExists = await EnsureParentExistsAsync(
                    request.ParentCategoryId.Value,
                    ct);

                if (!parentExists)
                {
                    return ValidationFailure<ResearchCategoryDetailDTO>(
                        ErrorMessages.ResearchCategory.ParentNotFound,
                        ErrorCodes.ResearchCategory.ParentNotFound,
                        nameof(AddResearchCategoryRequestDTO.ParentCategoryId));
                }
            }

            var entity = new ResearchCategory
            {
                Name = name,
                IsActive = request.IsActive,
                ResearchCategoryTypeId = request.ResearchCategoryTypeId,
                ParentCategoryId = request.ParentCategoryId
            };

            await _uow.ResearchCategories.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // Recargar con includes mínimos para devolver nombres
            var created = await QueryWithTypeAndParent()
                .FirstAsync(x => x.Id == entity.Id, ct);

            var dto = ToDetailDto(created);

            return ServiceResult<ResearchCategoryDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ResearchCategoryDetailDTO>> UpdateAsync(
            UpdateResearchCategoryRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
            {
                return ValidationFailure<ResearchCategoryDetailDTO>(
                    ErrorMessages.ResearchCategory.InvalidId,
                    ErrorCodes.ResearchCategory.InvalidId,
                    nameof(UpdateResearchCategoryRequestDTO.Id));
            }

            var name = NormalizeName(request.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                return ValidationFailure<ResearchCategoryDetailDTO>(
                    ErrorMessages.Common.NameRequired,
                    ErrorCodes.Common.NameRequired,
                    nameof(UpdateResearchCategoryRequestDTO.Name));
            }

            if (request.ResearchCategoryTypeId <= 0)
            {
                return ValidationFailure<ResearchCategoryDetailDTO>(
                    ErrorMessages.ResearchCategory.TypeIdRequired,
                    ErrorCodes.ResearchCategory.TypeIdRequired,
                    nameof(UpdateResearchCategoryRequestDTO.ResearchCategoryTypeId));
            }

            var entity = await _uow.ResearchCategories
                .Query(false) // tracking
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            if (entity is null)
            {
                return ServiceResult<ResearchCategoryDetailDTO>.Fail(
                    ErrorMessages.ResearchCategory.NotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ResearchCategory.NotFound);
            }

            // Validar nombre duplicado excluyendo el propio Id
            var duplicated = await _uow.ResearchCategories.ExistsAsync(
                x => x.Name == name && x.Id != request.Id,
                ct);

            if (duplicated)
            {
                return ValidationFailure<ResearchCategoryDetailDTO>(
                    ErrorMessages.Common.NameAlreadyExists,
                    ErrorCodes.Common.NameAlreadyExists,
                    nameof(UpdateResearchCategoryRequestDTO.Name));
            }

            // Evitar parent = mismo Id
            if (request.ParentCategoryId.HasValue &&
                request.ParentCategoryId.Value == request.Id)
            {
                return ValidationFailure<ResearchCategoryDetailDTO>(
                    ErrorMessages.ResearchCategory.ParentCannotBeSameAsId,
                    ErrorCodes.ResearchCategory.ParentCannotBeSameAsId,
                    nameof(UpdateResearchCategoryRequestDTO.ParentCategoryId));
            }

            // Validar parent si viene
            if (request.ParentCategoryId.HasValue)
            {
                var parentExists = await EnsureParentExistsAsync(
                    request.ParentCategoryId.Value,
                    ct);

                if (!parentExists)
                {
                    return ValidationFailure<ResearchCategoryDetailDTO>(
                        ErrorMessages.ResearchCategory.ParentNotFound,
                        ErrorCodes.ResearchCategory.ParentNotFound,
                        nameof(UpdateResearchCategoryRequestDTO.ParentCategoryId));
                }
            }

            entity.Name = name;
            entity.IsActive = request.IsActive;
            entity.ResearchCategoryTypeId = request.ResearchCategoryTypeId;
            entity.ParentCategoryId = request.ParentCategoryId;

            _uow.ResearchCategories.Update(entity);
            await _uow.SaveChangesAsync(ct);

            // Recargar con includes
            var updated = await QueryWithTypeAndParent()
                .FirstAsync(x => x.Id == entity.Id, ct);

            var dto = ToDetailDto(updated);

            return ServiceResult<ResearchCategoryDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
            {
                return ValidationFailure<NoContent>(
                    ErrorMessages.ResearchCategory.InvalidId,
                    ErrorCodes.ResearchCategory.InvalidId,
                    "Id");
            }

            var entity = await _uow.ResearchCategories
                .GetByIdAsync(new object[] { id }, ct);

            if (entity is null)
            {
                return ServiceResult<NoContent>.Fail(
                    ErrorMessages.ResearchCategory.NotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ResearchCategory.NotFound);
            }

            _uow.ResearchCategories.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        private static ServiceResult<T> ValidationFailure<T>(
            string message,
            string errorCode,
            params string[] fields)
        {
            Dictionary<string, string[]>? validation = null;

            if (fields is { Length: > 0 })
            {
                validation = fields
                    .Distinct(StringComparer.Ordinal)
                    .ToDictionary(
                        field => field,
                        _ => new[] { message },
                        StringComparer.Ordinal);
            }

            return ServiceResult<T>.Fail(
                message,
                ErrorType.Validation,
                errorCode,
                validation);
        }

        private IQueryable<ResearchCategory> QueryWithTypeAndParent()
        {
            return _uow.ResearchCategories
                .Query()
                .Include(x => x.ResearchCategoryType)
                .Include(x => x.ParentCategory);
        }

        private static ResearchCategoryListItemDTO ToListItemDto(ResearchCategory x)
        {
            return new ResearchCategoryListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                ResearchCategoryTypeId = x.ResearchCategoryTypeId,
                ResearchCategoryTypeName = x.ResearchCategoryType.Name,
                ParentCategoryId = x.ParentCategoryId,
                ParentCategoryName = x.ParentCategory != null
                    ? x.ParentCategory.Name
                    : null
            };
        }

        private static ResearchCategoryDetailDTO ToDetailDto(ResearchCategory entity)
        {
            return new ResearchCategoryDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                ResearchCategoryTypeId = entity.ResearchCategoryTypeId,
                ResearchCategoryTypeName = entity.ResearchCategoryType.Name,
                ParentCategoryId = entity.ParentCategoryId,
                ParentCategoryName = entity.ParentCategory?.Name
            };
        }

        private static string NormalizeName(string? name)
        {
            return (name ?? string.Empty).Trim();
        }

        private Task<bool> EnsureParentExistsAsync(int parentId, CancellationToken ct)
        {
            return _uow.ResearchCategories.ExistsAsync(
                x => x.Id == parentId,
                ct);
        }

        private static List<ResearchCategoryTreeItemDTO> BuildTree(List<ResearchCategory> items)
        {
            // Mapa de nodos
            var map = items.ToDictionary(
                x => x.Id,
                x => new ResearchCategoryTreeItemDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = x.IsActive,
                    ResearchCategoryTypeId = x.ResearchCategoryTypeId,
                    ResearchCategoryTypeName = x.ResearchCategoryType.Name,
                    ParentCategoryId = x.ParentCategoryId,
                    Children = new List<ResearchCategoryTreeItemDTO>()
                });

            var roots = new List<ResearchCategoryTreeItemDTO>();

            foreach (var entity in items)
            {
                var node = map[entity.Id];

                if (entity.ParentCategoryId.HasValue &&
                    map.TryGetValue(entity.ParentCategoryId.Value, out var parentNode))
                {
                    parentNode.Children.Add(node);
                }
                else
                {
                    // Sin padre => raíz del árbol (Dominio, etc.)
                    roots.Add(node);
                }
            }

            return roots;
        }
    }
}