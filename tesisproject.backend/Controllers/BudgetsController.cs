using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Budgets.Request;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BudgetsController : ControllerBase
    {
        private readonly IBudgetService _service;

        public BudgetsController(IBudgetService service) => _service = service;

        // POST: api/Budgets
        [HttpPost]
        public async Task<ActionResult<ServiceResult<BudgetDTO>>> Create(
            [FromBody] CreateBudgetRequestDTO request,
            CancellationToken ct)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        // GET: api/Budgets/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<BudgetDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/Budgets
        [HttpGet]
        public async Task<ActionResult<ServiceResult<List<BudgetListItemDTO>>>> List(
            CancellationToken ct = default)
            => (await _service.GetAllAsync(ct)).ToActionResult();

        // GET: api/Budgets/project/{projectId}
        [HttpGet("project/{projectId:int}")]
        public async Task<ActionResult<ServiceResult<List<BudgetDTO>>>> GetByProjectId(
            int projectId,
            CancellationToken ct = default)
            => (await _service.GetByProjectIdAsync(projectId, ct)).ToActionResult();

        // PUT: api/Budgets/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<BudgetDTO>>> Update(
            int id,
            [FromBody] UpdateBudgetRequestDTO request,
            CancellationToken ct)
            => (await _service.UpdateAsync(id, request, ct)).ToActionResult();

        // DELETE: api/Budgets/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ServiceResult<NoContent>>> Delete(
            int id,
            CancellationToken ct)
            => (await _service.DeleteAsync(id, ct)).ToActionResult();

        [HttpPost("{budgetId:int}/certifications")]
        public async Task<ActionResult<ServiceResult<BudgetTransactionDTO>>> AddCertification(
            int budgetId,
            [FromBody] AddCertificationRequestDTO request,
            CancellationToken ct)
        {
            if (request.BudgetId == 0)
                request.BudgetId = budgetId;

            return (await _service.AddCertificationAsync(request, ct))
                .ToActionResult();
        }

        [HttpPost("transactions/{transactionId:int}/devengar")]
        public async Task<ActionResult<ServiceResult<BudgetTransactionDTO>>> ExecuteDevengado(
            int transactionId,
            [FromBody] ExecuteDevengadoRequestDTO request,
            CancellationToken ct)
        {
            if (request.BudgetTransactionId == 0)
                request.BudgetTransactionId = transactionId;

            return (await _service.ExecuteDevengadoAsync(request, ct))
                .ToActionResult();
        }

        // GET: api/Budgets/{budgetId}/transactions
        [HttpGet("{budgetId:int}/transactions")]
        public async Task<ActionResult<ServiceResult<List<BudgetTransactionDTO>>>> GetTransactions(
            int budgetId,
            CancellationToken ct)
            => (await _service.GetTransactionsAsync(budgetId, ct)).ToActionResult();

        // PUT: api/Budgets/transactions/{transactionId}/cancel
        [HttpPut("transactions/{transactionId:int}/cancel")]
        public async Task<ActionResult<ServiceResult<BudgetTransactionDTO>>> CancelTransaction(
            int transactionId,
            CancellationToken ct)
            => (await _service.CancelTransactionAsync(transactionId, ct))
                .ToActionResult();

        // PUT: api/Budgets/transactions/{transactionId}
        [HttpPut("transactions/{transactionId:int}")]
        public async Task<ActionResult<ServiceResult<BudgetTransactionDTO>>> UpdateTransaction(
            int transactionId,
            [FromBody] UpdateBudgetTransactionRequestDTO request,
            CancellationToken ct)
            => (await _service.UpdateTransactionAsync(transactionId, request, ct))
                .ToActionResult();
    }
}