using Microsoft.AspNetCore.Identity;

namespace tesisproject.backend.Repositories.Interfaces
{
    /// <summary>
    /// Repository for ASP.NET Identity users with custom queries
    /// for emails and usernames.
    /// </summary>
    public interface IAspNetUserRepository : IGenericRepository<IdentityUser<int>>
    {
        // ===== Emails =====

        /// <summary>
        /// Gets all emails for the specified user IDs.
        /// </summary>
        Task<Dictionary<int, string?>> GetEmailsByUserIdsAsync(
            IEnumerable<int> userIds,
            CancellationToken ct = default);

        /// <summary>
        /// Gets a single user email by ASP.NET user ID.
        /// </summary>
        Task<(int Id, string? Email)?> GetEmailByUserIdAsync(
            int userId,
            CancellationToken ct = default);

        // ===== Usernames =====

        /// <summary>
        /// Gets all usernames for the specified user IDs.
        /// </summary>
        Task<Dictionary<int, string?>> GetUsernamesByUserIdsAsync(
            IEnumerable<int> userIds,
            CancellationToken ct = default);

        /// <summary>
        /// Gets a single username by ASP.NET user ID.
        /// </summary>
        Task<(int Id, string? UserName)?> GetUsernameByUserIdAsync(
            int userId,
            CancellationToken ct = default);
        /// <summary>
        /// Gets all usernames in the system.
        /// </summary>

        Task<IReadOnlyList<string>> GetAllUsernamesAsync(CancellationToken ct = default);
    }
}