using tesisproject.shared.DTOs.Budgets.Request;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IBudgetService
    {
        Task<ServiceResult<List<BudgetListItemDTO>>> GetAllAsync(CancellationToken ct = default);
        Task<ServiceResult<BudgetDTO>> GetByIdAsync(int budgetId, CancellationToken ct = default);
        Task<ServiceResult<BudgetDTO>> GetByProjectIdAsync(int projectId, CancellationToken ct = default);
        Task<ServiceResult<BudgetDTO>> CreateAsync(CreateBudgetRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<BudgetDTO>> UpdateAsync(int budgetId, UpdateBudgetRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<bool>> DeleteAsync(int budgetId, CancellationToken ct = default);

        // ------- NUEVOS ----------
        Task<ServiceResult<BudgetTransactionDTO>> AddCertificationAsync(AddCertificationRequestDTO request, int currentUserId, CancellationToken ct = default);
        Task<ServiceResult<BudgetTransactionDTO>> ExecuteDevengadoAsync(ExecuteDevengadoRequestDTO request, int currentUserId, CancellationToken ct = default);

        Task<ServiceResult<List<BudgetTransactionDTO>>> GetTransactionsAsync(int budgetId, CancellationToken ct = default);
    }
}