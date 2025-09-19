using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IBudgetRepository : IGenericRepository<Budget>
    {
        Task<Budget?> GetDetailAsync(int budgetId, bool includeTransactions = false, CancellationToken ct = default);
        Task<Budget?> GetByProjectIdAsync(int projectId, bool includeTransactions = false, CancellationToken ct = default);
        Task<bool> ExistsForProjectAsync(int projectId, CancellationToken ct = default);
    }
}