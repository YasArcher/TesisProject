using Azure.Core;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Interfaces;
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
                // Opción A: usar directamente el repositorio genérico (sin el método custom)
                var query = _uow.Budgets
                    .Query(asNoTracking: true)
                    .Where(b => b.ProjectId == projectId);

                // Si tienes navegación FundingType en la entidad Budget:
                query = query.Include(b => b.FundingType);

                var list = await query.ToListAsync(ct);

                if (list.Count == 0)
                    return ServiceResult<List<BudgetDTO>>.Fail(
                        "No budgets found for project.",
                        ErrorType.NotFound);

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

            FundingTypeId = e.FundingTypeId,
            FundingTypeName = e.FundingType?.Name,

            InitialAmount = e.InitialAmount,
            CertifiedAmount = e.CertifiedAmount,
            ExecutedAmount = e.ExecutedAmount,
            ApprovedAt = e.ApprovedAt
        };


        public async Task<ServiceResult<BudgetTransactionDTO>> AddCertificationAsync(
            AddCertificationRequestDTO request,
            int currentUserId,
            CancellationToken ct = default)
        {
            try
            {
                var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
                if (user is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "User not found.",
                        ErrorType.NotFound
                    );
                }
                var budget = await _uow.Budgets.GetByIdAsync(new object[] { request.BudgetId }, ct);
                if (budget is null)
                    return ServiceResult<BudgetTransactionDTO>.Fail("Budget not found.", ErrorType.NotFound);

                // Validación: la certificación NO puede exceder el monto inicial
                var newCertified = budget.CertifiedAmount + request.CertifiedAmount;
                if (newCertified > budget.InitialAmount)
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Certification exceeds initial amount.",
                        ErrorType.Validation
                    );

                var tx = new BudgetTransaction
                {
                    BudgetId = request.BudgetId,
                    TransactionTypeId = 1,
                    Name = request.Name,
                    CertifiedAmount = request.CertifiedAmount,
                    BudgetItem = request.BudgetItem,
                    CertificationDescription = request.CertificationDescription,
                    CertifiedByUserId = user.IdUser,
                    CertifiedAt = (request.CertifiedAt ?? DateTime.UtcNow).Date
                };

                await _uow.Budgets.AddTransactionAsync(tx, ct);

                // Efecto agregado: solo sumas a CertifiedAmount del presupuesto
                budget.CertifiedAmount = newCertified;
                _uow.Budgets.Update(budget);

                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(Map(tx), "Certification registered");
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
            int currentUserId,
            CancellationToken ct = default)
        {
            try
            {
                // 1) Obtener AppUser (para usar IdUser)
                var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
                if (user is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "User not found.",
                        ErrorType.NotFound
                    );
                }

                // 2) Obtener la transacción presupuestaria
                var tx = await _uow.Budgets.GetTransactionByIdAsync( request.BudgetTransactionId , ct);

                if (tx is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Budget transaction not found.",
                        ErrorType.NotFound
                    );
                }

                // 3) Validar que aún no esté devengada
                //    Usamos ExecutedAt como indicador
                if (tx.ExecutedAt != null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "This transaction has already been executed.",
                        ErrorType.Validation
                    );
                }

                // 4) Validar que el devengado no exceda lo certificado en ESTA transacción
                if (request.ExecutedAmount > tx.CertifiedAmount)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Executed amount cannot exceed certified amount for this transaction.",
                        ErrorType.Validation
                    );
                }

                // 5) Obtener el presupuesto asociado
                var budget = await _uow.Budgets.GetByIdAsync(new object[] { tx.BudgetId }, ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Budget not found.",
                        ErrorType.NotFound
                    );
                }

                // 6) Validar que el acumulado ejecutado del presupuesto no se pase
                var newExecutedTotal = budget.ExecutedAmount + request.ExecutedAmount;
                if (newExecutedTotal > budget.CertifiedAmount)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Executed total for this budget cannot exceed the certified total.",
                        ErrorType.Validation
                    );
                }

                // 7) Aplicar cambios en la transacción
                tx.ExecutedAmount = request.ExecutedAmount;
                tx.CURNumber = request.CURNumber;
                tx.ExecutionDescription = request.ExecutionDescription;
                tx.ExecutedByUserId = user.IdUser;
                tx.ExecutedAt = (request.ExecutedAt ?? DateTime.UtcNow).Date;
                tx.TransactionTypeId = 2;

                // 8) Actualizar agregados del presupuesto
                budget.ExecutedAmount = newExecutedTotal;

                // 👇 Aquí la parte importante con múltiples certificaciones:
                var newCertifiedTotal = budget.CertifiedAmount - tx.CertifiedAmount;
                if (newCertifiedTotal < 0)
                    newCertifiedTotal = 0;

                budget.CertifiedAmount = newCertifiedTotal;

                _uow.Budgets.Update(budget);

                // 9) Guardar cambios
                await _uow.SaveChangesAsync(ct);


                return ServiceResult<BudgetTransactionDTO>.Ok(
                    Map(tx),
                    "Execution registered"
                );
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict
                );
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected
                );
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

        public async Task<ServiceResult<BudgetTransactionDTO>> CancelTransactionAsync(
            int budgetTransactionId,
            CancellationToken ct = default)
        {
            try
            {
                // 1) Obtener la transacción
                var tx = await _uow.Budgets.GetTransactionByIdAsync(budgetTransactionId, ct);

                if (tx is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Budget transaction not found.",
                        ErrorType.NotFound
                    );
                }

                // 2) Validar que no sea un devengado (no se puede cancelar)
                if (tx.TransactionTypeId == 2 || tx.ExecutedAt != null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Executed transactions cannot be cancelled.",
                        ErrorType.Validation
                    );
                }

                // 3) Validar si ya está cancelada
                if (tx.TransactionTypeId == 3)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "This transaction is already cancelled.",
                        ErrorType.Validation
                    );
                }

                // 4) Ajustar el presupuesto:
                //    - Solo tiene sentido para certificaciones (TransactionTypeId == 1)
                var budget = await _uow.Budgets.GetByIdAsync(new object[] { tx.BudgetId }, ct);
                if (budget is null)
                {
                    return ServiceResult<BudgetTransactionDTO>.Fail(
                        "Budget not found.",
                        ErrorType.NotFound
                    );
                }

                if (tx.TransactionTypeId == 1)
                {
                    // Restar del acumulado certificado el valor de ESTA transacción
                    var newCertifiedTotal = budget.CertifiedAmount - tx.CertifiedAmount;

                    // Por seguridad, evitar negativos
                    if (newCertifiedTotal < 0)
                        newCertifiedTotal = 0;

                    budget.CertifiedAmount = newCertifiedTotal;
                    _uow.Budgets.Update(budget);
                }

                // 5) Marcar la transacción como cancelada
                tx.TransactionTypeId = 3;

                // 6) Guardar cambios
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<BudgetTransactionDTO>.Ok(
                    Map(tx),
                    "Transaction cancelled."
                );
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict
                );
            }
            catch (Exception ex)
            {
                return ServiceResult<BudgetTransactionDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected
                );
            }
        }



        // Mapper local
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