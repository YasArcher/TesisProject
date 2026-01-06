using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IBudgetRepository : IGenericRepository<Budget>
    {
        Task<Budget?> GetDetailAsync(int budgetId, bool includeTransactions = false, CancellationToken ct = default);
        Task<List<Budget>> GetByProjectIdAsync(int projectId, bool includeTransactions, CancellationToken ct);
        Task<bool> ExistsForProjectAsync(int projectId, CancellationToken ct = default);

        // Transactions
        Task AddTransactionAsync(BudgetTransaction tx, CancellationToken ct = default);
        Task<BudgetTransaction?> GetTransactionByIdAsync(int txId, CancellationToken ct = default);
        Task<int?> FindBudgetIdByTransactionAsync(int txId, CancellationToken ct = default);
        Task<List<BudgetTransaction>> GetTransactionsAsync(int budgetId, CancellationToken ct = default);
        void UpdateTransaction(BudgetTransaction tx);
    }
}