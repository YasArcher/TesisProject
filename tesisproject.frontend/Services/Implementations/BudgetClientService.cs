using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Budgets.Request;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class BudgetClientService : IBudgetClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "budgets";

        public BudgetClientService(IApiClient api) => _api = api;

        // =========================
        //            CRUD
        // =========================

        public Task<HttpResponseWrapper<BudgetDTO?>> CreateAsync(
            CreateBudgetRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/Budgets
            return _api.PostAsync<CreateBudgetRequestDTO, BudgetDTO>(
                _baseUrl,
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<BudgetDTO?>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("id must be a positive value.", nameof(id));

            // GET: api/Budgets/{id}
            return _api.GetAsync<BudgetDTO>(
                $"{_baseUrl}/{id}",
                ct
            );
        }

        public Task<HttpResponseWrapper<List<BudgetListItemDTO>?>> GetListAsync(
            CancellationToken ct = default)
        {
            // GET: api/Budgets
            return _api.GetAsync<List<BudgetListItemDTO>>(
                _baseUrl,
                ct
            );
        }

        public Task<HttpResponseWrapper<BudgetDTO?>> UpdateAsync(
            int id,
            UpdateBudgetRequestDTO request,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("id must be a positive value.", nameof(id));

            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // PUT: api/Budgets/{id}
            return _api.PutAsync<UpdateBudgetRequestDTO, BudgetDTO>(
                $"{_baseUrl}/{id}",
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<NoContent?>> DeleteAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentException("id must be a positive value.", nameof(id));

            return _api.DeleteAsync($"{_baseUrl}/{id}", ct);
        }


        // =========================
        //            QUERIES
        // =========================

        public Task<HttpResponseWrapper<List<BudgetDTO>?>> GetProjectBudgetsAsync(
            int projectId,
            CancellationToken ct = default)
        {
            if (projectId <= 0)
                throw new ArgumentException("projectId must be a positive value.", nameof(projectId));

            // GET: api/Budgets/project/{projectId}
            return _api.GetAsync<List<BudgetDTO>>(
                $"{_baseUrl}/project/{projectId}",
                ct
            );
        }

        // =========================
        //         TRANSACTIONS
        // =========================

        public Task<HttpResponseWrapper<List<BudgetTransactionDTO>?>> GetListBudgetTransactionAsync(
            int idBudget,
            CancellationToken ct = default)
        {
            if (idBudget <= 0)
                throw new ArgumentException("idBudget must be a positive value.", nameof(idBudget));

            // GET: api/Budgets/{budgetId}/transactions
            return _api.GetAsync<List<BudgetTransactionDTO>>(
                $"{_baseUrl}/{idBudget}/transactions",
                ct
            );
        }

        public Task<HttpResponseWrapper<BudgetTransactionDTO?>> CreateCertificationAsync(
            int budgetId,
            AddCertificationRequestDTO request,
            CancellationToken ct = default)
        {
            if (budgetId <= 0)
                throw new ArgumentException("budgetId must be a positive value.", nameof(budgetId));

            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/Budgets/{budgetId}/certifications
            return _api.PostAsync<AddCertificationRequestDTO, BudgetTransactionDTO>(
                $"{_baseUrl}/{budgetId}/certifications",
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<BudgetTransactionDTO?>> ExecuteAccrualAsync(
            int transactionId,
            ExecuteDevengadoRequestDTO request,
            CancellationToken ct = default)
        {
            if (transactionId <= 0)
                throw new ArgumentException("transactionId must be a positive value.", nameof(transactionId));

            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // POST: api/Budgets/transactions/{transactionId}/devengar
            return _api.PostAsync<ExecuteDevengadoRequestDTO, BudgetTransactionDTO>(
                $"{_baseUrl}/transactions/{transactionId}/devengar",
                request,
                ct
            );
        }

        public Task<HttpResponseWrapper<BudgetTransactionDTO?>> CancelTransactionAsync(
            int transactionId,
            CancellationToken ct = default)
        {
            if (transactionId <= 0)
                throw new ArgumentException("transactionId must be a positive value.", nameof(transactionId));

            // PUT: api/Budgets/transactions/{transactionId}/cancel
            // We send an empty body because ApiClient always serializes something.
            var emptyBody = new { };

            return _api.PutAsync<object, BudgetTransactionDTO>(
                $"{_baseUrl}/transactions/{transactionId}/cancel",
                emptyBody,
                ct
            );
        }
        public Task<HttpResponseWrapper<BudgetTransactionDTO?>> UpdateTransactionAsync(
    int transactionId,
    UpdateBudgetTransactionRequestDTO request,
    CancellationToken ct = default)
        {
            if (transactionId <= 0)
                throw new ArgumentException("transactionId must be a positive value.", nameof(transactionId));

            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // PUT: api/Budgets/transactions/{transactionId}
            return _api.PutAsync<UpdateBudgetTransactionRequestDTO, BudgetTransactionDTO>(
                $"{_baseUrl}/transactions/{transactionId}",
                request,
                ct
            );
        }

    }
}
