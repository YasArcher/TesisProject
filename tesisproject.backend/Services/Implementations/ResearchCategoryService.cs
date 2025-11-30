using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.Entities.Catalogs;
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
                .Select(x => new ResearchCategoryListItemDTO
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
                })
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

            return ServiceResult<List<ResearchCategoryTreeItemDTO>>.Ok(roots);
        }

        public async Task<ServiceResult<ResearchCategoryDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ResearchCategories
                .Query()
                .Include(x => x.ResearchCategoryType)
                .Include(x => x.ParentCategory)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null)
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("ResearchCategory not found.", ErrorType.NotFound);

            var dto = new ResearchCategoryDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                ResearchCategoryTypeId = entity.ResearchCategoryTypeId,
                ResearchCategoryTypeName = entity.ResearchCategoryType.Name,
                ParentCategoryId = entity.ParentCategoryId,
                ParentCategoryName = entity.ParentCategory?.Name
            };

            return ServiceResult<ResearchCategoryDetailDTO>.Ok(dto);
        }

        // ================= WRITES =================

        public async Task<ServiceResult<ResearchCategoryDetailDTO>> CreateAsync(
            AddResearchCategoryRequestDTO request,
            CancellationToken ct = default)
        {
            var name = (request?.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("Name is required.", ErrorType.Validation);

            if (request!.ResearchCategoryTypeId <= 0)
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("ResearchCategoryTypeId is required.", ErrorType.Validation);

            // Validar duplicado por nombre (catálogo)
            var duplicated = await _uow.ResearchCategories.ExistsAsync(
                x => x.Name == name,
                ct);

            if (duplicated)
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("Name already exists.", ErrorType.Validation);

            // Validar parent (si viene)
            if (request.ParentCategoryId.HasValue)
            {
                var parentExists = await _uow.ResearchCategories.ExistsAsync(
                    x => x.Id == request.ParentCategoryId.Value,
                    ct);

                if (!parentExists)
                    return ServiceResult<ResearchCategoryDetailDTO>
                        .Fail("Parent category not found.", ErrorType.Validation);
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
            var created = await _uow.ResearchCategories
                .Query()
                .Include(x => x.ResearchCategoryType)
                .Include(x => x.ParentCategory)
                .FirstAsync(x => x.Id == entity.Id, ct);

            var dto = new ResearchCategoryDetailDTO
            {
                Id = created.Id,
                Name = created.Name,
                IsActive = created.IsActive,
                ResearchCategoryTypeId = created.ResearchCategoryTypeId,
                ResearchCategoryTypeName = created.ResearchCategoryType.Name,
                ParentCategoryId = created.ParentCategoryId,
                ParentCategoryName = created.ParentCategory?.Name
            };

            return ServiceResult<ResearchCategoryDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ResearchCategoryDetailDTO>> UpdateAsync(
            UpdateResearchCategoryRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("Name is required.", ErrorType.Validation);

            if (request.ResearchCategoryTypeId <= 0)
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("ResearchCategoryTypeId is required.", ErrorType.Validation);

            var entity = await _uow.ResearchCategories
                .Query(false) // tracking
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            if (entity is null)
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("ResearchCategory not found.", ErrorType.NotFound);

            // Validar nombre duplicado excluyendo el propio Id
            var duplicated = await _uow.ResearchCategories.ExistsAsync(
                x => x.Name == name && x.Id != request.Id,
                ct);

            if (duplicated)
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("Name already exists.", ErrorType.Validation);

            // Evitar parent = mismo Id
            if (request.ParentCategoryId.HasValue &&
                request.ParentCategoryId.Value == request.Id)
            {
                return ServiceResult<ResearchCategoryDetailDTO>
                    .Fail("ParentCategoryId cannot be the same as Id.", ErrorType.Validation);
            }

            // Validar parent si viene
            if (request.ParentCategoryId.HasValue)
            {
                var parentExists = await _uow.ResearchCategories.ExistsAsync(
                    x => x.Id == request.ParentCategoryId.Value,
                    ct);

                if (!parentExists)
                    return ServiceResult<ResearchCategoryDetailDTO>
                        .Fail("Parent category not found.", ErrorType.Validation);
            }

            entity.Name = name;
            entity.IsActive = request.IsActive;
            entity.ResearchCategoryTypeId = request.ResearchCategoryTypeId;
            entity.ParentCategoryId = request.ParentCategoryId;

            _uow.ResearchCategories.Update(entity);
            await _uow.SaveChangesAsync(ct);

            // Recargar con includes
            var updated = await _uow.ResearchCategories
                .Query()
                .Include(x => x.ResearchCategoryType)
                .Include(x => x.ParentCategory)
                .FirstAsync(x => x.Id == entity.Id, ct);

            var dto = new ResearchCategoryDetailDTO
            {
                Id = updated.Id,
                Name = updated.Name,
                IsActive = updated.IsActive,
                ResearchCategoryTypeId = updated.ResearchCategoryTypeId,
                ResearchCategoryTypeName = updated.ResearchCategoryType.Name,
                ParentCategoryId = updated.ParentCategoryId,
                ParentCategoryName = updated.ParentCategory?.Name
            };

            return ServiceResult<ResearchCategoryDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<NoContent>
                    .Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ResearchCategories
                .GetByIdAsync(new object[] { id }, ct);

            if (entity is null)
                return ServiceResult<NoContent>
                    .Fail("ResearchCategory not found.", ErrorType.NotFound);

            _uow.ResearchCategories.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent());
        }
    }
}