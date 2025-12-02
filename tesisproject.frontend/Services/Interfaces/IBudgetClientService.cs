using tesisproject.shared.DTOs.Budgets.Request;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IBudgetClientService
    {
        Task<HttpResponseWrapper<List<BudgetDTO>?>> GetProjectBudgetsAsync(int projectId, CancellationToken ct = default);

        Task<HttpResponseWrapper<List<BudgetTransactionDTO>?>> GetListBudgetTransactionAsync(int idBudget, CancellationToken ct = default);
        //Crear certificacion
        Task<HttpResponseWrapper<BudgetTransactionDTO?>> CreateCertificationAsync(int budgetId, AddCertificationRequestDTO request, CancellationToken ct = default);

        //Ejecutar debengado
        Task<HttpResponseWrapper<BudgetTransactionDTO?>> ExecuteAccrualAsync(int budgetId , ExecuteDevengadoRequestDTO request, CancellationToken ct = default);

        Task<HttpResponseWrapper<BudgetTransactionDTO?>> CancelTransactionAsync(int transactionId, CancellationToken ct = default);

    }
}
