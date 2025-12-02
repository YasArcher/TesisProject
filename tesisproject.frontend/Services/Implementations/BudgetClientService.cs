using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Budgets.Request;

namespace tesisproject.frontend.Services.Implementations
{
    public class BudgetClientService : IBudgetClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/Budgets";

        public BudgetClientService(IApiClient api) => _api = api;

        // Obtiene el presupuesto de un proyecto
        public Task<HttpResponseWrapper<List<BudgetDTO>?>> GetProjectBudgetsAsync(int projectId, CancellationToken ct = default)
        {
            return _api.GetAsync<List<BudgetDTO>>($"{_baseUrl}/project/{projectId}", ct);
        }

        // Lista de transacciones de un presupuesto
        public Task<HttpResponseWrapper<List<BudgetTransactionDTO>?>> GetListBudgetTransactionAsync(int idBudget, CancellationToken ct = default)
        {
            return _api.GetAsync<List<BudgetTransactionDTO>>($"{_baseUrl}/{idBudget}/transactions", ct);
        }
        // Crear una certificacion en un presupuesto
        public Task<HttpResponseWrapper<BudgetTransactionDTO?>> CreateCertificationAsync(int budgetId, AddCertificationRequestDTO request, CancellationToken ct = default)
        {
            return _api.PostAsync<AddCertificationRequestDTO, BudgetTransactionDTO>($"{_baseUrl}/{budgetId}/certifications", request,  ct);
        }

        public Task<HttpResponseWrapper<BudgetTransactionDTO?>> ExecuteAccrualAsync(int budgetId, ExecuteDevengadoRequestDTO request, CancellationToken ct = default)
        {
            return _api.PostAsync<ExecuteDevengadoRequestDTO, BudgetTransactionDTO>($"{_baseUrl}/transactions/{budgetId}/devengar", request, ct);
        }

        public Task<HttpResponseWrapper<BudgetTransactionDTO?>> CancelTransactionAsync(
            int transactionId,
            CancellationToken ct = default)
        {
            // Enviamos un body vacío porque nuestro ApiClient siempre serializa algo.
            var emptyBody = new { };

            return _api.PutAsync<object, BudgetTransactionDTO>(
                $"{_baseUrl}/transactions/{transactionId}/cancel",
                emptyBody,
                ct);
        }
    }
}
