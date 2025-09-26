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
        public Task<HttpResponseWrapper<BudgetDTO?>> GetBudgetAsync(int idProject, CancellationToken ct = default)
        {
            return _api.GetAsync<BudgetDTO>($"{_baseUrl}/project/{idProject}", ct);
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
    }
}
