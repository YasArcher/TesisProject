using Azure.Core;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Budgets.Request;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class BudgetService : IBudgetService
    {
        private readonly IUnitOfWork _uow;

        public BudgetService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        private const int TransactionType_Certification = 1;

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

        public async Task<ServiceResult<BudgetDTO>> GetByProjectIdAsync(int projectId, CancellationToken ct = default)
        {
            try
            {
                var e = await _uow.Budgets.GetByProjectIdAsync(projectId, includeTransactions: false, ct);
                if (e is null)
                    return ServiceResult<BudgetDTO>.Fail("Budget not found for project.", ErrorType.NotFound);

                return ServiceResult<BudgetDTO>.Ok(MapToDTO(e), "Budget retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== WRITES ===============

        public async Task<ServiceResult<BudgetDTO>> CreateAsync(
            CreateBudgetRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                // ============================================
                // 1) Validar que el proyecto exista
                // ============================================
                var projectExists = await _uow.Projects
                    .Query(asNoTracking: true)
                    .AnyAsync(p => p.ProjectId == request.ProjectId, ct);

                if (!projectExists)
                {
                    return ServiceResult<BudgetDTO>.Fail(
                        "Project does not exist.",
                        ErrorType.NotFound);
                }

                // ============================================
                // 2) Crear Budget
                // ============================================
                var entity = new Budget
                {
                    ProjectId = request.ProjectId,
                    ApprovedByUserId = request.ApprovedByUserId,
                    InitialAmount = request.InitialAmount,

                    // Nuevos valores iniciales estándar
                    CertifiedAmount = 0,
                    ExecutedAmount = 0,
                    ApprovedAt = null,

                    FundingTypeId = request.FundingTypeId
                };

                await _uow.Budgets.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                // ============================================
                // 3) Respuesta
                // ============================================
                return ServiceResult<BudgetDTO>.Ok(
                    MapToDTO(entity),
                    "Budget created");
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


        public async Task<ServiceResult<BudgetDTO>> UpdateAsync(int budgetId, UpdateBudgetRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var e = await _uow.Budgets.GetByIdAsync(new object[] { budgetId }, ct);
                if (e is null)
                    return ServiceResult<BudgetDTO>.Fail("Budget not found.", ErrorType.NotFound);

                if (request.CertifiedAmount > request.InitialAmount)
                    return ServiceResult<BudgetDTO>.Fail("Certified amount cannot exceed initial amount.", ErrorType.Validation);

                if (request.ExecutedAmount > request.CertifiedAmount)
                    return ServiceResult<BudgetDTO>.Fail("Executed amount cannot exceed certified amount.", ErrorType.Validation);

                e.ApprovedByUserId = request.ApprovedByUserId;
                e.InitialAmount = request.InitialAmount;
                e.CertifiedAmount = request.CertifiedAmount;
                e.ExecutedAmount = request.ExecutedAmount;
                e.ApprovedAt = request.ApprovedAt;

                _uow.Budgets.Update(e);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetDTO>.Ok(MapToDTO(e), "Budget updated");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int budgetId, CancellationToken ct = default)
        {
            try
            {
                var e = await _uow.Budgets.GetByIdAsync(new object[] { budgetId }, ct);
                if (e is null)
                    return ServiceResult<bool>.Fail("Budget not found.", ErrorType.NotFound);

                _uow.Budgets.Remove(e);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<bool>.Ok(true, "Budget deleted");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<bool>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== Mapper ===============
        private static BudgetDTO MapToDTO(Budget e) => new()
        {
            BudgetId = e.BudgetId,
            ProjectId = e.ProjectId,
            ApprovedByUserId = e.ApprovedByUserId,
            InitialAmount = e.InitialAmount,
            CertifiedAmount = e.CertifiedAmount,
            ExecutedAmount = e.ExecutedAmount,
            ApprovedAt = e.ApprovedAt
        };
        public async Task<ServiceResult<BudgetTransactionDTO>> AddCertificationAsync(AddCertificationRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var budget = await _uow.Budgets.GetByIdAsync(new object[] { request.BudgetId }, ct);
                if (budget is null)
                    return ServiceResult<BudgetTransactionDTO>.Fail("Budget not found.", ErrorType.NotFound);

                // Validaciones: certificación no puede exceder al inicial
                var newCertified = budget.CertifiedAmount + request.Amount;
                if (newCertified > budget.InitialAmount)
                    return ServiceResult<BudgetTransactionDTO>.Fail("Certification exceeds initial amount.", ErrorType.Validation);

                var tx = new BudgetTransaction
                {
                    BudgetId = request.BudgetId,
                    TransactionTypeId = TransactionType_Certification, // 1
                    Amount = request.Amount,
                    BudgetItem = request.BudgetItem,
                    CURNumber = request.CURNumber,
                    CertifiedByUserId = request.CertifiedByUserId,
                    CertifiedAt = (request.CertifiedAt ?? DateTime.UtcNow).Date
                };

                await _uow.Budgets.AddTransactionAsync(tx, ct);

                // Efecto agregado: SOLO suma a certificado
                budget.CertifiedAmount = newCertified;
                _uow.Budgets.Update(budget);

                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(Map(tx), "Certification registered");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<BudgetTransactionDTO>> ExecuteDevengadoAsync(ExecuteDevengadoRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                // Buscar transacción y presupuesto
                var tx = await _uow.Budgets.GetTransactionByIdAsync(request.BudgetTransactionId, ct);
                if (tx is null)
                    return ServiceResult<BudgetTransactionDTO>.Fail("Transaction not found.", ErrorType.NotFound);

                var budget = await _uow.Budgets.GetByIdAsync(new object[] { tx.BudgetId }, ct);
                if (budget is null)
                    return ServiceResult<BudgetTransactionDTO>.Fail("Budget not found.", ErrorType.NotFound);

                if (tx.ExecutedAt != default(DateTime))
                    return ServiceResult<BudgetTransactionDTO>.Fail("Transaction already executed.", ErrorType.Validation);

                // Validación: no puedes devengar más de lo certificado
                //if (budget.ExecutedAmount + tx.Amount > budget.CertifiedAmount)
                //    return ServiceResult<BudgetTransactionDTO>.Fail("Execution exceeds certified amount.", ErrorType.Validation);

                // Marcar como ejecutado
                tx.ExecutedAt = (request.ExecutedAt ?? DateTime.UtcNow).Date;
                tx.ExecutedByUserId = request.ExecutedByUserId;
                if (!string.IsNullOrWhiteSpace(request.CURNumber))
                    tx.CURNumber = request.CURNumber;

                // Efecto agregado: mueve montos
                budget.CertifiedAmount -= tx.Amount;   // resta de certificado
                budget.ExecutedAmount += tx.Amount;   // suma a ejecutado

                _uow.Budgets.Update(budget);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(Map(tx), "Execution (devengado) registered");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<BudgetTransactionDTO>>> GetTransactionsAsync(int budgetId, CancellationToken ct = default)
        {
            try
            {
                var exists = await _uow.Budgets.GetByIdAsync(new object[] { budgetId }, ct);
                if (exists is null)
                    return ServiceResult<List<BudgetTransactionDTO>>.Fail("Budget not found.", ErrorType.NotFound);

                var list = await _uow.Budgets.GetTransactionsAsync(budgetId, ct);
                var dto = list.Select(Map).ToList();

                if (dto.Count == 0)
                    return ServiceResult<List<BudgetTransactionDTO>>.Fail("No transactions found.", ErrorType.NotFound);

                return ServiceResult<List<BudgetTransactionDTO>>.Ok(dto, "Transactions retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<BudgetTransactionDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // Mapper local
        private static BudgetTransactionDTO Map(BudgetTransaction t) => new()
        {
            BudgetTransactionId = t.BudgetTransactionId,
            BudgetId = t.BudgetId,
            TransactionTypeId = t.TransactionTypeId,
            Amount = t.Amount,
            CertifiedAt = t.CertifiedAt,
            ExecutedAt = t.ExecutedAt,
            CertifiedByUserId = t.CertifiedByUserId,
            ExecutedByUserId = t.ExecutedByUserId,
            BudgetItem = t.BudgetItem,
            CURNumber = t.CURNumber
        };
    }
}