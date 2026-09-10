using tesisproject.backend.UnitOfWork.Unified.Interfaces;
// tesisproject.backend/Services/Implementations/UnifiedCatalogQueryService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Filters;
using tesisproject.backend.Data.UnifiedEntities.Base;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public sealed class UnifiedCatalogQueryService : IUnifiedCatalogQueryService
    {

        private const string MsgCatalogItemsRetrieved = "Catalog items retrieved.";

        private readonly IUnifiedUnitOfWork _uow;

        public UnifiedCatalogQueryService(IUnifiedUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync<TCatalog>(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
            where TCatalog : CatalogEntityBase
        {
            try
            {
                var repo = UnifiedCatalogAccess.Get<TCatalog>(_uow);
                var query = repo.Query();

                query = query.Where(x => x.IsActive);

                if (!string.IsNullOrWhiteSpace(term))
                    query = query.Where(x => EF.Functions.Like(x.Name, $"%{term}%"));

                if (take.HasValue && take.Value > 0)
                    query = query.Take(take.Value);

                var list = await query
                    .OrderBy(x => x.Name)
                    .Select(x => new KeyValueItemDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                    })
                    .ToListAsync(ct);

                if (list.Count == 0)
                {
                    return ServiceResult<List<KeyValueItemDTO>>.Fail(
                        ErrorMessages.UnifiedLegacy.CatalogQueryService_MsgNoItemsFound,
                        ErrorType.NotFound,
                        ErrorCodes.CatalogQuery.NoItemsFound);
                }

                return ServiceResult<List<KeyValueItemDTO>>.Ok(
                    list,
                    MsgCatalogItemsRetrieved);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<List<KeyValueItemDTO>>.Fail(
                    ErrorMessages.Common.OperationCanceled,
                    ErrorType.Unexpected,
                    ErrorCodes.Common.OperationCanceled);
            }
            catch (Exception)
            {
                return ServiceResult<List<KeyValueItemDTO>>.Fail(
                    ErrorMessages.Common.UnexpectedError,
                    ErrorType.Unexpected,
                    ErrorCodes.Common.UnexpectedError);
            }
        }
    }
}