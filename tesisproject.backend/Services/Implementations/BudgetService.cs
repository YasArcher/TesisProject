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
        private const string NoBudgetsFoundMessage = "No budgets found.";
        private const string BudgetsRetrievedMessage = "Budgets retrieved";
        private const string BudgetNotFoundMessage = "Budget not found.";
        private const string BudgetRetrievedMessage = "Budget retrieved";
        private const string NoBudgetsForProjectMessage = "No budgets found for project.";
        private const string UserNotFoundMessage = "User not found.";
        private const string ProjectDoesNotExistMessage = "Project does not exist.";
        private const string BudgetCreatedMessage = "Budget created";
        private const string UserNotAuthenticatedMessage = "User not authenticated.";
        private const string AuthUserNotAuthenticatedCode = "AUTH_USER_NOT_AUTHENTICATED";
        private const string BudgetUpdatedMessage = "Budget updated";
        private const string BudgetDeletedMessage = "Budget deleted.";
        private const string CertificationExceedsInitialAmountMessage = "Certification exceeds initial amount.";
        private const string CertificationRegisteredMessage = "Certification registered";
        private const string BudgetTransactionNotFoundMessage = "Budget transaction not found.";
        private const string TransactionAlreadyExecutedMessage = "This transaction has already been executed.";
        private const string ExecutedExceedsCertifiedForTransactionMessage = "Executed amount cannot exceed certified amount for this transaction.";
        private const string ExecutedTotalExceedsCertifiedTotalMessage = "Executed total for this budget cannot exceed the certified total.";
        private const string ExecutionRegisteredMessage = "Execution registered";
        private const string ExecutedTransactionsCannotBeCancelledMessage = "Executed transactions cannot be cancelled.";
        private const string TransactionAlreadyCancelledMessage = "This transaction is already cancelled.";
        private const string TransactionCancelledMessage = "Transaction cancelled.";
        private const string CancelledTransactionsCannotBeUpdatedMessage = "Cancelled transactions cannot be updated.";
        private const string ExecutedAmountExceedsCertifiedMessage = "Executed amount cannot exceed certified amount.";
        private const string ExecutedAmountRequiredMessage = "ExecutedAmount is required for executed transactions.";
        private const string ExecutedAtRequiredForExecutedMessage = "Executed transactions cannot be updated to type Executed without ExecutedAt set.";
        private const string TotalCertifiedExceedsInitialMessage = "Total certified amount exceeds the budget initial amount.";
        private const string TotalExecutedExceedsCertifiedMessage = "Total executed amount cannot exceed the total certified amount.";
        private const string TransactionUpdatedMessage = "Transaction updated.";
        private const string TransactionsRetrievedMessage = "Transactions retrieved";

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
                    return ServiceResult<List<BudgetListItemDTO>>.Fail(NoBudgetsFoundMessage, ErrorType.NotFound);

                return ServiceResult<List<BudgetListItemDTO>>.Ok(items, BudgetsRetrievedMessage);
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
                    return ServiceResult<BudgetDTO>.Fail(BudgetNotFoundMessage, ErrorType.NotFound);

                var dto = MapToDTO(e);
                return ServiceResult<BudgetDTO>.Ok(dto, BudgetRetrievedMessage);
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
                        NoBudgetsForProjectMessage,
                        ErrorType.NotFound);
                }

                var dtoList = list
                    .Select(MapToDTO)
                    .ToList();

                return ServiceResult<List<BudgetDTO>>.Ok(dtoList, BudgetsRetrievedMessage);
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
                        UserNotFoundMessage,
                        ErrorType.NotFound);
                }

                var projectExists = await _uow.Projects
                    .Query(asNoTracking: true)
                    .AnyAsync(p => p.ProjectId == request.ProjectId, ct);

                if (!projectExists)
                {
                    return ServiceResult<BudgetDTO>.Fail(
                        ProjectDoesNotExistMessage,
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
                    BudgetCreatedMessage);
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<BudgetDTO>.Fail(
                    UserNotAuthenticatedMessage,
                    ErrorType.Unauthorized,
                    AuthUserNotAuthenticatedCode);
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
                        UserNotFoundMessage,
                        ErrorType.NotFound);
                }

                var e = await _uow.Budgets.GetByIdAsync(new object[] { budgetId }, ct);
                if (e is null)
                    return ServiceResult<BudgetDTO>.Fail(BudgetNotFoundMessage, ErrorType.NotFound);

                e.ApprovedByUserId = actorUserId.Value;
                e.InitialAmount = request.InitialAmount;
                e.CertifiedAmount = request.CertifiedAmount;
                e.ExecutedAmount = request.ExecutedAmount;
                e.ApprovedAt = request.ApprovedAt;
                e.FundingTypeId = request.FundingTypeId;

                _uow.Budgets.Update(e);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetDTO>.Ok(MapToDTO(e), BudgetUpdatedMessage);
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<BudgetDTO>.Fail(
                    UserNotAuthenticatedMessage,
                    ErrorType.Unauthorized,
                    AuthUserNotAuthenticatedCode);
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
                    return ServiceResult<NoContent>.Fail(BudgetNotFoundMessage, ErrorType.NotFound);

                _uow.Budgets.Remove(e);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), BudgetDeletedMessage);
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
                        UserNotFoundMessage,
                        ErrorType.NotFound);
                }

                var budget = await _uow.Budgets.GetByIdAsync(new object[] { request.BudgetId }, ct);
                if (budget is null)
                    return ServiceResult<BudgetTransactionDTO>.Fail(BudgetNotFoundMessage, ErrorType.NotFound);

                var newCertified = budget.CertifiedAmount + request.CertifiedAmount;
                if (newCertified > budget.InitialAmount)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        CertificationExceedsInitialAmountMessage,
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

                return ServiceResult<BudgetTransactionDTO>.Ok(Map(tx), CertificationRegisteredMessage);
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    UserNotAuthenticatedMessage,
                    ErrorType.Unauthorized,
                    AuthUserNotAuthenticatedCode);
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
                        UserNotFoundMessage,
                        ErrorType.NotFound);
                }

                var tx = await _uow.Budgets.GetTransactionByIdAsync(request.BudgetTransactionId, ct);
                if (tx is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        BudgetTransactionNotFoundMessage,
                        ErrorType.NotFound);
                }

                if (tx.TransactionTypeId != BudgetTransactionTypeIds.Certification)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        TransactionAlreadyExecutedMessage,
                        ErrorType.Validation);
                }

                if (request.ExecutedAmount > tx.CertifiedAmount)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        ExecutedExceedsCertifiedForTransactionMessage,
                        ErrorType.Validation);
                }

                var budget = await _uow.Budgets.GetByIdAsync(new object[] { tx.BudgetId }, ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        BudgetNotFoundMessage,
                        ErrorType.NotFound);
                }

                var newExecutedTotal = budget.ExecutedAmount + request.ExecutedAmount;
                if (newExecutedTotal > budget.CertifiedAmount)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        ExecutedTotalExceedsCertifiedTotalMessage,
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
                    ExecutionRegisteredMessage);
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    UserNotAuthenticatedMessage,
                    ErrorType.Unauthorized,
                    AuthUserNotAuthenticatedCode);
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
                    return ServiceResult<List<BudgetTransactionDTO>>.Fail(BudgetNotFoundMessage, ErrorType.NotFound);

                var list = await _uow.Budgets.GetTransactionsAsync(budgetId, ct);
                var dto = list.Select(Map).ToList();

                return ServiceResult<List<BudgetTransactionDTO>>.Ok(dto, TransactionsRetrievedMessage);
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
                        BudgetTransactionNotFoundMessage,
                        ErrorType.NotFound);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Executed || tx.ExecutedAt != null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        ExecutedTransactionsCannotBeCancelledMessage,
                        ErrorType.Validation);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Cancelled)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        TransactionAlreadyCancelledMessage,
                        ErrorType.Validation);
                }

                var budget = await _uow.Budgets.GetByIdAsync(new object[] { tx.BudgetId }, ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        BudgetNotFoundMessage,
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
                    TransactionCancelledMessage);
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
                        UserNotFoundMessage,
                        ErrorType.NotFound);
                }

                var tx = await _uow.Budgets.GetTransactionByIdAsync(budgetTransactionId, ct);
                if (tx is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        BudgetTransactionNotFoundMessage,
                        ErrorType.NotFound);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Cancelled)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        CancelledTransactionsCannotBeUpdatedMessage,
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
                        ExecutedAmountExceedsCertifiedMessage,
                        ErrorType.Validation);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Executed)
                {
                    if (tx.ExecutedAmount is null)
                    {
                        return ServiceResult<BudgetTransactionDTO>.Fail(
                            ExecutedAmountRequiredMessage,
                            ErrorType.Validation);
                    }

                    if (tx.ExecutedAt is null)
                    {
                        return ServiceResult<BudgetTransactionDTO>.Fail(
                            ExecutedAtRequiredForExecutedMessage,
                            ErrorType.Validation);
                    }
                }

                var budget = await _uow.Budgets.GetByIdAsync(new object[] { tx.BudgetId }, ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        BudgetNotFoundMessage,
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
                        TotalCertifiedExceedsInitialMessage,
                        ErrorType.Validation);
                }

                if (executedTotal > certifiedEverTotal)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        TotalExecutedExceedsCertifiedMessage,
                        ErrorType.Validation);
                }

                budget.CertifiedAmount = certifiedCurrentTotal;
                budget.ExecutedAmount = executedTotal;

                _uow.Budgets.Update(budget);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(
                    Map(tx),
                    TransactionUpdatedMessage);
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    UserNotAuthenticatedMessage,
                    ErrorType.Unauthorized,
                    AuthUserNotAuthenticatedCode);
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
