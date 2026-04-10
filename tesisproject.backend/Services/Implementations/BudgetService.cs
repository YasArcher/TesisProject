using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Budgets.Request;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class BudgetService : IBudgetService
    {
        private const string BudgetsRetrievedMessage = "Budgets retrieved.";
        private const string BudgetRetrievedMessage = "Budget retrieved.";
        private const string BudgetCreatedMessage = "Budget created.";
        private const string BudgetUpdatedMessage = "Budget updated.";
        private const string BudgetDeletedMessage = "Budget deleted.";
        private const string CertificationRegisteredMessage = "Certification registered.";
        private const string ExecutionRegisteredMessage = "Execution registered.";
        private const string TransactionCancelledMessage = "Transaction cancelled.";
        private const string TransactionUpdatedMessage = "Transaction updated.";
        private const string TransactionsRetrievedMessage = "Transactions retrieved.";

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
                {
                    return ServiceResult<List<BudgetListItemDTO>>.Fail(
                        ErrorMessages.Budget.NoneFound,
                        ErrorType.NotFound,
                        ErrorCodes.Budget.NoneFound);
                }

                return ServiceResult<List<BudgetListItemDTO>>.Ok(items, BudgetsRetrievedMessage);
            }
            catch (Exception)
            {
                return FailUnexpected<List<BudgetListItemDTO>>();
            }
        }

        public async Task<ServiceResult<BudgetDTO>> GetByIdAsync(int budgetId, CancellationToken ct = default)
        {
            try
            {
                var e = await _uow.Budgets.GetByIdAsync([budgetId], ct);
                if (e is null)
                {
                    return ServiceResult<BudgetDTO>.Fail(
                        ErrorMessages.Budget.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Budget.NotFound);
                }

                var dto = MapToDTO(e);
                return ServiceResult<BudgetDTO>.Ok(dto, BudgetRetrievedMessage);
            }
            catch (Exception)
            {
                return FailUnexpected<BudgetDTO>();
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
                        ErrorMessages.Budget.NoneFoundForProject,
                        ErrorType.NotFound,
                        ErrorCodes.Budget.NoneFoundForProject);
                }

                var dtoList = list
                    .Select(MapToDTO)
                    .ToList();

                return ServiceResult<List<BudgetDTO>>.Ok(dtoList, BudgetsRetrievedMessage);
            }
            catch (Exception)
            {
                return FailUnexpected<List<BudgetDTO>>();
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
                    return FailActorUserNotFound<BudgetDTO>();
                }

                var projectExists = await _uow.Projects
                    .Query(asNoTracking: true)
                    .AnyAsync(p => p.ProjectId == request.ProjectId, ct);

                if (!projectExists)
                {
                    return ServiceResult<BudgetDTO>.Fail(
                        ErrorMessages.Project.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Project.NotFound);
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
                return FailUnauthorized<BudgetDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<BudgetDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<BudgetDTO>();
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
                    return FailActorUserNotFound<BudgetDTO>();
                }

                var e = await _uow.Budgets.GetByIdAsync([budgetId], ct);
                if (e is null)
                {
                    return ServiceResult<BudgetDTO>.Fail(
                        ErrorMessages.Budget.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Budget.NotFound);
                }

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
                return FailUnauthorized<BudgetDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<BudgetDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<BudgetDTO>();
            }
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(int budgetId, CancellationToken ct = default)
        {
            try
            {
                var e = await _uow.Budgets.GetByIdAsync([budgetId], ct);
                if (e is null)
                {
                    return ServiceResult<NoContent>.Fail(
                        ErrorMessages.Budget.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Budget.NotFound);
                }

                _uow.Budgets.Remove(e);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), BudgetDeletedMessage);
            }
            catch (DbUpdateException)
            {
                return FailConflict<NoContent>();
            }
            catch (Exception)
            {
                return FailUnexpected<NoContent>();
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
                    return FailActorUserNotFound<BudgetTransactionDTO>();
                }

                var budget = await _uow.Budgets.GetByIdAsync([request.BudgetId], ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        ErrorMessages.Budget.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Budget.NotFound);
                }

                var newCertified = budget.CertifiedAmount + request.CertifiedAmount;
                if (newCertified > budget.InitialAmount)
                {
                    return ValidationFailure<BudgetTransactionDTO>(
                        ErrorMessages.Budget.CertificationExceedsInitialAmount,
                        ErrorCodes.Budget.CertificationExceedsInitialAmount,
                        nameof(AddCertificationRequestDTO.CertifiedAmount));
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
                return FailUnauthorized<BudgetTransactionDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<BudgetTransactionDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<BudgetTransactionDTO>();
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
                    return FailActorUserNotFound<BudgetTransactionDTO>();
                }

                var tx = await _uow.Budgets.GetTransactionByIdAsync(request.BudgetTransactionId, ct);
                if (tx is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        ErrorMessages.BudgetTransaction.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.BudgetTransaction.NotFound);
                }

                if (tx.TransactionTypeId != BudgetTransactionTypeIds.Certification)
                {
                    return ValidationFailure<BudgetTransactionDTO>(
                        ErrorMessages.BudgetTransaction.InvalidStateForExecution,
                        ErrorCodes.BudgetTransaction.InvalidStateForExecution);
                }

                if (request.ExecutedAmount > tx.CertifiedAmount)
                {
                    return ValidationFailure<BudgetTransactionDTO>(
                        ErrorMessages.BudgetTransaction.ExecutedAmountExceedsCertifiedAmount,
                        ErrorCodes.BudgetTransaction.ExecutedAmountExceedsCertifiedAmount,
                        nameof(ExecuteDevengadoRequestDTO.ExecutedAmount));
                }

                var budget = await _uow.Budgets.GetByIdAsync([tx.BudgetId], ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        ErrorMessages.Budget.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Budget.NotFound);
                }

                var newExecutedTotal = budget.ExecutedAmount + request.ExecutedAmount;
                if (newExecutedTotal > budget.CertifiedAmount)
                {
                    return ValidationFailure<BudgetTransactionDTO>(
                        ErrorMessages.BudgetTransaction.ExecutedTotalExceedsBudgetCertifiedAmount,
                        ErrorCodes.BudgetTransaction.ExecutedTotalExceedsBudgetCertifiedAmount,
                        nameof(ExecuteDevengadoRequestDTO.ExecutedAmount));
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
                {
                    newCertifiedTotal = 0;
                }

                budget.CertifiedAmount = newCertifiedTotal;
                _uow.Budgets.Update(budget);

                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(
                    Map(tx),
                    ExecutionRegisteredMessage);
            }
            catch (UnauthorizedAccessException)
            {
                return FailUnauthorized<BudgetTransactionDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<BudgetTransactionDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<BudgetTransactionDTO>();
            }
        }

        public async Task<ServiceResult<List<BudgetTransactionDTO>>> GetTransactionsAsync(
            int budgetId,
            CancellationToken ct = default)
        {
            try
            {
                var exists = await _uow.Budgets.GetByIdAsync([budgetId], ct);
                if (exists is null)
                {
                    return ServiceResult<List<BudgetTransactionDTO>>.Fail(
                        ErrorMessages.Budget.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Budget.NotFound);
                }

                var list = await _uow.Budgets.GetTransactionsAsync(budgetId, ct);
                var dto = list.Select(Map).ToList();

                return ServiceResult<List<BudgetTransactionDTO>>.Ok(dto, TransactionsRetrievedMessage);
            }
            catch (Exception)
            {
                return FailUnexpected<List<BudgetTransactionDTO>>();
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
                        ErrorMessages.BudgetTransaction.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.BudgetTransaction.NotFound);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Executed || tx.ExecutedAt != null)
                {
                    return ValidationFailure<BudgetTransactionDTO>(
                        ErrorMessages.BudgetTransaction.ExecutedTransactionsCannotBeCancelled,
                        ErrorCodes.BudgetTransaction.ExecutedTransactionsCannotBeCancelled);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Cancelled)
                {
                    return ValidationFailure<BudgetTransactionDTO>(
                        ErrorMessages.BudgetTransaction.AlreadyCancelled,
                        ErrorCodes.BudgetTransaction.AlreadyCancelled);
                }

                var budget = await _uow.Budgets.GetByIdAsync([tx.BudgetId], ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        ErrorMessages.Budget.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Budget.NotFound);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Certification)
                {
                    var newCertifiedTotal = budget.CertifiedAmount - tx.CertifiedAmount;

                    if (newCertifiedTotal < 0)
                    {
                        newCertifiedTotal = 0;
                    }

                    budget.CertifiedAmount = newCertifiedTotal;
                    _uow.Budgets.Update(budget);
                }

                tx.TransactionTypeId = BudgetTransactionTypeIds.Cancelled;

                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(
                    Map(tx),
                    TransactionCancelledMessage);
            }
            catch (DbUpdateException)
            {
                return FailConflict<BudgetTransactionDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<BudgetTransactionDTO>();
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
                    return FailActorUserNotFound<BudgetTransactionDTO>();
                }

                var tx = await _uow.Budgets.GetTransactionByIdAsync(budgetTransactionId, ct);
                if (tx is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        ErrorMessages.BudgetTransaction.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.BudgetTransaction.NotFound);
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Cancelled)
                {
                    return ValidationFailure<BudgetTransactionDTO>(
                        ErrorMessages.BudgetTransaction.CancelledTransactionsCannotBeUpdated,
                        ErrorCodes.BudgetTransaction.CancelledTransactionsCannotBeUpdated);
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
                    return ValidationFailure<BudgetTransactionDTO>(
                        ErrorMessages.BudgetTransaction.ExecutedAmountExceedsCertifiedAmount,
                        ErrorCodes.BudgetTransaction.ExecutedAmountExceedsCertifiedAmount,
                        nameof(UpdateBudgetTransactionRequestDTO.ExecutedAmount));
                }

                if (tx.TransactionTypeId == BudgetTransactionTypeIds.Executed)
                {
                    if (tx.ExecutedAmount is null)
                    {
                        return ValidationFailure<BudgetTransactionDTO>(
                            ErrorMessages.BudgetTransaction.ExecutedAmountRequired,
                            ErrorCodes.BudgetTransaction.ExecutedAmountRequired,
                            nameof(UpdateBudgetTransactionRequestDTO.ExecutedAmount));
                    }

                    if (tx.ExecutedAt is null)
                    {
                        return ValidationFailure<BudgetTransactionDTO>(
                            ErrorMessages.BudgetTransaction.ExecutedAtRequired,
                            ErrorCodes.BudgetTransaction.ExecutedAtRequired,
                            "ExecutedAt");
                    }
                }

                var budget = await _uow.Budgets.GetByIdAsync([tx.BudgetId], ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        ErrorMessages.Budget.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Budget.NotFound);
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
                    return ValidationFailure<BudgetTransactionDTO>(
                        ErrorMessages.Budget.TotalCertifiedExceedsInitialAmount,
                        ErrorCodes.Budget.TotalCertifiedExceedsInitialAmount,
                        nameof(UpdateBudgetTransactionRequestDTO.CertifiedAmount));
                }

                if (executedTotal > certifiedEverTotal)
                {
                    return ValidationFailure<BudgetTransactionDTO>(
                        ErrorMessages.Budget.TotalExecutedExceedsCertifiedAmount,
                        ErrorCodes.Budget.TotalExecutedExceedsCertifiedAmount,
                        nameof(UpdateBudgetTransactionRequestDTO.ExecutedAmount));
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
                return FailUnauthorized<BudgetTransactionDTO>();
            }
            catch (DbUpdateException)
            {
                return FailConflict<BudgetTransactionDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<BudgetTransactionDTO>();
            }
        }

        // =============== HELPERS ===============

        private async Task<int?> GetExistingActorUserIdAsync(CancellationToken ct)
        {
            var currentUserId = _currentUser.GetRequiredUserId();
            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            return user?.IdUser;
        }

        private static ServiceResult<T> FailUnauthorized<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Auth.UserNotAuthenticated,
                ErrorType.Unauthorized,
                ErrorCodes.Auth.UserNotAuthenticated);

        private static ServiceResult<T> FailActorUserNotFound<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Auth.ActorUserNotFound,
                ErrorType.NotFound,
                ErrorCodes.Auth.ActorUserNotFound);

        private static ServiceResult<T> FailConflict<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.PersistenceConflict,
                ErrorType.Conflict,
                ErrorCodes.Common.PersistenceConflict);

        private static ServiceResult<T> FailUnexpected<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);

        private static ServiceResult<T> ValidationFailure<T>(
            string message,
            string errorCode,
            params string[] fields)
        {
            Dictionary<string, string[]>? validation = null;

            if (fields is { Length: > 0 })
            {
                validation = fields
                    .Distinct(StringComparer.Ordinal)
                    .ToDictionary(
                        field => field,
                        _ => new[] { message },
                        StringComparer.Ordinal);
            }

            return ServiceResult<T>.Fail(
                message,
                ErrorType.Validation,
                errorCode,
                validation);
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