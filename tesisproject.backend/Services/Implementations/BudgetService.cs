using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Budgets.Request;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class BudgetService : IBudgetService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public BudgetService(
            IUnitOfWork uow,
            ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        // =============== READS ===============

        public async Task<ServiceResult<List<BudgetListItemDTO>>> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                var items = await _uow.Budgets
                    .Query(asNoTracking: true)
                    .OrderByDescending(b => b.BudgetId)
                    .Select(b => new BudgetListItemDTO
                    {
                        BudgetId = b.BudgetId,
                        ProjectId = b.ProjectId,
                        InitialAmount = b.InitialAmount,
                        CertifiedAmount = b.CertifiedAmount,
                        ExecutedAmount = b.ExecutedAmount,
                        ApprovedAt = b.ApprovedAt
                    })
                    .ToListAsync(ct);

                if (items.Count == 0)
                    return ServiceResult<List<BudgetListItemDTO>>.Fail("No budgets found.", ErrorType.NotFound);

                return ServiceResult<List<BudgetListItemDTO>>.Ok(items, "Budgets retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<BudgetListItemDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<BudgetDTO>> GetByIdAsync(int budgetId, CancellationToken ct = default)
        {
            try
            {
                var e = await _uow.Budgets.GetByIdAsync(new object[] { budgetId }, ct);
                if (e is null)
                    return ServiceResult<BudgetDTO>.Fail("Budget not found.", ErrorType.NotFound);

                var dto = MapToDTO(e);
                return ServiceResult<BudgetDTO>.Ok(dto, "Budget retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<BudgetDTO>>> GetByProjectIdAsync(
            int projectId,
            CancellationToken ct = default)
        {
            try
            {
                var query = _uow.Budgets
                    .Query(asNoTracking: true)
                    .Where(b => b.ProjectId == projectId)
                    .Include(b => b.FundingType);

                var list = await query.ToListAsync(ct);

                if (list.Count == 0)
                {
                    return ServiceResult<List<BudgetDTO>>.Fail(
                        "No budgets found for project.",
                        ErrorType.NotFound);
                }

                var dtoList = list
                    .Select(MapToDTO)
                    .ToList();

                return ServiceResult<List<BudgetDTO>>.Ok(dtoList, "Budgets retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<BudgetDTO>>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        // =============== WRITES ===============

        public async Task<ServiceResult<BudgetDTO>> CreateAsync(
            CreateBudgetRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return ServiceResult<BudgetDTO>.Fail(
                        "User not found.",
                        ErrorType.NotFound);
                }

                var projectExists = await _uow.Projects
                    .Query(asNoTracking: true)
                    .AnyAsync(p => p.ProjectId == request.ProjectId, ct);

                if (!projectExists)
                {
                    return ServiceResult<BudgetDTO>.Fail(
                        "Project does not exist.",
                        ErrorType.NotFound);
                }

                var entity = new Budget
                {
                    ProjectId = request.ProjectId,
                    ApprovedByUserId = actorUserId.Value,
                    InitialAmount = request.InitialAmount,
                    CertifiedAmount = 0,
                    ExecutedAmount = 0,
                    ApprovedAt = null,
                    FundingTypeId = request.FundingTypeId
                };

                await _uow.Budgets.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetDTO>.Ok(
                    MapToDTO(entity),
                    "Budget created");
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<BudgetDTO>.Fail(
                    "User not authenticated.",
                    ErrorType.Unauthorized,
                    "AUTH_USER_NOT_AUTHENTICATED");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<BudgetDTO>> UpdateAsync(
            int budgetId,
            UpdateBudgetRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return ServiceResult<BudgetDTO>.Fail(
                        "User not found.",
                        ErrorType.NotFound);
                }

                var e = await _uow.Budgets.GetByIdAsync(new object[] { budgetId }, ct);
                if (e is null)
                    return ServiceResult<BudgetDTO>.Fail("Budget not found.", ErrorType.NotFound);

                e.ApprovedByUserId = actorUserId.Value;
                e.InitialAmount = request.InitialAmount;
                e.CertifiedAmount = request.CertifiedAmount;
                e.ExecutedAmount = request.ExecutedAmount;
                e.ApprovedAt = request.ApprovedAt;
                e.FundingTypeId = request.FundingTypeId;

                _uow.Budgets.Update(e);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetDTO>.Ok(MapToDTO(e), "Budget updated");
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<BudgetDTO>.Fail(
                    "User not authenticated.",
                    ErrorType.Unauthorized,
                    "AUTH_USER_NOT_AUTHENTICATED");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(int budgetId, CancellationToken ct = default)
        {
            try
            {
                var e = await _uow.Budgets.GetByIdAsync(new object[] { budgetId }, ct);
                if (e is null)
                    return ServiceResult<NoContent>.Fail("Budget not found.", ErrorType.NotFound);

                _uow.Budgets.Remove(e);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "Budget deleted.");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<NoContent>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<BudgetTransactionDTO>> AddCertificationAsync(
            AddCertificationRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "User not found.",
                        ErrorType.NotFound);
                }

                var budget = await _uow.Budgets.GetByIdAsync(new object[] { request.BudgetId }, ct);
                if (budget is null)
                    return ServiceResult<BudgetTransactionDTO>.Fail("Budget not found.", ErrorType.NotFound);

                var newCertified = budget.CertifiedAmount + request.CertifiedAmount;
                if (newCertified > budget.InitialAmount)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Certification exceeds initial amount.",
                        ErrorType.Validation);
                }

                var tx = new BudgetTransaction
                {
                    BudgetId = request.BudgetId,
                    TransactionTypeId = BudgetTransactionTypeIds.Certification,
                    Name = request.Name,
                    CertifiedAmount = request.CertifiedAmount,
                    BudgetItem = request.BudgetItem,
                    CertificationDescription = request.CertificationDescription,
                    CertifiedByUserId = actorUserId.Value,
                    CertifiedAt = (request.CertifiedAt ?? DateTime.UtcNow).Date
                };

                await _uow.Budgets.AddTransactionAsync(tx, ct);

                budget.CertifiedAmount = newCertified;
                _uow.Budgets.Update(budget);

                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(Map(tx), "Certification registered");
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    "User not authenticated.",
                    ErrorType.Unauthorized,
                    "AUTH_USER_NOT_AUTHENTICATED");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<BudgetTransactionDTO>> ExecuteDevengadoAsync(
            ExecuteDevengadoRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "User not found.",
                        ErrorType.NotFound);
                }

                var tx = await _uow.Budgets.GetTransactionByIdAsync(request.BudgetTransactionId, ct);
                if (tx is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Budget transaction not found.",
                        ErrorType.NotFound);
                }

                if (tx.TransactionTypeId != BudgetTransactionTypeIds.Certification)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "This transaction has already been executed.",
                        ErrorType.Validation);
                }

                if (request.ExecutedAmount > tx.CertifiedAmount)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Executed amount cannot exceed certified amount for this transaction.",
                        ErrorType.Validation);
                }

                var budget = await _uow.Budgets.GetByIdAsync(new object[] { tx.BudgetId }, ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Budget not found.",
                        ErrorType.NotFound);
                }

                var newExecutedTotal = budget.ExecutedAmount + request.ExecutedAmount;
                if (newExecutedTotal > budget.CertifiedAmount)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Executed total for this budget cannot exceed the certified total.",
                        ErrorType.Validation);
                }

                tx.ExecutedAmount = request.ExecutedAmount;
                tx.CURNumber = request.CURNumber;
                tx.ExecutionDescription = request.ExecutionDescription;
                tx.ExecutedByUserId = actorUserId.Value;
                tx.ExecutedAt = (request.ExecutedAt ?? DateTime.UtcNow).Date;
                tx.TransactionTypeId = BudgetTransactionTypeIds.Executed;

                budget.ExecutedAmount = newExecutedTotal;

                var newCertifiedTotal = budget.CertifiedAmount - tx.CertifiedAmount;
                if (newCertifiedTotal < 0)
                    newCertifiedTotal = 0;

                budget.CertifiedAmount = newCertifiedTotal;
                _uow.Budgets.Update(budget);

                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(
                    Map(tx),
                    "Execution registered");
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    "User not authenticated.",
                    ErrorType.Unauthorized,
                    "AUTH_USER_NOT_AUTHENTICATED");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<BudgetTransactionDTO>>> GetTransactionsAsync(
            int budgetId,
            CancellationToken ct = default)
        {
            try
            {
                var exists = await _uow.Budgets.GetByIdAsync(new object[] { budgetId }, ct);
                if (exists is null)
                    return ServiceResult<List<BudgetTransactionDTO>>.Fail("Budget not found.", ErrorType.NotFound);

                var list = await _uow.Budgets.GetTransactionsAsync(budgetId, ct);
                var dto = list.Select(Map).ToList();

                return ServiceResult<List<BudgetTransactionDTO>>.Ok(dto, "Transactions retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<BudgetTransactionDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<BudgetTransactionDTO>> CancelTransactionAsync(
            int budgetTransactionId,
            CancellationToken ct = default)
        {
            try
            {
                var tx = await _uow.Budgets.GetTransactionByIdAsync(budgetTransactionId, ct);

                if (tx is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Budget transaction not found.",
                        ErrorType.NotFound);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Executed || tx.ExecutedAt != null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Executed transactions cannot be cancelled.",
                        ErrorType.Validation);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Cancelled)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "This transaction is already cancelled.",
                        ErrorType.Validation);
                }

                var budget = await _uow.Budgets.GetByIdAsync(new object[] { tx.BudgetId }, ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Budget not found.",
                        ErrorType.NotFound);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Certification)
                {
                    var newCertifiedTotal = budget.CertifiedAmount - tx.CertifiedAmount;

                    if (newCertifiedTotal < 0)
                        newCertifiedTotal = 0;

                    budget.CertifiedAmount = newCertifiedTotal;
                    _uow.Budgets.Update(budget);
                }

                tx.TransactionTypeId = BudgetTransactionTypeIds.Cancelled;

                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(
                    Map(tx),
                    "Transaction cancelled.");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<BudgetTransactionDTO>> UpdateTransactionAsync(
            int budgetTransactionId,
            UpdateBudgetTransactionRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "User not found.",
                        ErrorType.NotFound);
                }

                var tx = await _uow.Budgets.GetTransactionByIdAsync(budgetTransactionId, ct);
                if (tx is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Budget transaction not found.",
                        ErrorType.NotFound);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Cancelled)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Cancelled transactions cannot be updated.",
                        ErrorType.Validation);
                }

                tx.TransactionTypeId = request.TransactionTypeId;
                tx.Name = request.Name;
                tx.CertifiedAmount = request.CertifiedAmount;
                tx.ExecutedAmount = request.ExecutedAmount;
                tx.BudgetItem = request.BudgetItem;
                tx.CURNumber = request.CURNumber;
                tx.CertificationDescription = request.CertificationDescription;
                tx.ExecutionDescription = request.ExecutionDescription;

                var executedValue = tx.ExecutedAmount ?? 0m;
                if (executedValue > tx.CertifiedAmount)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Executed amount cannot exceed certified amount.",
                        ErrorType.Validation);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Executed)
                {
                    if (tx.ExecutedAmount is null)
                    {
                        return ServiceResult<BudgetTransactionDTO>.Fail(
                            "ExecutedAmount is required for executed transactions.",
                            ErrorType.Validation);
                    }

                    if (tx.ExecutedAt is null)
                    {
                        return ServiceResult<BudgetTransactionDTO>.Fail(
                            "Executed transactions cannot be updated to type Executed without ExecutedAt set.",
                            ErrorType.Validation);
                    }
                }

                var budget = await _uow.Budgets.GetByIdAsync(new object[] { tx.BudgetId }, ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Budget not found.",
                        ErrorType.NotFound);
                }

                var all = await _uow.Budgets.GetTransactionsAsync(budget.BudgetId, ct);

                for (int i = 0; i < all.Count; i++)
                {
                    if (all[i].BudgetTransactionId == tx.BudgetTransactionId)
                    {
                        all[i].TransactionTypeId = tx.TransactionTypeId;
                        all[i].Name = tx.Name;
                        all[i].CertifiedAmount = tx.CertifiedAmount;
                        all[i].ExecutedAmount = tx.ExecutedAmount;
                        all[i].BudgetItem = tx.BudgetItem;
                        all[i].CURNumber = tx.CURNumber;
                        all[i].CertificationDescription = tx.CertificationDescription;
                        all[i].ExecutionDescription = tx.ExecutionDescription;
                        break;
                    }
                }

                var certifiedCurrentTotal = all
                    .Where(t => t.TransactionTypeId == BudgetTransactionTypeIds.Certification)
                    .Sum(t => t.CertifiedAmount);

                var executedTotal = all
                    .Where(t => t.TransactionTypeId == BudgetTransactionTypeIds.Executed)
                    .Sum(t => t.ExecutedAmount ?? 0m);

                var certifiedEverTotal = all
                    .Where(t =>
                        t.TransactionTypeId == BudgetTransactionTypeIds.Certification ||
                        t.TransactionTypeId == BudgetTransactionTypeIds.Executed)
                    .Sum(t => t.CertifiedAmount);

                if (certifiedEverTotal > budget.InitialAmount)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Total certified amount exceeds the budget initial amount.",
                        ErrorType.Validation);
                }

                if (executedTotal > certifiedEverTotal)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Total executed amount cannot exceed the total certified amount.",
                        ErrorType.Validation);
                }

                budget.CertifiedAmount = certifiedCurrentTotal;
                budget.ExecutedAmount = executedTotal;

                _uow.Budgets.Update(budget);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(
                    Map(tx),
                    "Transaction updated.");
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    "User not authenticated.",
                    ErrorType.Unauthorized,
                    "AUTH_USER_NOT_AUTHENTICATED");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        // =============== HELPERS ===============

        private async Task<int?> GetExistingActorUserIdAsync(CancellationToken ct)
        {
            var currentUserId = _currentUser.GetRequiredUserId();
            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            return user?.IdUser;
        }

        private static BudgetDTO MapToDTO(Budget e) => new()
        {
            BudgetId = e.BudgetId,
            ProjectId = e.ProjectId,
            ApprovedByUserId = e.ApprovedByUserId,
            FundingTypeId = e.FundingTypeId,
            FundingTypeName = e.FundingType?.Name,
            InitialAmount = e.InitialAmount,
            CertifiedAmount = e.CertifiedAmount,
            ExecutedAmount = e.ExecutedAmount,
            ApprovedAt = e.ApprovedAt
        };

        private static BudgetTransactionDTO Map(BudgetTransaction t) => new()
        {
            BudgetTransactionId = t.BudgetTransactionId,
            BudgetId = t.BudgetId,
            TransactionTypeId = t.TransactionTypeId,
            Name = t.Name,
            CertifiedAmount = t.CertifiedAmount,
            ExecutedAmount = t.ExecutedAmount ?? 0,
            CertifiedAt = t.CertifiedAt,
            ExecutedAt = t.ExecutedAt,
            CertifiedByUserId = t.CertifiedByUserId,
            ExecutedByUserId = t.ExecutedByUserId,
            BudgetItem = t.BudgetItem,
            CURNumber = t.CURNumber,
            CertificationDescription = t.CertificationDescription,
            ExecutionDescription = t.ExecutionDescription
        };
    }
}