using tesisproject.shared.DTOs.Convocation.Response;
using tesisproject.shared.DTOs.ConvocationRule.Request;
using tesisproject.shared.DTOs.ConvocationRule.Response;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    /// <summary>
    /// Repository for Convocation aggregate.
    /// Handles Convocation, ConvocationRule, and ConvocationRuleIndexing entities.
    /// </summary>
    public interface IConvocationRepository : IGenericRepository<Convocation>
    {
        // =============================================================
        // ============= Convocation-level operations ==================
        // =============================================================

        /// <summary>
        /// Gets a Convocation by id; optionally includes its Rules and Indexings.
        /// </summary>
        Task<Convocation?> GetByIdAsync(int id, bool includeRules = false, CancellationToken ct = default);

        /// <summary>
        /// Returns the currently active Convocation (there should be 0 or 1).
        /// </summary>
        Task<Convocation?> GetActiveAsync(CancellationToken ct = default);

        /// <summary>
        /// Checks if any Convocation is active.
        /// </summary>
        Task<bool> AnyActiveAsync(CancellationToken ct = default);

        /// <summary>
        /// Makes the given Convocation the only active one, deactivating all others.
        /// Does NOT call SaveChanges; UoW must persist afterwards.
        /// </summary>
        Task SetActiveExclusiveAsync(int convocationId, CancellationToken ct = default);

        /// <summary>
        /// Searches convocations by text and optionally by active state.
        /// </summary>
        Task<List<Convocation>> SearchAsync(string? text, bool? onlyActive, CancellationToken ct = default);

        /// <summary>
        /// Returns paginated convocations (without date logic).
        /// </summary>
        Task<(List<Convocation> items, int total)> GetPagedAsync(
            int page,
            int pageSize,
            string? search = null,
            CancellationToken ct = default);

        // =============================================================
        // ======== Aggregated child operations (Rules/Indexings) ======
        // =============================================================

        /// <summary>
        /// Adds a new Rule to an existing Convocation.
        /// </summary>
        Task AddRuleAsync(int convocationId, ConvocationRule rule, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing Rule within a Convocation.
        /// </summary>
        Task UpdateRuleAsync(int convocationId, ConvocationRule rule, CancellationToken ct = default);

        /// <summary>
        /// Removes a Rule from a Convocation.
        /// </summary>
        Task RemoveRuleAsync(int convocationId, int ruleId, CancellationToken ct = default);

        /// <summary>
        /// Sets the allowed IndexingSources for a specific Rule (N:N relationship).
        /// </summary>
        Task SetRuleAllowedIndexingsAsync(int ruleId, IEnumerable<int> indexingSourceIds, CancellationToken ct = default);
    }
}