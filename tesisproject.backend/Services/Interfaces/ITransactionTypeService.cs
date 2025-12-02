using tesisproject.shared.DTOs.Catalog.TransactionTypes.Request;
using tesisproject.shared.DTOs.Catalog.TransactionTypes.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface ITransactionTypeService
    {
        // ================= READS =================

        Task<ServiceResult<IReadOnlyList<TransactionTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<TransactionTypeListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);

        // ================ WRITES ================

        Task<ServiceResult<TransactionTypeListItemDTO>> CreateAsync(
            AddTransactionTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<TransactionTypeListItemDTO>> UpdateAsync(
            UpdateTransactionTypeRequestDTO request,
            CancellationToken ct = default);
    }
}