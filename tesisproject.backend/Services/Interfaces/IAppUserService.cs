using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    /// <summary>
    /// Handles linking between ASP.NET Identity users and AppUser bridge table.
    /// Can be used from Auth and from Project creation to obtain AppUserId.
    /// </summary>
    public interface IAppUserService
    {
        /// <summary>
        /// Ensures there is an AppUser row for the given request and returns IdUser.
        /// Reuses existing Identity/AppUser rows when possible.
        /// </summary>
        Task<ServiceResult<int>> EnsureAppUserAsync(RegisterRequest dto, CancellationToken ct = default);

        /// <summary>
        /// Ensures AppUser rows for a list of requests and returns their IdUser list
        /// in the same order as the input.
        /// </summary>
        Task<ServiceResult<List<int>>> EnsureAppUsersAsync(IEnumerable<RegisterRequest> dtos, CancellationToken ct = default);

        /// <summary>
        /// Resolves the internal AppUser IdUser from an LocalASP user id.
        /// </summary>
        Task<ServiceResult<int>> GetAppUserIdByLocalIdAsync(int localUserId, CancellationToken ct = default);


    }
}