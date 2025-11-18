// tesisproject.backend/Services/Implementations/CatalogQueryService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Base;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class CatalogQueryService : ICatalogQueryService
    {
        private readonly IServiceProvider _sp;
        public CatalogQueryService(IServiceProvider sp) => _sp = sp;

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync<TCatalog>(
            string? term = null, int? take = null, CancellationToken ct = default)
            where TCatalog : CatalogEntityBase
        {
            try
            {
                var repo = _sp.GetRequiredService<ICatalogRepository<TCatalog>>();
                var query = repo.Query(); // AsNoTracking aplicado por el repo
                // 🔹 Filtro por activos
                query = query.Where(x => x.IsActive);

                // 🔹 Filtro por término de búsqueda
                if (!string.IsNullOrWhiteSpace(term))
                    query = query.Where(x => EF.Functions.Like(x.Name, $"%{term}%"));

                // 🔹 Límite de resultados
                if (take.HasValue && take.Value > 0)
                    query = query.Take(take.Value);

                // 🔹 Proyección a DTO
                var list = await query
                    .OrderBy(x => x.Name)
                    .Select(x => new KeyValueItemDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                    })
                    .ToListAsync(ct);

                if (list.Count == 0)
                    return ServiceResult<List<KeyValueItemDTO>>.Fail("No items found.", ErrorType.NotFound);

                return ServiceResult<List<KeyValueItemDTO>>.Ok(list, "Catalog items retrieved.");
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<List<KeyValueItemDTO>>.Fail("Operation was canceled.", ErrorType.Unexpected);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<KeyValueItemDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

    }
}
