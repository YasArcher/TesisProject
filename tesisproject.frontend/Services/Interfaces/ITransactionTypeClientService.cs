using tesisproject.shared.DTOs.Catalog.TransactionTypes.Request;
using tesisproject.shared.DTOs.Catalog.TransactionTypes.Response;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface ITransactionTypeClientService
    {
        Task<HttpResponseWrapper<List<TransactionTypeListItemDTO>?>> GetListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<TransactionTypeListItemDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<TransactionTypeListItemDTO?>> CreateAsync(
            AddTransactionTypeRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<TransactionTypeListItemDTO?>> UpdateAsync(
            UpdateTransactionTypeRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<KeyValueItemDTO>?>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);
    }
}