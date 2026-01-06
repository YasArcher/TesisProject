using tesisproject.shared.DTOs.Budgets.Request;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IBudgetClientService
    {
        // =========================
        //            CRUD
        // =========================

        Task<HttpResponseWrapper<BudgetDTO?>> CreateAsync(
            CreateBudgetRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<BudgetDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<BudgetListItemDTO>?>> GetListAsync(
            CancellationToken ct = default);

        Task<HttpResponseWrapper<BudgetDTO?>> UpdateAsync(
            int id,
            UpdateBudgetRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> DeleteAsync(int id, CancellationToken ct = default);


        // =========================
        //            QUERIES
        // =========================

        Task<HttpResponseWrapper<List<BudgetDTO>?>> GetProjectBudgetsAsync(
            int projectId,
            CancellationToken ct = default);

        // =========================
        //         TRANSACTIONS
        // =========================

        Task<HttpResponseWrapper<List<BudgetTransactionDTO>?>> GetListBudgetTransactionAsync(
            int idBudget,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<BudgetTransactionDTO?>> CreateCertificationAsync(
            int budgetId,
            AddCertificationRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<BudgetTransactionDTO?>> ExecuteAccrualAsync(
            int transactionId,
            ExecuteDevengadoRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<BudgetTransactionDTO?>> CancelTransactionAsync(
            int transactionId,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<BudgetTransactionDTO?>> UpdateTransactionAsync(
    int transactionId,
    UpdateBudgetTransactionRequestDTO request,
    CancellationToken ct = default);

    }
}
